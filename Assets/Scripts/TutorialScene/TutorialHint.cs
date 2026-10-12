using Data.Localization;
using TMPro;
using UnityEngine;

namespace TutorialScene
{
    /// <summary>
    /// 화면 위쪽 가운데에 반투명 띠와 한 줄 문구로 지금 할 일을 알려 준다. 다음 버튼은 없다.
    /// 사용자가 그 동작을 하면 부른 쪽이 다음 단계로 넘어가며 문구를 바꾸거나 거둔다.
    /// </summary>
    /// <remarks>
    /// 프리팹 루트가 자기 Screen Space Overlay Canvas 를 가져 씬 파일을 고치지 않고 띄운다.
    /// 띠와 문구는 raycast 를 받지 않아 뒤의 전장과 버튼을 막지 않는다. 한 씬에 하나만 둔다.
    /// </remarks>
    public sealed class TutorialHint : MonoBehaviour
    {
        private const string ResourcePath = "UI/Tutorial/TutorialHint";

        [SerializeField] private GameObject band;
        [SerializeField] private TMP_Text message;

        private static TutorialHint current;

        // 문구를 비동기로 읽는 사이에 다른 문구를 띄우거나 거두면, 늦게 도착한 앞 문구가
        // 뒤 문구를 덮어쓴다. 띄울 때마다 번호를 올리고 도착한 결과의 번호를 맞춰 본다.
        private int requestId;

        /// <summary>table 의 key 문구를 한 줄로 띄운다. 이미 떠 있으면 문구만 바꾼다.</summary>
        public static void Show(string table, string key)
        {
            TutorialHint hint = GetOrCreate();
            if (hint != null)
            {
                hint.SetMessage(table, key);
            }
        }

        /// <summary>떠 있는 문구를 거둔다. 떠 있지 않으면 아무것도 하지 않는다.</summary>
        public static void HideCurrent()
        {
            if (current == null)
            {
                return;
            }

            current.requestId++;
            current.band.SetActive(false);
        }

        private static TutorialHint GetOrCreate()
        {
            if (current != null)
            {
                return current;
            }

            TutorialHint prefab = Resources.Load<TutorialHint>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[TutorialHint] Missing prefab at Resources/{ResourcePath}");
                return null;
            }

            current = Instantiate(prefab);
            return current;
        }

        private async void SetMessage(string table, string key)
        {
            int id = ++requestId;
            band.SetActive(true);
            message.text = string.Empty;

            string localized = await LocaleUtils.GetStringAsync(table, key);
            if (this == null || id != requestId)
            {
                return;
            }

            message.text = string.IsNullOrWhiteSpace(localized) ? key : localized;
        }

        private void Awake()
        {
            band.SetActive(false);
        }

        private void OnDestroy()
        {
            if (current == this)
            {
                current = null;
            }
        }
    }
}
