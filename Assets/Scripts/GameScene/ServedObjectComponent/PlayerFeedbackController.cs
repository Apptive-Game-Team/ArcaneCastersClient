using System.Linq;
using GameScene.Player;
using GameScene.ServedObjectComponent.OnAttack;
using Global;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    public class PlayerFeedbackController : LocalSingletonObject<PlayerFeedbackController> 
    {
    
        private GameObject playerObject;

        public GameObject PlayerObject
        {
            get
            {
                if (playerObject == null)
                {
                    playerObject = GetPlayerObject();
                }
                return playerObject;
            }
        }
    
        private GameObject GetPlayerObject()
        {
            return FindObjectsByType<PlayerNameSetter>(FindObjectsSortMode.None)
                .Where(o => o.GetComponent<ServedObject>() != null)
                .Select(o => o.GetComponent<ServedObject>())
                .Where(s => s.GetMaster().Equals(SceneContext.Me))
                .Select(s => s.gameObject)
                .FirstOrDefault();
        }

        public void PlayCardSelectFeedback()
        {
            if (PlayerObject != null
                && PlayerObject.TryGetComponent(out PlayerStaffPoseController pose)
                && pose.UsesFrameAnimation)
            {
                return;
            }

            Transform spriteTr = GetPlayerVisualTransform();
            if (spriteTr == null)
            {
                Debug.LogWarning("[PlayerFeedbackController] Could not find player visual for card select feedback.");
                return;
            }

            DOTweenAction.BouncePlayerUseCard(spriteTr);
        }

        private Transform GetPlayerVisualTransform()
        {
            if (PlayerObject == null)
            {
                return null;
            }

            ServedObject servedObject = PlayerObject.GetComponent<ServedObject>();
            return servedObject != null ? servedObject.GetActualTransform() : null;
        }
    }
}
