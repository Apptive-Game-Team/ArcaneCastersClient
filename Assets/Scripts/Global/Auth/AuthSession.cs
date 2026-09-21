using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Data;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Global.Serialization;

namespace Global.Auth
{
    /// <summary>
    /// access token 을 들고 있는 하나뿐인 곳. 로그인/회원가입/guest 로그인 성공 뒤 refresh token 을
    /// <see cref="IRefreshTokenStore"/>에 저장하고, access token 이 만료되기 전에 미리 갱신해서
    /// <see cref="Server.SetAuthorization"/> 을 쓰는 개별 요청마다 401 재시도를 넣지 않아도 되게 한다.
    ///
    /// <see cref="EnsureInstance"/> 가 앱 시작 시 스스로를 만들어 <c>DontDestroyOnLoad</c> 로 살아남는
    /// 오브젝트에 붙는다. Editor 가 없어 scene 파일을 손으로 편집하기 어려운 환경이라, scene 배선 없이
    /// 코드만으로 준비되게 했다.
    /// </summary>
    public class AuthSession : SingletonObject<AuthSession>
    {
        /// <summary>access token 이 3600초로 줄어드는 계약에 맞춰, 만료 10분 전쯤 미리 갱신한다.</summary>
        private const float RenewalIntervalSeconds = 50f * 60f;

        private const float WebGlCallbackTimeoutSeconds = 15f;
        private const int RequestTimeoutSeconds = 10;

        private const string LoginSceneName = "LoginScene";
        private const string MessageTable = "SystemMessageUI";

        private static readonly LocalizedString authSessionExpired = new LocalizedString
            { TableReference = MessageTable, TableEntryReference = "authSessionExpired" };

        private enum RefreshOutcome
        {
            Success,
            Unauthorized,
            TransientFailure
        }

        private IRefreshTokenStore refreshTokenStore;
        private Coroutine renewalLoop;
        private bool refreshInFlight;
        private readonly List<Action<RefreshOutcome>> pendingRefreshCallbacks = new List<Action<RefreshOutcome>>();

        private CredentialedPostResponse webGlRefreshResponse;
        private bool webGlRefreshResponsePending;
        private bool webGlLogoutResponsePending;

        /// <summary>가장 최근 갱신에서 서버가 알려준 access token 만료 시각. 진단용으로만 쓴다.</summary>
        public DateTime AccessTokenExpiresAtUtc { get; private set; } = DateTime.MinValue;

        public RefreshTokenDelivery TokenDelivery => refreshTokenStore.Delivery;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance != null) return;
            new GameObject(nameof(AuthSession)).AddComponent<AuthSession>();
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return; // 중복 생성돼 파괴될 인스턴스. 초기화할 필요가 없다.

