using System;
using Data;
using Data.Localization;
using Data.Magic;
using Data.Util;
using UnityEngine;
using UnityEngine.UI;

namespace MagicBookScene
{
    public class MagicButton : MonoBehaviour
    {
        [SerializeField] private Image magicImage;
        // 고른 마법 칸에만 켜는 금색 테두리.
        [SerializeField] private GameObject selectedRing;
        
        private CombinedMagicData data;
        public event Action<CombinedMagicData> OnClick;

        private Button button;

        public CombinedMagicData Data => data;
        
        private void OnButtonClick()
        {
            OnClick?.Invoke(data);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }
        
        public void SetActive(bool active)
        {
            button ??= GetComponent<Button>();
            button.interactable = active;
            magicImage.color = active ? Color.white : new Color(0, 0, 0, 0.6f);
        }

        public void SetSelected(bool selected)
        {
            if (selectedRing != null)
            {
                selectedRing.SetActive(selected);
            }
        }

        public void Init(CombinedMagicData data)
        {
            button ??= GetComponent<Button>();
            button.onClick.RemoveListener(OnButtonClick);
            button.onClick.AddListener(OnButtonClick);
            this.data = data;
            magicImage.sprite = data.GetSprite();
        }
    }
}
