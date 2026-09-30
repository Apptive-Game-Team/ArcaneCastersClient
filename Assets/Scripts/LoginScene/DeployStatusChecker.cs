using System;
using System.Collections;
using Data;
using Global;
using Global.Util;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;
using Global.Serialization;

namespace LoginScene
{
    public static class DeployStatusChecker
    {
        [Serializable]
        private class DeployStatusDto
        {
            public string status;
        }

        private static readonly LocalizedString serverMaintenance = new LocalizedString
            { TableReference = "SystemMessageUI", TableEntryReference = "serverMaintenance" };

        private static readonly LocalizedString serverDown = new LocalizedString
            { TableReference = "SystemMessageUI", TableEntryReference = "serverDown" };

        private static readonly LocalizedString noInternet = new LocalizedString
            { TableReference = "SystemMessageUI", TableEntryReference = "noInternet" };

        private static readonly LocalizedString serverDeploying = new LocalizedString
            { TableReference = "SystemMessageUI", TableEntryReference = "serverDeploying" };

        /// <summary>
        /// Checks the deploy status of the lobby server.
        /// Invokes onResult with true if the server is Healthy, false otherwise.
        /// Displays an appropriate localized message for non-Healthy states.
        /// </summary>
        public static IEnumerator CheckDeployStatus(Action<bool, string> onResult)
        {
            {
                // 1. WebGL에서 LocalizationSettings가 로드될 때까지 비동기 대기 (필수)
                if (!LocalizationSettings.InitializationOperation.IsDone)
                {
                    yield return LocalizationSettings.InitializationOperation;
                }

                var url = ServerList.MatchingServer.url + "/api/deploy/status";

                using var www = UnityWebRequest.Get(url);

                // 2. 이제 내부에서 SelectedLocale에 접근해도 WaitForCompletion() 오류가 발생하지 않습니다.
                Server.SetAcceptLanguage(www);
                www.timeout = 10;

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    WDebug.LogError($"[CheckDeployStatus] fail: {www.responseCode} / {www.error}");

                    // A ProtocolError means the server answered, so the connection is fine. Only a
                    // request that never got an answer can be the device's own connection.
                    bool online = true;
                    if (www.result == UnityWebRequest.Result.ConnectionError)
                    {
                        yield return InternetProbe.IsOnline(isOnline => online = isOnline);
                    }

                    onResult?.Invoke(false, (online ? serverDown : noInternet).GetLocalizedString());
                    yield break;
                }

                string body = www.downloadHandler.text;
                if (!JsonCodec.TryDeserialize(body, out DeployStatusDto dto, out string parseError))
                {
                    WDebug.LogError($"[CheckDeployStatus] JSON parse error: {parseError}\n{JsonCodec.Excerpt(body)}");
                    SystemMessageUI.Instance.ShowMessage(serverDown);
                    onResult?.Invoke(false, serverDown.GetLocalizedString());
                    yield break;
                }

                if (dto == null || string.IsNullOrEmpty(dto.status))
                {
                    WDebug.LogWarning("[CheckDeployStatus] empty or null status");
                    SystemMessageUI.Instance.ShowMessage(serverDown);
                    onResult?.Invoke(false, serverDown.GetLocalizedString());
                    yield break;
                }

                WDebug.Log("[CheckDeployStatus] server status: " + dto.status);

                switch (dto.status.ToUpperInvariant())
                {
                    case "HEALTHY":
                        onResult?.Invoke(true, "HEALTHY");
                        break;
                    case "MAINTENANCE":
                        SystemMessageUI.Instance.ShowMessage(serverMaintenance);
                        onResult?.Invoke(false, serverMaintenance.GetLocalizedString());
                        break;
                    case "DEPLOYING":
                        SystemMessageUI.Instance.ShowMessage(serverDeploying);
                        onResult?.Invoke(false, serverDeploying.GetLocalizedString());
                        break;
                    default:
                        SystemMessageUI.Instance.ShowMessage(serverDown);
                        onResult?.Invoke(false, serverDown.GetLocalizedString());
                        break;
                }
            }
        }
    }
}