            refreshTokenStore = RefreshTokenStoreSelector.SelectForCurrentPlatform();
        }

        /// <summary>
        /// 앱을 켤 때 한 번 부른다. refresh 를 한 번 시도해서, 성공하면 로그인 화면을 건너뛸 수 있다고
        /// 알린다. WebGL 은 cookie 가 있는지 미리 알 방법이 없어(<see cref="IRefreshTokenStore.TryRead"/>
        /// 가 항상 false), 저장된 값을 먼저 확인하고 갈라 타는 대신 모든 플랫폼이 이 refresh 시도 자체로
        /// 판단한다. 이 시도 앞에 TryRead 검사를 넣지 않는다 — 넣으면 WebGL 은 유효한 cookie 가 있어도
        /// 항상 로그인 화면으로 빠진다.
        /// </summary>
        public IEnumerator TryRestoreSession(Action<bool> onRestored)
        {
            yield return RefreshAccessToken(outcome =>
            {
                bool restored = outcome == RefreshOutcome.Success;
                if (restored) RestartRenewalLoop();
                onRestored?.Invoke(restored);
            });
        }

        /// <summary>
        /// 로그인/회원가입/guest 로그인 성공 뒤 부른다. refresh token 을 저장하고 만료 시각을 기록한
        /// 다음 주기 갱신을 시작한다.
        /// </summary>
        public void BeginSession(string jwt, string refreshToken, int expiresInSeconds)
        {
            SceneContext.JwtToken = jwt;
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresInSeconds);

            // Cookie 방식은 refreshToken 이 애초에 오지 않는다(서버가 Set-Cookie 로 이미 심었다).
            if (refreshTokenStore.Delivery == RefreshTokenDelivery.Body && !string.IsNullOrEmpty(refreshToken))
            {
                refreshTokenStore.Save(refreshToken);
            }

            RestartRenewalLoop();
        }

        /// <summary>
        /// 취소 endpoint 를 부르고(결과와 관계없이) 저장소와 메모리 상태를 비운다.
        /// </summary>
        public IEnumerator Logout(Action onComplete = null)
        {
            yield return SendLogoutRequest();

            refreshTokenStore.Clear();
            SceneContext.ClearContext();

            if (renewalLoop != null)
            {
                StopCoroutine(renewalLoop);
                renewalLoop = null;
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// 화면이 꺼졌다 돌아오면 이 coroutine 이 멈춰 있던 동안 시간이 얼마나 흘렀는지 스스로는 모른다.
        /// 주기 timer 하나로는 그 공백을 못 메우므로, 돌아올 때마다 한 번 즉시 갱신부터 하고 나서 다시
        /// 50분 주기를 새로 잡는다.
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) return;
            if (renewalLoop == null) return; // 세션이 없으면 갱신할 것도 없다.

            StopCoroutine(renewalLoop);
            renewalLoop = StartCoroutine(ResumeRenewalLoop());
        }

        private void RestartRenewalLoop()
        {
            if (renewalLoop != null) StopCoroutine(renewalLoop);
            renewalLoop = StartCoroutine(RenewalLoop());
        }

        private IEnumerator ResumeRenewalLoop()
        {
            RefreshOutcome outcome = RefreshOutcome.TransientFailure;
            yield return RefreshAccessToken(result => outcome = result);

            if (outcome == RefreshOutcome.Unauthorized)
            {
                HandleSessionExpired();
                yield break;
            }

            yield return RenewalLoop();
        }

        private IEnumerator RenewalLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(RenewalIntervalSeconds);

                RefreshOutcome outcome = RefreshOutcome.TransientFailure;
                yield return RefreshAccessToken(result => outcome = result);

                if (outcome == RefreshOutcome.Unauthorized)
                {
                    HandleSessionExpired();
                    yield break;
                }

                // TransientFailure: 네트워크가 잠깐 끊긴 것일 수 있으므로 로그아웃시키지 않는다.
                // 세션을 그대로 두고 다음 주기에 다시 시도한다.
            }
        }

        private void HandleSessionExpired()
        {
            renewalLoop = null;
            // store 와 SceneContext 는 PerformRefresh 의 Unauthorized 분기에서 이미 비웠다.
            WDebug.LogWarning("[AuthSession] refresh token 이 거부됐다(401). 로그인 화면으로 돌아간다.");

            // 씬마다 SystemMessageUI가 있으리라 보장할 수 없다. 안내를 못 띄워도 로그인 화면 전환은 막지 않는다.
            if (SystemMessageUI.Instance != null) SystemMessageUI.Instance.ShowMessage(authSessionExpired);

            SceneManager.LoadScene(LoginSceneName);
        }

        /// <summary>
        /// 이미 진행 중인 refresh 가 있으면 새 호출은 그 결과를 같이 받는다 — refresh token 은
        /// rotation 되므로, 겹쳐 부르면 뒤에 도착한 요청이 서버가 이미 태워버린 옛 token 을 다시
        /// 보내 애먼 401 을 만들 수 있다.
        /// </summary>
        private IEnumerator RefreshAccessToken(Action<RefreshOutcome> onComplete)
        {
            bool callbackFired = false;
            pendingRefreshCallbacks.Add(result =>
            {
                callbackFired = true;
                onComplete?.Invoke(result);
            });

            if (!refreshInFlight)
            {
                refreshInFlight = true;
                yield return PerformRefresh();
            }

            while (!callbackFired) yield return null;
        }

        private IEnumerator PerformRefresh()
        {
            RefreshOutcome outcome;
            string newJwt = null;
            string newRefreshToken = null;
            int expiresInSeconds = 0;

            if (refreshTokenStore.Delivery == RefreshTokenDelivery.Cookie)
            {
                webGlRefreshResponse = null;
                webGlRefreshResponsePending = true;
                BrowserCredentialedPost.Send(
                    ServerList.AccountServer.Api.Path("api", "auth", "refresh"),
                    gameObject.name,
                    nameof(HandleWebGlRefreshResponse));

                float deadline = Time.realtimeSinceStartup + WebGlCallbackTimeoutSeconds;
                while (webGlRefreshResponsePending && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                webGlRefreshResponsePending = false;

                CredentialedPostResponse response = webGlRefreshResponse;
                webGlRefreshResponse = null;

                if (response == null)
                {
                    WDebug.LogWarning("[AuthSession] cookie refresh 가 browser 콜백을 기다리다 시간 초과됐다.");
                    outcome = RefreshOutcome.TransientFailure;
                }
                else if (response.status == 401)
                {
                    outcome = RefreshOutcome.Unauthorized;
                }
                else if (response.status == 200 &&
                         JsonCodec.TryDeserialize(response.body, out RefreshResponseDto dto, out _))
                {
                    outcome = RefreshOutcome.Success;
                    newJwt = dto.jwt;
                    newRefreshToken = dto.refreshToken;
                    expiresInSeconds = dto.expiresIn;
                }
                else
                {
                    WDebug.LogWarning($"[AuthSession] cookie refresh 실패: status={response.status} body={JsonCodec.Excerpt(response.body)}");
                    outcome = RefreshOutcome.TransientFailure;
                }
            }
            else
            {
                refreshTokenStore.TryRead(out string storedToken);
                string requestBody = JsonCodec.Serialize(new RefreshTokenBodyDto(storedToken));

                using UnityWebRequest webRequest = new UnityWebRequest(
                    ServerList.AccountServer.Api.Path("api", "auth", "refresh"), "POST");
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                Server.SetTokenDelivery(webRequest);
                webRequest.timeout = RequestTimeoutSeconds;

                yield return webRequest.SendWebRequest();

                if (webRequest.responseCode == 401)
                {
                    outcome = RefreshOutcome.Unauthorized;
                }
                else if (webRequest.responseCode == 200 &&
                         JsonCodec.TryDeserialize(webRequest.downloadHandler.text, out RefreshResponseDto dto, out _))
                {
                    outcome = RefreshOutcome.Success;
                    newJwt = dto.jwt;
                    newRefreshToken = dto.refreshToken;
                    expiresInSeconds = dto.expiresIn;
                }
                else
                {
                    WDebug.LogWarning($"[AuthSession] refresh 실패: {webRequest.responseCode} / {JsonCodec.Excerpt(webRequest.downloadHandler.text)}");
                    outcome = RefreshOutcome.TransientFailure;
                }
            }

            switch (outcome)
            {
                case RefreshOutcome.Success:
                    SceneContext.JwtToken = newJwt;
                    AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresInSeconds);
                    // 서버가 매 refresh 마다 값을 rotation 한다. 여기서 덮어쓰지 않으면 다음
                    // 시도가 서버가 이미 태운 옛 값을 보내 로그아웃당한다.
                    if (refreshTokenStore.Delivery == RefreshTokenDelivery.Body && !string.IsNullOrEmpty(newRefreshToken))
                    {
                        refreshTokenStore.Save(newRefreshToken);
                    }
                    break;
                case RefreshOutcome.Unauthorized:
                    refreshTokenStore.Clear();
                    SceneContext.ClearContext();
                    break;
                case RefreshOutcome.TransientFailure:
                    // 저장된 값은 그대로 둔다. 네트워크 문제였을 수 있고, 다음 시도가 판단한다.
                    break;
            }

            refreshInFlight = false;
            List<Action<RefreshOutcome>> callbacks = new List<Action<RefreshOutcome>>(pendingRefreshCallbacks);
            pendingRefreshCallbacks.Clear();
            foreach (Action<RefreshOutcome> callback in callbacks) callback?.Invoke(outcome);
        }

        private IEnumerator SendLogoutRequest()
        {
            if (refreshTokenStore.Delivery == RefreshTokenDelivery.Cookie)
            {
                webGlLogoutResponsePending = true;
                BrowserCredentialedPost.Send(
                    ServerList.AccountServer.Api.Path("api", "auth", "logout"),
                    gameObject.name,
                    nameof(HandleWebGlLogoutResponse));

                float deadline = Time.realtimeSinceStartup + WebGlCallbackTimeoutSeconds;
                while (webGlLogoutResponsePending && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                webGlLogoutResponsePending = false;
                // 서버 응답과 무관하게 호출부가 local 상태를 비운다 — 취소는 best-effort 다.
            }
            else
            {
                refreshTokenStore.TryRead(out string storedToken);
                string requestBody = JsonCodec.Serialize(new RefreshTokenBodyDto(storedToken));

                using UnityWebRequest webRequest = new UnityWebRequest(
                    ServerList.AccountServer.Api.Path("api", "auth", "logout"), "POST");
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                Server.SetTokenDelivery(webRequest);
                webRequest.timeout = RequestTimeoutSeconds;

                yield return webRequest.SendWebRequest();

                if (webRequest.responseCode != 204)
                {
                    WDebug.LogWarning($"[AuthSession] logout endpoint 가 {webRequest.responseCode} 를 돌려줬다. local 세션은 그래도 비운다.");
                }
            }
        }

        // JS(RefreshToken.jslib) 가 BrowserCredentialedPost 를 통해 SendMessage 로 부른다.
        private void HandleWebGlRefreshResponse(string json)
        {
            if (!JsonCodec.TryDeserialize(json, out CredentialedPostResponse response, out string error))
            {
                WDebug.LogError($"[AuthSession] WebGL refresh 콜백 파싱 실패: {error} / {JsonCodec.Excerpt(json)}");
                response = new CredentialedPostResponse { status = 0, body = error };
            }
            webGlRefreshResponse = response;
            webGlRefreshResponsePending = false;
        }

        private void HandleWebGlLogoutResponse(string json)
        {
            webGlLogoutResponsePending = false;
        }

        [Serializable]
        private class RefreshTokenBodyDto
        {
            public string refreshToken;

            public RefreshTokenBodyDto(string refreshToken)
            {
                this.refreshToken = refreshToken;
            }
        }

        [Serializable]
        private class RefreshResponseDto
        {
            public string jwt;
            public string refreshToken;
            public int expiresIn;
        }
    }
}
