using System.Collections;
using Global;
using UnityEngine;
using UnityEngine.UI;

namespace GameScene.Emote
{
    /// <summary>
    /// GameScene HUD 의 emote 고르기 판. toggle button 을 누르면 아이콘 5개가 열리고,
    /// 하나를 고르면 서버로 보낸 뒤 판을 닫고 3초 동안 button 을 잠근다.
    /// </summary>
    /// <remarks>
    /// 잠금은 눈에 보이는 안내일 뿐이다. 3초 안에 두 번 온 emote 를 버리는 것은 서버이고,
    /// 서버가 판단을 갖는다.
    /// </remarks>
    public class EmotePickerUI : MonoBehaviour
    {
        [SerializeField] private GameObject picker;
        [SerializeField] private Button toggleButton;

        /// <summary>EmoteTypes.All 과 같은 순서로 채운다.</summary>
        [SerializeField] private Button[] emoteButtons;

        [SerializeField] private float cooldownSeconds = 3f;

        private Coroutine cooldownRoutine;

        private void Awake()
        {
            if (picker != null)
            {
                picker.SetActive(false);
            }

            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(TogglePicker);
            }

            if (emoteButtons == null)
            {
                return;
            }

            int wiredCount = Mathf.Min(emoteButtons.Length, EmoteTypes.All.Length);
            if (emoteButtons.Length != EmoteTypes.All.Length)
            {
                WDebug.LogWarning(
                    $"[Emote] button 수가 emote 수와 다르다. buttons: {emoteButtons.Length}, emotes: {EmoteTypes.All.Length}");
            }

            for (int i = 0; i < wiredCount; i++)
            {
                Button button = emoteButtons[i];
                if (button == null)
                {
                    continue;
                }

                EmoteType emote = EmoteTypes.All[i];
                button.onClick.AddListener(() => OnEmoteButtonClicked(emote));
            }
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(TogglePicker);
            }
        }

        private void TogglePicker()
        {
            if (picker == null)
            {
                return;
            }

            picker.SetActive(!picker.activeSelf);
        }

        private void OnEmoteButtonClicked(EmoteType emote)
        {
            if (cooldownRoutine != null)
            {
                return;
            }

            if (!EmoteInputSender.Send(emote))
            {
                return;
            }

            if (picker != null)
            {
                picker.SetActive(false);
            }

            cooldownRoutine = StartCoroutine(RunCooldown());
        }

        private IEnumerator RunCooldown()
        {
            SetButtonsInteractable(false);
            yield return new WaitForSeconds(cooldownSeconds);
            SetButtonsInteractable(true);
            cooldownRoutine = null;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (emoteButtons == null)
            {
                return;
            }

            foreach (Button button in emoteButtons)
            {
                if (button != null)
                {
                    button.interactable = interactable;
                }
            }
        }
    }
}
