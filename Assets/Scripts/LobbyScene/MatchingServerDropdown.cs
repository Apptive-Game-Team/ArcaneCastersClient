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
    /// Lets an admin pick which matching server the client talks to.
    ///
    /// Servers that fail their health check are left out of the dropdown, and the first healthy
    /// server in <see cref="MatchingServerCatalog.Options"/> order (127.0.0.1, dev, deploy) is
    /// selected unless the admin already picked one on this device.
    ///
    /// Put this on a GameObject that also has <see cref="AdminOnly"/>: that component deactivates
    /// the GameObject in Awake for non-admins, so none of the probing below runs for them.
    /// </summary>
    public class MatchingServerDropdown : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;

        private readonly List<MatchingServerOption> healthyOptions = new List<MatchingServerOption>();

        private IEnumerator Start()
        {
            dropdown.ClearOptions();
            dropdown.interactable = false;

            yield return CollectHealthyOptions();

            if (healthyOptions.Count == 0)
            {
                WDebug.LogWarning("[MatchingServerDropdown] no healthy matching server found; keeping " +
                                  MatchingServerCatalog.Current.DisplayName);
                yield break;
            }

            dropdown.AddOptions(healthyOptions.Select(option => option.DisplayName).ToList());

            MatchingServerOption initialOption = ResolveInitialOption();
            dropdown.SetValueWithoutNotify(healthyOptions.IndexOf(initialOption));
            dropdown.RefreshShownValue();
            dropdown.interactable = true;
            dropdown.onValueChanged.AddListener(OnOptionPicked);

            if (initialOption != MatchingServerCatalog.Current)
            {
                MatchingServerSwitcher.SwitchTo(initialOption);
            }
        }

        /// <summary>
        /// Probes every candidate at once and keeps the healthy ones in catalog priority order.
        /// </summary>
        private IEnumerator CollectHealthyOptions()
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

            healthyOptions.Clear();
            foreach (MatchingServerOption option in MatchingServerCatalog.Options)
            {
                if (probeResults[option])
                    healthyOptions.Add(option);
            }
        }

        private MatchingServerOption ResolveInitialOption()
        {
            MatchingServerOption storedOption = MatchingServerCatalog.FindStoredOption();
            if (storedOption != null && healthyOptions.Contains(storedOption))
                return storedOption;

            return healthyOptions[0];
        }

        private void OnOptionPicked(int index)
        {
            if (index < 0 || index >= healthyOptions.Count)
                return;

            MatchingServerSwitcher.SwitchTo(healthyOptions[index]);
        }
    }
}
