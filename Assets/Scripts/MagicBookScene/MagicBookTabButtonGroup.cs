using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MagicBookScene
{
    public class MagicBookTabButtonGroup : MonoBehaviour
    {
        [SerializeField] private Button[] buttons;
        [SerializeField] private int selectedIndex;
        [SerializeField] private bool addClickListeners = true;
        [SerializeField] private Vector2 selectedOffset = new(0f, -8f);
        [SerializeField] private Vector3 selectedScale = new(0.96f, 0.96f, 1f);
        [SerializeField] private Color selectedColor = new(0.78f, 0.78f, 0.78f, 1f);
        // 탭 글자. buttons 와 같은 순서다. 고른 탭 글자는 흰색 + 외곽선 material 로 바꾼다.
        [SerializeField] private TMP_Text[] labels;
        [SerializeField] private Color selectedLabelColor = Color.white;
        [SerializeField] private Material selectedLabelMaterial;

        private Vector2[] defaultAnchoredPositions;
        private Vector3[] defaultScales;
        private Color[] defaultColors;
        private Color[] defaultLabelColors;
        private Material[] defaultLabelMaterials;
        private UnityAction[] clickActions;

        public int SelectedIndex => selectedIndex;

        private void Awake()
        {
            CacheDefaults();

            if (addClickListeners)
            {
                AddButtonListeners();
            }
        }

        private void Start()
        {
            ApplySelection();
        }

        private void OnDestroy()
        {
            if (buttons == null)
            {
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button != null && clickActions != null && i < clickActions.Length && clickActions[i] != null)
                {
                    button.onClick.RemoveListener(clickActions[i]);
                }
            }
        }

        public void Select(int index)
        {
            if (buttons == null || index < 0 || index >= buttons.Length)
            {
                return;
            }

            selectedIndex = index;
            ApplySelection();
        }

        private void CacheDefaults()
        {
            if (buttons == null)
            {
                return;
            }

            defaultAnchoredPositions = new Vector2[buttons.Length];
            defaultScales = new Vector3[buttons.Length];
            defaultColors = new Color[buttons.Length];

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                RectTransform rectTransform = button.transform as RectTransform;
                defaultAnchoredPositions[i] = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
                defaultScales[i] = button.transform.localScale;

                Graphic graphic = GetButtonGraphic(button);
                defaultColors[i] = graphic != null ? graphic.color : Color.white;
            }

            if (labels == null)
            {
                return;
            }

            defaultLabelColors = new Color[labels.Length];
            defaultLabelMaterials = new Material[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null)
                {
                    defaultLabelColors[i] = labels[i].color;
                    defaultLabelMaterials[i] = labels[i].fontSharedMaterial;
                }
            }
        }

        private void AddButtonListeners()
        {
            clickActions = new UnityAction[buttons.Length];

            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                Button button = buttons[index];
                if (button != null)
                {
                    clickActions[index] = () => Select(index);
                    button.onClick.AddListener(clickActions[index]);
                }
            }
        }

        private void ApplySelection()
        {
            if (buttons == null || defaultAnchoredPositions == null)
            {
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                bool isSelected = i == selectedIndex;
                RectTransform rectTransform = button.transform as RectTransform;
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = defaultAnchoredPositions[i] + (isSelected ? selectedOffset : Vector2.zero);
                }

                button.transform.localScale = isSelected ? selectedScale : defaultScales[i];

                Graphic graphic = GetButtonGraphic(button);
                if (graphic != null)
                {
                    graphic.color = isSelected ? selectedColor : defaultColors[i];
                }

                ApplyLabelSelection(i, isSelected);
            }
        }

        private void ApplyLabelSelection(int index, bool isSelected)
        {
            if (labels == null || defaultLabelColors == null || index >= labels.Length || labels[index] == null)
            {
                return;
            }

            TMP_Text label = labels[index];
            label.color = isSelected ? selectedLabelColor : defaultLabelColors[index];
            Material material = isSelected && selectedLabelMaterial != null
                ? selectedLabelMaterial
                : defaultLabelMaterials[index];
            if (material != null)
            {
                label.fontSharedMaterial = material;
            }
        }

        private static Graphic GetButtonGraphic(Button button)
        {
            return button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
        }
    }
}
