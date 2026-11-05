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
    /// 정상 응답한 서버 중 저장된 선택을 우선 적용한다.
    /// 저장된 서버가 응답하지 않으면 정상 서버를 우선순위에 따라 고른다.
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
        /// 저장된 선택도 상태 검사를 통과해야 자동 선택 후보에 포함한다.
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
            foreach (MatchingServerOption option in MatchingServerCatalog.Options)
            {
                if (probeResults[option])
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
