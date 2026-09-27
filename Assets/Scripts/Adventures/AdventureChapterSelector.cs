using System.Collections.Generic;
using Data.Adventures;
using Data.Adventures.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Adventures
{
    /// <summary>
    /// Top-right segmented control listing every adventure the server returns:
    /// the current chapter highlighted orange, locked ones greyed. Selecting an
    /// unlocked chapter switches <see cref="CurrentAdventure"/> and tells
    /// <see cref="AdventureMapController"/> to rebuild the map in place, without
    /// leaving this scene.
    /// </summary>
    public class AdventureChapterSelector : MonoBehaviour
    {
        private static readonly Color SelectedColor = new Color(1f, 0.6039216f, 0.12156863f); // #FF9A1F
        private static readonly Color UnselectedColor = Color.white;
        private static readonly Color SelectedTextColor = Color.white;
        private static readonly Color UnlockedTextColor = new Color(0.10980392f, 0.101960786f, 0.16862746f); // ink
        private static readonly Color LockedTextColor = new Color(0.6039216f, 0.6392157f, 0.70588237f); // grey-text

        private readonly struct Segment
        {
            public readonly Adventure Adventure;
            public readonly Image Background;
            public readonly TMP_Text Label;

            public Segment(Adventure adventure, Image background, TMP_Text label)
            {
                Adventure = adventure;
                Background = background;
                Label = label;
            }
        }

        // The container has no Unity layout component (its exact package script GUID
        // could not be verified without the Editor), so segments are packed by hand:
        // left to right, each one sized to its own label, and the container's own
        // width grows to match. The container is anchored to the screen's top-right
        // corner with its pivot there too, so the extra width extends leftward.
        private const float ContainerPadding = 6f;
        private const float SegmentGap = 6f;
        private const float SegmentTextPadding = 28f;
        private const float MinSegmentWidth = 100f;

        [SerializeField] private AdventureDataSource dataSource;
        [SerializeField] private AdventureMapController mapController;
        [SerializeField] private RectTransform segmentsContainer;
        [SerializeField] private GameObject segmentPrefab;

        private readonly List<Segment> segments = new List<Segment>();

        private void Start()
        {
            dataSource.GetAdventures(Populate);
        }

        private void Populate(List<Adventure> adventures)
        {
            foreach (Transform child in segmentsContainer)
            {
                Destroy(child.gameObject);
            }
            segments.Clear();

            Adventure current = CurrentAdventure.Instance != null ? CurrentAdventure.Instance.Adventure : null;
            long currentId = current != null ? current.Id : -1;

            float x = ContainerPadding;
            foreach (Adventure adventure in adventures)
            {
                GameObject segmentObject = Instantiate(segmentPrefab, segmentsContainer);
                var rect = (RectTransform)segmentObject.transform;
                Image background = segmentObject.GetComponent<Image>();
                TMP_Text label = segmentObject.GetComponentInChildren<TMP_Text>();
                Button button = segmentObject.GetComponent<Button>();

                label.text = adventure.Name;
                label.ForceMeshUpdate();
                float width = Mathf.Max(MinSegmentWidth, label.preferredWidth + SegmentTextPadding * 2f);
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                rect.anchoredPosition = new Vector2(x, 0f);
                x += width + SegmentGap;

                bool isLocked = adventure.State == State.INACTIVE;
                button.interactable = !isLocked;
                button.onClick.AddListener(() => Select(adventure));

                segments.Add(new Segment(adventure, background, label));
            }

            float totalWidth = adventures.Count > 0 ? x - SegmentGap + ContainerPadding : ContainerPadding * 2f;
            segmentsContainer.sizeDelta = new Vector2(totalWidth, segmentsContainer.sizeDelta.y);

            Repaint(currentId);
        }

        private void Select(Adventure adventure)
        {
            CurrentAdventure.Instance.SetAdventure(adventure);
            mapController.Show(adventure);
            Repaint(adventure.Id);
        }

        private void Repaint(long selectedId)
        {
            foreach (Segment segment in segments)
            {
                bool isSelected = segment.Adventure.Id == selectedId;
                bool isLocked = segment.Adventure.State == State.INACTIVE;
                segment.Background.color = isSelected ? SelectedColor : UnselectedColor;
                segment.Label.color = isSelected ? SelectedTextColor : (isLocked ? LockedTextColor : UnlockedTextColor);
            }
        }
    }
}
