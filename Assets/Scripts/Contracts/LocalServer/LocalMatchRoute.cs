using System;
using System.Collections;
using Data;

namespace LocalServer
{
    /// <summary>
    /// 봇전과 모험 시작 앞에 두는 로컬 경로. desktop 에서만 <c>LocalMatchRouter</c> 가 <see cref="Starter"/> 를
    /// 채운다. WebGL 과 Android 에서는 null 로 남아 기존 경로만 탄다.
    ///
    /// <c>MatchQueueApiService</c> 가 이 어셈블리(WordOnline.Contracts)에 있어 Assembly-CSharp 의 로컬 서버
    /// 코드를 직접 부를 수 없으므로, <see cref="Server.JwtTokenProvider"/> 처럼 delegate 로 연결한다.
    /// </summary>
    public static class LocalMatchRoute
    {
        /// <summary>
        /// 로컬 경기를 만들어 본다. 성공하면 callback 에 <see cref="MatchedInfoDto"/> 를, 조건이 맞지 않거나 어느
        /// 단계든 실패하면 null 을 넘긴다. null 이면 호출부는 조용히 기존 endpoint 로 간다.
        /// 인자는 <see cref="LocalSessionRequest.type"/> 값과 모험 시나리오 id 다.
        /// </summary>
        public static Func<string, long?, Action<MatchedInfoDto>, IEnumerator> Starter { get; set; }

        /// <summary><see cref="Starter"/> 가 없으면 바로 null 을 넘긴다.</summary>
        public static IEnumerator TryStart(string type, long? scenarioId, Action<MatchedInfoDto> callback)
        {
            Func<string, long?, Action<MatchedInfoDto>, IEnumerator> starter = Starter;
            if (starter == null)
            {
                callback(null);
                yield break;
            }

            MatchedInfoDto result = null;
            bool done = false;
            IEnumerator routine = null;
            try
            {
                routine = starter(type, scenarioId, dto =>
                {
                    result = dto;
                    done = true;
                });
            }
            catch (Exception exception)
            {
                Global.WDebug.LogWarning($"[LocalMatchRoute] 로컬 경기 준비 실패, 공유 서버로 갑니다: {exception.Message}");
            }

            if (routine != null) yield return routine;

            callback(done ? result : null);
        }
    }
}
