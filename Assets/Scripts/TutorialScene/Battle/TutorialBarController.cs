using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TutorialScene
{
    public class TutorialBarController : MonoBehaviour
    {
    
        private const float BarUpY = 540f;
        private const float BarDownY = 240f;

        private RectTransform _rectTransform;
        /// <summary>사용자가 정한 열림 상태. 조준 중 임시로 내려가는 것은 여기에 반영하지 않는다.</summary>
        [FormerlySerializedAs("isActive")]
        [SerializeField] private bool userWantsOpen = false;
        [SerializeField] private Button manaBarButton;
        [SerializeField] private Button fieldButton;
        private bool lastActive = false;
        private Coroutine _moveBarCoroutine;

        [SerializeField] private TutorialCardSender cardInputSender;
    
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            // 씬에는 마나 바가 올라간 위치(y 540)로 저장되어 있고 userWantsOpen 은 false 다.
            // lastActive 가 false 인 채로 시작하면 Update 가 이미 내려가 있다고 보고 움직이지
            // 않아, 마나 바가 올라간 채로 남고 카드를 골라 조준할 때도 내려가지 않았다.
            // 시작할 때 위치를 userWantsOpen 에 맞춰 둔다.
            lastActive = userWantsOpen;
            _rectTransform.anchoredPosition = new Vector2(0, userWantsOpen ? BarUpY : BarDownY);
        
            manaBarButton.onClick.AddListener(() =>
            {
                // 마법을 고른 상태에서는 취소로 동작한다. 터치에는 우클릭이 없어
                // TutorialCardSender 의 우클릭 취소를 쓸 수 없다.
                if (cardInputSender.IsFieldSelectMode())
                {
                    cardInputSender.Cancel();
                    return;
                }

                userWantsOpen = !userWantsOpen;
            });
            fieldButton.onClick.AddListener(() =>
            {
                userWantsOpen = false;
            });
        }

        private void Update()
        {
            bool shown = userWantsOpen && !cardInputSender.IsFieldSelectMode();
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
            Vector2 endPosition = new Vector2(0, up ? BarUpY : BarDownY);
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
