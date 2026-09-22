using System.Collections;
using Global;
using UnityEngine;
using UnityEngine.UI;

namespace GameScene.Emote
{
    /// <summary>
    /// GameScene HUD 의 emote 고르기 판. toggle button 을 누르면 아이콘 5개가 열리고,
    /// 하나를 고르면 서버로 보낸 뒤 판을 닫고 3.5초 동안 button 을 잠근다.
    /// </summary>
    /// <remarks>
    /// 잠금은 눈에 보이는 안내일 뿐이다. 3초 안에 두 번 온 emote 를 버리는 것은 서버이고,
    /// 서버가 판단을 갖는다.
    /// </remarks>
    public class EmotePickerUI : MonoBehaviour
    {
        /// <summary>
        /// button 을 잠가 두는 시간. 서버의 3초보다 0.5초 길다.
        /// <para>
        /// 두 시간이 서로 다른 순간에 시작한다. 이쪽은 보내는 순간부터 세고 서버는 받는 순간부터
        /// 세므로, network 지연만큼 이쪽이 먼저 풀린다. 같은 3초로 맞추면 잠금이 풀린 직후에 누른
        /// emote 가 아직 서버의 3초 안에 있어 조용히 버려진다. 서버는 버린 것을 알려주지 않고
        /// 말풍선은 서버가 돌려보낸 뒤에야 뜨므로, 누른 사람에게는 아무 일도 일어나지 않은 것처럼
        /// 보인다. 0.5초는 그 틈을 덮는 여유다. 서버와 같은 값으로 되돌리지 말 것.
        /// </para>
        /// </summary>
        private const float CooldownSeconds = 3.5f;

        [SerializeField] private GameObject picker;
        [SerializeField] private Button toggleButton;

        /// <summary>EmoteTypes.All 과 같은 순서로 채운다.</summary>
        [SerializeField] private Button[] emoteButtons;

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

        /// <summary>
        /// 잠금은 이 시계 하나로만 풀린다. 서버가 돌려보낸 emote 를 기다리면 그 메시지가
        /// 늦거나 버려졌을 때 button 이 잠긴 채로 남는다.
        /// </summary>
        private IEnumerator RunCooldown()
        {
            SetButtonsInteractable(false);
            yield return new WaitForSeconds(CooldownSeconds);
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
