using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data;
using Global;
using TMPro;
using UnityEngine;

namespace LobbyScene
{
    /// <summary>
    /// 관리자가 고른 서버는 일시적으로 응답하지 않아도 유지한다.
    /// 저장된 선택이 없는 첫 실행에서만 정상 응답한 서버를 우선순위에 따라 고른다.
    /// </summary>
    public class MatchingServerDropdown : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;

        private readonly List<MatchingServerOption> serverOptions = new List<MatchingServerOption>();

        private IEnumerator Start()
        {
            dropdown.ClearOptions();
            dropdown.interactable = false;

            yield return CollectOptions();

            if (serverOptions.Count == 0)
            {
                WDebug.LogWarning("[MatchingServerDropdown] no healthy matching server found; keeping " +
                                  MatchingServerCatalog.Current.DisplayName);
                yield break;
            }

            dropdown.AddOptions(serverOptions.Select(option => option.DisplayName).ToList());

            MatchingServerOption initialOption = ResolveInitialOption();
            dropdown.SetValueWithoutNotify(serverOptions.IndexOf(initialOption));
            dropdown.RefreshShownValue();
            dropdown.interactable = true;
            dropdown.onValueChanged.AddListener(OnOptionPicked);

            if (initialOption != MatchingServerCatalog.Current)
            {
                MatchingServerSwitcher.SwitchTo(initialOption);
            }
        }

        /// <summary>
        /// 모든 후보를 확인하고, 정상 서버와 관리자가 명시적으로 고른 서버를 목록에 남긴다.
        /// </summary>
        private IEnumerator CollectOptions()
        {
            var probeResults = new Dictionary<MatchingServerOption, bool>();

            foreach (MatchingServerOption option in MatchingServerCatalog.Options)
            {
                MatchingServerOption probedOption = option;
                StartCoroutine(MatchingServerHealthProbe.Probe(
                    probedOption.Server,
                    isHealthy => probeResults[probedOption] = isHealthy));
            }

            while (probeResults.Count < MatchingServerCatalog.Options.Count)
            {
                yield return null;
            }

            serverOptions.Clear();
            MatchingServerOption storedOption = MatchingServerCatalog.FindStoredOption();
            foreach (MatchingServerOption option in MatchingServerCatalog.Options)
            {
                if (probeResults[option] || option == storedOption)
                    serverOptions.Add(option);
            }
        }

        private MatchingServerOption ResolveInitialOption()
        {
            MatchingServerOption storedOption = MatchingServerCatalog.FindStoredOption();
            if (storedOption != null && serverOptions.Contains(storedOption))
                return storedOption;

            return serverOptions[0];
        }

        private void OnOptionPicked(int index)
        {
            if (index < 0 || index >= serverOptions.Count)
                return;

            MatchingServerSwitcher.SwitchTo(serverOptions[index]);
        }
    }
}
