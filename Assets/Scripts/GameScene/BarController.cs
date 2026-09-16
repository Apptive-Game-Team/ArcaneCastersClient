using System.Collections;
using GameScene.Card;
using UnityEngine;
using UnityEngine.UI;

namespace GameScene
{
    public class BarController : MonoBehaviour
    {
    
        private RectTransform _rectTransform;
        [SerializeField] private bool isActive = false;
        [SerializeField] private Button manaBarButton;
        [SerializeField] private Button fieldButton;
        private bool lastActive = false;
        private Coroutine _moveBarCoroutine;

        /// <summary>훈수 시스템이 마나 바를 띄웠는지 판단할 때 읽는다.</summary>
        public bool IsBarOpen => isActive;

        /// <summary>훈수 시스템이 강조할 마나 바 버튼.</summary>
        public Transform ManaBarButtonTransform => manaBarButton != null ? manaBarButton.transform : null;
    
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        
            manaBarButton.onClick.AddListener(OnManaBarButtonClicked);
            fieldButton.onClick.AddListener(() =>
            {
                isActive = false;
            });
        }

        /// <summary>
        /// 마법을 고른 상태에서는 마나 바 버튼이 취소로 동작한다. 터치에는 우클릭이 없어
        /// CardInputSender 의 우클릭 취소를 쓸 수 없고, 그 상태에서는 Update 가 마나 바를
        /// 도로 내리기 때문에 이 버튼을 눌러도 아무 일이 없었다.
        /// </summary>
        private void OnManaBarButtonClicked()
        {
            CardInputSender sender = CardInputSender.Instance;
            if (sender != null && sender.IsFieldSelectMode())
            {
                sender.Cancel();
                return;
            }

            TryOpenBar();
        }

        /// <summary>
        /// 마나 바를 올린다. 이미 올라와 있거나 지금 올릴 수 없는 상태면 아무것도 하지 않고 false.
        /// 마나 바 버튼과 스페이스 키가 같은 경로를 타도록 여기에 모아 둔다.
        /// </summary>
        public bool TryOpenBar()
        {
            if (isActive || !CanOpenBar())
            {
                return false;
            }

            isActive = true;
            return true;
        }

        /// <summary>Update가 마나 바를 도로 내리는 상태에서는 올리지 않는다.</summary>
        private static bool CanOpenBar()
        {
            CardInputSender sender = CardInputSender.Instance;
            return sender == null
                   || (!sender.IsFieldSelectMode() && !sender.IsWaitingInputResponse());
        }

        private void Update()
        {
            if (CardInputSender.Instance.IsFieldSelectMode() || CardInputSender.Instance.IsWaitingInputResponse())
            {
                isActive = false;
            }
        
            fieldButton.gameObject.SetActive(isActive);
        
            if (lastActive != isActive)
            {
                lastActive = isActive;
                SetBarActive(isActive);
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
