using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DeckScene
{
    internal sealed class HoverPopupTransition
    {
        public const float DefaultShowDelaySeconds = 1f;
        public const float DefaultFadeSeconds = 0.25f;
        public const int PopupSortingOrder = 100;

        private readonly MonoBehaviour owner;
        private Coroutine delayCoroutine;
        private Coroutine fadeCoroutine;
        private MonoBehaviour delayRunner;
        private MonoBehaviour fadeRunner;

        public HoverPopupTransition(MonoBehaviour owner)
        {
            this.owner = owner;
        }

        public void ShowAfterDelay(GameObject root, Action beforeShow, float delaySeconds = DefaultShowDelaySeconds, float fadeSeconds = DefaultFadeSeconds)
        {
            HideImmediate(root);

            if (owner == null || root == null)
            {
                return;
            }

            RunAfterDelay(() => ShowNow(root, beforeShow, fadeSeconds), delaySeconds);
        }

        public void RunAfterDelay(Action action, float delaySeconds = DefaultShowDelaySeconds)
        {
            CancelDelay();

            MonoBehaviour runner = GetRunner();
            if (runner == null)
            {
                return;
            }

            delayRunner = runner;
            delayCoroutine = runner.StartCoroutine(DelayRoutine(action, delaySeconds));
        }

        public void ShowNow(GameObject root, Action beforeShow, float fadeSeconds = DefaultFadeSeconds)
        {
            CancelFade();

            if (root == null)
            {
                return;
            }

            root.SetActive(true);
            BringToFront(root, PopupSortingOrder + 1);
            SetAlpha(root, 0f);
            beforeShow?.Invoke();
            MonoBehaviour runner = GetRunner();
            if (runner == null)
            {
                SetAlpha(root, 1f);
                return;
            }

            fadeRunner = runner;
            fadeCoroutine = runner.StartCoroutine(FadeRoutine(root, fadeSeconds));
        }

        public void HideImmediate(GameObject root)
        {
            CancelDelay();
            CancelFade();

            if (root == null)
            {
                return;
            }

            SetAlpha(root, 0f);
            root.SetActive(false);
        }

        // Popups are plain children in the scene hierarchy, so other UI that comes
        // later in sibling order draws over them. A nested Canvas with override
        // sorting lifts the popup above that UI; the raycaster keeps its items hoverable.
        public static void BringToFront(GameObject root, int sortingOrder)
        {
            if (root == null)
            {
                return;
            }

            root.transform.SetAsLastSibling();

            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = root.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            if (root.GetComponent<GraphicRaycaster>() == null)
            {
                root.AddComponent<GraphicRaycaster>();
            }
        }

        private IEnumerator DelayRoutine(Action action, float delaySeconds)
        {
            if (delaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            delayCoroutine = null;
            delayRunner = null;
            action?.Invoke();
        }

        private IEnumerator FadeRoutine(GameObject root, float fadeSeconds)
        {
            if (fadeSeconds <= 0f)
            {
                SetAlpha(root, 1f);
                fadeCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(root, Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }

            SetAlpha(root, 1f);
            fadeCoroutine = null;
            fadeRunner = null;
        }

        private void CancelDelay()
        {
            MonoBehaviour runner = delayRunner;
            if (delayCoroutine == null || runner == null)
            {
                return;
            }

            runner.StopCoroutine(delayCoroutine);
            delayCoroutine = null;
            delayRunner = null;
        }

        private void CancelFade()
        {
            MonoBehaviour runner = fadeRunner;
            if (fadeCoroutine == null || runner == null)
            {
                return;
            }

            runner.StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
            fadeRunner = null;
        }

        private MonoBehaviour GetRunner()
        {
            return owner != null && owner.isActiveAndEnabled
                ? owner
                : HoverPopupTransitionRunner.Instance;
        }

        private static void SetAlpha(GameObject root, float alpha)
        {
            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = root.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = alpha;
        }

        private sealed class HoverPopupTransitionRunner : MonoBehaviour
        {
            private static HoverPopupTransitionRunner instance;

            public static HoverPopupTransitionRunner Instance
            {
                get
                {
                    if (instance != null)
                    {
                        return instance;
                    }

                    var runnerObject = new GameObject(nameof(HoverPopupTransitionRunner));
                    UnityEngine.Object.DontDestroyOnLoad(runnerObject);
                    instance = runnerObject.AddComponent<HoverPopupTransitionRunner>();
                    return instance;
                }
            }
        }
    }
}
