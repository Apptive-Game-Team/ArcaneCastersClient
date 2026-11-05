using Coach;
using Global.Coach;
using LobbyScene.Button;
using UnityEngine;

namespace LobbyScene.Coach
{
    /// <summary>
    /// 로비에서 아무것도 하지 않고 앉아 있고 매칭 큐에도 없다. 그대로 세워 두는 대신
    /// 봇 전투 쪽으로 밀어 준다.
    /// </summary>
    public class LobbyIdleRule : ICoachRule
    {
        private PracticeButton practiceButton;
        private Vector3 lastMousePosition;
        private bool hasMouseSample;

        public CoachRuleId Id => CoachRuleId.LobbyIdle;

        public string MessageKey => "coach.lobbyIdle";

        public int Priority => 6;

        public float DwellSeconds => 45f;

        public int MaxShowsPerSession => 2;

        public bool UseAlternatePlacement => true;

        public bool IsActive()
        {
            if (IsMatching())
            {
                return false;
            }

            return !HasInputThisFrame();
        }

        public Transform[] ResolveTargets()
        {
            if (practiceButton == null)
            {
                practiceButton = UnityEngine.Object.FindObjectOfType<PracticeButton>();
            }

            return practiceButton != null ? new[] { practiceButton.transform } : null;
        }

        public void OnShown()
        {
        }

        public void OnHidden()
        {
        }

        private static bool IsMatching()
        {
            LobbySceneViewModel viewModel = LobbySceneViewModel.Instance;
            return viewModel != null
                   && viewModel.CurrentState.Data != LobbySceneViewModel.LobbyState.Idle;
        }

        /// <summary>
        /// 마우스 이동도 활동으로 친다. 덱 드롭다운을 읽고 있는 유저가 키를 안 눌렀다고
        /// 놀고 있는 것은 아니다.
        /// </summary>
        private bool HasInputThisFrame()
        {
            Vector3 mousePosition = Input.mousePosition;
            bool moved = hasMouseSample && mousePosition != lastMousePosition;

            lastMousePosition = mousePosition;
            hasMouseSample = true;

            return moved || Input.anyKeyDown;
        }
    }
}
