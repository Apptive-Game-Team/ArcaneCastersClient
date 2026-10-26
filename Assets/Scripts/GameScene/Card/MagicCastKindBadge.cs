using Data.Magic;
using UnityEngine;
using UnityEngine.UI;

namespace GameScene.Card {
    public class MagicCastKindBadge : MonoBehaviour {
        [SerializeField] private Image icon;
        [SerializeField] private Sprite unitIcon;
        [SerializeField] private Sprite buildingIcon;
        [SerializeField] private Sprite dropIcon;
        [SerializeField] private Sprite explosionIcon;
        [SerializeField] private Sprite shotIcon;

        public void Bind(MagicCastKind kind) {
            icon.sprite = kind switch {
                MagicCastKind.Unit => unitIcon,
                MagicCastKind.Building => buildingIcon,
                MagicCastKind.Drop => dropIcon,
                MagicCastKind.Explosion => explosionIcon,
                MagicCastKind.Shot => shotIcon,
                _ => null,
            };
            gameObject.SetActive(icon.sprite != null);
        }
    }
}
