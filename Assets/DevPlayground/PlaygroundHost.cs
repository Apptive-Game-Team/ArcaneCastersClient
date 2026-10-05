using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DevPlayground {
    // Define-constrained assembly: usable in Editor Play Mode, absent in every player build.
    // The Editor bridge owns game-specific logic without referencing Assembly-CSharp here.
    public sealed class PlaygroundHost : MonoBehaviour {
        public TMP_Dropdown sideDropdown;
        public TMP_Text statusText;
        public TMP_Text timerText;
        public TMP_Text targetText;
        public RectTransform iconContent;
        public Button iconTemplate;
        public Button clearAllyButton;
        public Button clearEnemyButton;
        public Button clearAllButton;
        public Button immuneAllyButton;
        public Button immuneEnemyButton;
        public Button closeButton;

        public static Action<PlaygroundHost> Configure;
        public Func<IEnumerator> OnStart;
        public Action OnUpdate;
        public Action OnDestroyed;
        private void Awake() => Configure?.Invoke(this);
        private IEnumerator Start() => OnStart?.Invoke();
        private void Update() => OnUpdate?.Invoke();
        private void OnDestroy() => OnDestroyed?.Invoke();
    }
}
