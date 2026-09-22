using System.Collections;
using Global;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameScene.Emote
{
    /// <summary>
    /// GameScene HUD 의 emote 고르기. 버튼을 누르면 아이콘 5개가 부채꼴로 펼쳐지고, 누른 채
    /// 옮기면 손 아래 있는 것이 커지며, 그 위에서 떼면 그 emote 를 보낸다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 끌지 않고 뗐으면 판을 열어 둔다. 끄는 대신 누르기만 하는 사람은 그다음에 아이콘 하나를
    /// 눌러 고를 수 있다. 다른 데서 떼면 아무것도 보내지 않고 닫는다.
    /// </para>
    /// <para>
    /// 무엇을 골랐는지는 뗀 자리를 raycast 해서 정한다. hover 로 정하면 손가락에는 hover 가
    /// 없어 touch 에서 아무것도 고르지 못한다. 그래서 desktop 과 touch 가 같은 동작 하나를
    /// 쓴다.
    /// </para>
    /// </remarks>
    public class EmotePickerUI : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        /// <summary>
        /// 버튼을 잠가 두는 시간. 서버의 3초보다 0.5초 길다.
        /// <para>
        /// 두 시간이 서로 다른 순간에 시작한다. 이쪽은 보내는 순간부터 세고 서버는 받는 순간부터
        /// 세므로, network 지연만큼 이쪽이 먼저 풀린다. 같은 3초로 맞추면 잠금이 풀린 직후에 누른
        /// emote 가 아직 서버의 3초 안에 있어 조용히 버려진다. 서버는 버린 것을 알려주지 않고
        /// 말풍선은 서버가 돌려보낸 뒤에야 뜨므로, 누른 사람에게는 아무 일도 일어나지 않은 것처럼
        /// 보인다. 0.5초는 그 틈을 덮는 여유다. 서버와 같은 값으로 되돌리지 말 것.
        /// </para>
        /// </summary>
        private const float CooldownSeconds = 3.5f;

        /// <summary>EventSystem 이 없을 때 쓰는 끌기 판정 거리. Unity 의 기본값과 같다.</summary>
        private const float FallbackDragThreshold = 10f;

        [SerializeField] private GameObject optionsRoot;
        [SerializeField] private EmoteOptionButton[] options;

        private Button openButton;
        private Coroutine cooldownRoutine;

        private bool pressActive;
        private bool capturedPointer;
        private int activePointerId;
        private Vector2 pressPosition;
        private bool wasOpenBeforePress;
        private EmoteOptionButton highlightedOption;

        private bool IsLocked => cooldownRoutine != null;

        private bool IsOpen => optionsRoot != null && optionsRoot.activeSelf;

        private void Awake()
        {
            openButton = GetComponent<Button>();
            SetOpen(false);

            if (options == null || options.Length != EmoteTypes.All.Length)
            {
                WDebug.LogWarning(
                    $"[Emote] 아이콘 수가 emote 수와 다르다. options: {(options == null ? 0 : options.Length)}, emotes: {EmoteTypes.All.Length}");
            }
        }

        private void OnDisable()
        {
            // 누른 채로 꺼지면 뗌이 오지 않는다. 잡아 둔 누름을 여기서 놓아 주지 않으면
            // 필드 입력이 영영 막힌다.
            ReleasePointerCapture();
            ClearHighlight();
            SetOpen(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            BeginPress(eventData, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            ContinuePress(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            EndPress(eventData, true);
        }

        /// <param name="fromOpenButton">여는 버튼에서 시작한 누름인지. 아이콘에서 시작했으면 거짓이다.</param>
        public void BeginPress(PointerEventData eventData, bool fromOpenButton)
        {
            if (pressActive || IsLocked)
            {
                return;
            }

            pressActive = true;
            activePointerId = eventData.pointerId;
            pressPosition = eventData.position;
            wasOpenBeforePress = IsOpen;

            capturedPointer = true;
            PointerInputUtility.BeginPointerCapture();

            if (fromOpenButton)
            {
                SetOpen(true);
            }

            UpdateHighlight(eventData.position);
        }

        public void ContinuePress(PointerEventData eventData)
        {
            if (!pressActive || eventData.pointerId != activePointerId)
            {
                return;
            }

            UpdateHighlight(eventData.position);
        }

        public void EndPress(PointerEventData eventData, bool fromOpenButton)
        {
            if (!pressActive || eventData.pointerId != activePointerId)
            {
                return;
            }

            pressActive = false;
            ReleasePointerCapture();
            ClearHighlight();

            EmoteOptionButton released = FindOptionAt(eventData.position);
            if (released != null)
            {
                Choose(released.Emote);
                return;
            }

            bool travelled = HasTravelled(eventData.position);
            bool overOpenButton =
                PointerInputUtility.FindUnderPointer<EmotePickerUI>(eventData.position) == this;

            // 여는 버튼 위에서 끌지 않고 뗐다. 처음 누른 것이면 판을 열어 두고, 이미 열려
            // 있었으면 이번 누름은 닫으라는 뜻으로 읽는다.
            if (fromOpenButton && overOpenButton && !travelled)
            {
                SetOpen(!wasOpenBeforePress);
                return;
            }

            SetOpen(false);
        }

        private void Choose(EmoteType emote)
        {
            if (IsLocked)
            {
                return;
            }

            SetOpen(false);

            if (!EmoteInputSender.Send(emote))
            {
                return;
            }

            cooldownRoutine = StartCoroutine(RunCooldown());
        }

        /// <summary>
        /// 잠금은 이 시계 하나로만 풀린다. 서버가 돌려보낸 emote 를 기다리면 그 메시지가
        /// 늦거나 버려졌을 때 버튼이 잠긴 채로 남는다.
        /// </summary>
        private IEnumerator RunCooldown()
        {
            SetInteractable(false);
            yield return new WaitForSeconds(CooldownSeconds);
            SetInteractable(true);
            cooldownRoutine = null;
        }

        private void SetInteractable(bool interactable)
        {
            if (openButton != null)
            {
                openButton.interactable = interactable;
            }

            if (options == null)
            {
                return;
            }

            foreach (EmoteOptionButton option in options)
            {
                if (option != null)
                {
                    option.SetInteractable(interactable);
                }
            }
        }

        private void SetOpen(bool open)
        {
            if (optionsRoot != null)
            {
                optionsRoot.SetActive(open);
            }

            if (!open)
            {
                ClearHighlight();
            }
        }

        private void UpdateHighlight(Vector2 screenPosition)
        {
            EmoteOptionButton option = IsOpen ? FindOptionAt(screenPosition) : null;
            if (option == highlightedOption)
            {
                return;
            }

            if (highlightedOption != null)
            {
                highlightedOption.SetHighlighted(false);
            }

            highlightedOption = option;

            if (highlightedOption != null)
            {
                highlightedOption.SetHighlighted(true);
            }
        }

        private void ClearHighlight()
        {
            if (highlightedOption != null)
            {
                highlightedOption.SetHighlighted(false);
                highlightedOption = null;
            }
        }

        private EmoteOptionButton FindOptionAt(Vector2 screenPosition)
        {
            if (!IsOpen)
            {
                return null;
            }

            EmoteOptionButton option = PointerInputUtility.FindUnderPointer<EmoteOptionButton>(screenPosition);
            return option != null && IsMine(option) ? option : null;
        }

        private bool IsMine(EmoteOptionButton option)
        {
            if (options == null)
            {
                return false;
            }

            foreach (EmoteOptionButton candidate in options)
            {
                if (candidate == option)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasTravelled(Vector2 releasePosition)
        {
            EventSystem eventSystem = EventSystem.current;
            float threshold = eventSystem != null ? eventSystem.pixelDragThreshold : FallbackDragThreshold;
            return (releasePosition - pressPosition).sqrMagnitude > threshold * threshold;
        }

        private void ReleasePointerCapture()
        {
            if (!capturedPointer)
            {
                return;
            }

            capturedPointer = false;
            PointerInputUtility.EndPointerCapture();
        }
    }
}
