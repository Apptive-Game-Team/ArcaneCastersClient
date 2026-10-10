using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TutorialScene
{
    public class TutorialBarController : MonoBehaviour
    {
    
        private RectTransform _rectTransform;
        /// <summary>사용자가 정한 열림 상태. 조준 중 임시로 내려가는 것은 여기에 반영하지 않는다.</summary>
        [FormerlySerializedAs("isActive")]
        [SerializeField] private bool userWantsOpen = false;
        [SerializeField] private Button manaBarButton;
        [SerializeField] private Button fieldButton;
        private bool lastActive = false;

        [SerializeField] private TutorialCardSender cardInputSender;
    
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        
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
            if (active)
            {
                StartCoroutine(MoveBar(true));
            }
            else
            {
                StartCoroutine(MoveBar(false));
            }
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
