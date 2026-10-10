using System.Collections;
using GameScene.Card;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameScene
{
    public class BarController : MonoBehaviour
    {
    
        private RectTransform _rectTransform;
        /// <summary>사용자가 정한 열림 상태. 조준 중 임시로 내려가는 것은 여기에 반영하지 않는다.</summary>
        [FormerlySerializedAs("isActive")]
        [SerializeField] private bool userWantsOpen = false;
        [SerializeField] private Button manaBarButton;
        [SerializeField] private Button fieldButton;
        private bool lastActive = false;
        private Coroutine _moveBarCoroutine;

        /// <summary>훈수 시스템이 마나 바를 띄웠는지 판단할 때 읽는다. 사용자가 정한 상태를 돌려준다.</summary>
        public bool IsBarOpen => userWantsOpen;

        /// <summary>화면에 올라와 있어야 하는 상태. 조준 중이거나 응답을 기다리는 동안은 내려간다.</summary>
        private bool IsBarShown()
        {
            CardInputSender sender = CardInputSender.Instance;
            return userWantsOpen
                   && (sender == null
                       || (!sender.IsFieldSelectMode() && !sender.IsWaitingInputResponse()));
        }

        /// <summary>훈수 시스템이 강조할 마나 바 버튼.</summary>
        public Transform ManaBarButtonTransform => manaBarButton != null ? manaBarButton.transform : null;
    
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        
            manaBarButton.onClick.AddListener(OnManaBarButtonClicked);
            fieldButton.onClick.AddListener(() =>
            {
                userWantsOpen = false;
            });
        }

        /// <summary>
        /// 마법을 고른 상태에서는 마나 바 버튼이 취소로 동작한다. 터치에는 우클릭이 없어
        /// CardInputSender 의 우클릭 취소를 쓸 수 없다.
        /// </summary>
        private void OnManaBarButtonClicked()
        {
            CardInputSender sender = CardInputSender.Instance;
            if (sender != null && sender.IsFieldSelectMode())
            {
                sender.Cancel();
                return;
            }

            ToggleBar();
        }

        /// <summary>
        /// 사용자가 정한 열림 상태를 올림과 내림 사이에서 바꾼다. 마나 바 버튼과 스페이스 키가
        /// 같은 경로를 탄다. 화면에 보이는 상태는 Update 가 계산한다.
        /// </summary>
        public void ToggleBar()
        {
            userWantsOpen = !userWantsOpen;
        }

        private void Update()
        {
            bool shown = IsBarShown();
            fieldButton.gameObject.SetActive(shown);

            if (lastActive != shown)
            {
                lastActive = shown;
                SetBarActive(shown);
            }
        }

        private void SetBarActive(bool active)
        {
            if (_moveBarCoroutine != null)
            {
                StopCoroutine(_moveBarCoroutine);
            }
            _moveBarCoroutine = StartCoroutine(MoveBar(active));
        }


    
        private IEnumerator MoveBar(bool up, float duration = 0.5f)
        {
        
            Vector2 startPosition = _rectTransform.anchoredPosition;
            Vector2 endPosition = up ? 
                new Vector2(0, 540) : 
                new Vector2(0, 240);
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                _rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _rectTransform.anchoredPosition = endPosition; // Ensure final position is set
        }
    }
}
