using Global.Sound.BGM;
using UnityEngine;

namespace GameScene.Object
{
    /// <summary>
    /// Attached at spawn to an object the server marked <c>boss: true</c>. While at least one such
    /// object is alive, the BGM is the scene's boss track; when the last one is destroyed it returns
    /// to the scene default. Without a boss clip on <see cref="BGMClipContainer"/> nothing changes.
    /// </summary>
    public sealed class BossBgmTrigger : MonoBehaviour
    {
        private static int activeBosses;

        public static BossBgmTrigger Attach(GameObject target)
        {
            BossBgmTrigger existing = target.GetComponent<BossBgmTrigger>();
            return existing != null ? existing : target.AddComponent<BossBgmTrigger>();
        }

        private void OnEnable()
        {
            activeBosses++;
            if (activeBosses != 1 || BGMPlayer.Instance == null)
            {
                return;
            }

            BGMClipContainer container = FindObjectOfType<BGMClipContainer>();
            BGMPlayer.Instance.PlayOverride(container != null ? container.GetBossClip() : null);
        }

        private void OnDisable()
        {
            activeBosses = Mathf.Max(0, activeBosses - 1);
            if (activeBosses == 0 && BGMPlayer.Instance != null)
            {
                BGMPlayer.Instance.RestoreSceneDefault();
            }
        }
    }
}
