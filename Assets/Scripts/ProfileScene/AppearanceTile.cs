using System;
using Data.Appearances;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProfileScene
{
    /// <summary>
    /// One appearance in the picker: the idle pose as a preview, the name, a lock over appearances the
    /// user does not own and a gold ring over the one currently picked.
    /// </summary>
    public class AppearanceTile : MonoBehaviour
    {
        [SerializeField] private Image previewImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject lockMark;
        [SerializeField] private GameObject selectedMark;
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private Color lockedPreviewColor = new Color(0.25f, 0.25f, 0.3f, 1f);

        private AppearanceDto appearance;
        private Action<AppearanceDto> onPicked;

        public string Key => appearance?.key;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<UnityEngine.UI.Button>();
            }

            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }
        }

        public void Bind(AppearanceDto appearanceDto, string displayName, Sprite preview, Action<AppearanceDto> picked)
        {
            appearance = appearanceDto;
            onPicked = picked;

            bool selectable = AppearanceCatalog.CanSelect(appearanceDto);
            if (previewImage != null)
            {
                previewImage.sprite = preview;
                previewImage.enabled = preview != null;
                previewImage.preserveAspect = true;
                previewImage.color = selectable ? Color.white : lockedPreviewColor;
            }

            if (nameText != null)
            {
                nameText.text = displayName ?? string.Empty;
            }

            if (lockMark != null)
            {
                lockMark.SetActive(!selectable);
            }

            if (button != null)
            {
                button.interactable = selectable;
            }

            SetPicked(false);
        }

        public void SetPicked(bool picked)
        {
            if (selectedMark != null)
            {
                selectedMark.SetActive(picked);
            }
        }

        private void OnClick()
        {
            if (AppearanceCatalog.CanSelect(appearance))
            {
                onPicked?.Invoke(appearance);
            }
        }
    }
}
