using System.Collections.Generic;
using Data;
using Data.GameConfig;
using Data.Localization;
using Data.Magic;
using GameScene.Card;
using Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MagicBookScene
{
    /// <summary>
    /// 도감 오른쪽 카드. 이름, 마나 칩, 원소 칩, 능력치 줄, 설명을 채운다.
    /// 칩과 능력치 줄은 씬에 꺼 둔 template 를 복제해 만든다.
    /// </summary>
    public class MagicInfo : MonoBehaviour
    {
        private const string MagicBookTable = "MagicBook";
        private const string ElementTable = "Element";
        private const string ManaKey = "filter.manaCost";
        private const string ManaFallback = "Mana";

        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Transform cardsParent;
        [SerializeField] private TMP_Text statsText;

        [SerializeField] private CardImageMapper mapper;

        // 마법을 고르기 전에는 빈 칩이 보이지 않도록 씬에서 꺼 두고 Init 에서 켠다.
        [SerializeField] private GameObject manaChip;
        [SerializeField] private TMP_Text manaText;
        [SerializeField] private MagicInfoChip elementChipTemplate;
        [SerializeField] private Transform statRowsParent;
        [SerializeField] private MagicStatRow statRowTemplate;
        [SerializeField] private MagicPreview previewPrefab;

        private readonly List<GameObject> spawned = new();
        private int initVersion;

        public async void Init(CombinedMagicData data)
        {
            int version = ++initVersion;
            // Stop the previous replay as soon as selection changes, even while text loads.
            foreach (GameObject spawnedObject in spawned)
            {
                if (spawnedObject != null && spawnedObject.GetComponent<MagicPreview>() != null)
                    spawnedObject.SetActive(false);
            }

            string magicName = await LocaleUtils.GetStringAsync("Magic", data.localizationKey);
            string manaWord = await GetText(MagicBookTable, ManaKey, ManaFallback);
            var elementNames = new List<string>();
            if (data.elements != null)
            {
                foreach (ElementType element in data.elements)
                {
                    elementNames.Add(await GetText(ElementTable, element.ToString(), element.ToString()));
                }
            }

            string description = await GetDescriptionAsync(data);

            // 결과를 기다리는 사이 다른 마법을 골랐다면 늦게 온 이 결과는 버린다.
            if (version != initVersion || this == null)
            {
                return;
            }

            // 새 결과가 다 모인 뒤에 한 번에 바꿔서 카드가 빈 채로 깜박이지 않게 한다.
            ClearSpawned();
            nameText.text = magicName;
            if (manaText != null)
            {
                manaText.text = $"{data.manaCost} {manaWord}";
            }

            if (manaChip != null)
            {
                manaChip.SetActive(true);
            }

            if (data.elements != null)
            {
                for (int i = 0; i < data.elements.Count; i++)
                {
                    SpawnElementChip(data.elements[i], elementNames[i]);
                }
            }

            SpawnStatRows(GameParameterResolver.GetMagicDisplayStats(data));

            if (statsText != null)
            {
                statsText.text = description;
                statsText.gameObject.SetActive(!string.IsNullOrWhiteSpace(description));
                // 복제한 능력치 줄 뒤에 설명이 오도록 맨 끝으로 보낸다.
                statsText.transform.SetAsLastSibling();
            }

            if (previewPrefab != null && statRowsParent != null && previewPrefab.Supports(data))
            {
                MagicPreview preview = Instantiate(previewPrefab, statRowsParent);
                preview.Configure(data);
                preview.SetViewZoom(1.35f);
                preview.name = "MagicExplanationPreview";
                LayoutElement layout = preview.GetComponent<LayoutElement>();
                layout.minHeight = 420f;
                layout.preferredHeight = 420f;
                preview.transform.SetAsLastSibling();
                preview.gameObject.SetActive(true);
                spawned.Add(preview.gameObject);
            }
        }

        private void SpawnElementChip(ElementType element, string elementName)
        {
            Sprite elementSprite = mapper != null ? mapper.GetElementImage(element) : null;
            if (elementSprite == null || elementChipTemplate == null || cardsParent == null)
            {
                // None 이나 mapper 가 모르는 원소는 자리 없이 건너뛴다.
                return;
            }

            MagicInfoChip chip = Instantiate(elementChipTemplate, cardsParent);
            chip.name = element.ToString();
            chip.Set(elementSprite, elementName, GetElementChipColor(element));
            chip.gameObject.SetActive(true);
            spawned.Add(chip.gameObject);
        }

        /// <summary>
        /// GameParameterResolver 는 "이름: 값" 줄을 오브젝트마다 빈 줄로 나눠 준다.
        /// 한 줄이 능력치 한 칸이 된다.
        /// </summary>
        private void SpawnStatRows(string stats)
        {
            if (string.IsNullOrWhiteSpace(stats) || statRowTemplate == null || statRowsParent == null)
            {
                return;
            }

            foreach (string line in stats.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                int separator = line.IndexOf(": ", System.StringComparison.Ordinal);
                string label = separator >= 0 ? line.Substring(0, separator) : line;
                string value = separator >= 0 ? line.Substring(separator + 2) : string.Empty;

                MagicStatRow row = Instantiate(statRowTemplate, statRowsParent);
                row.Set(label, value);
                row.gameObject.SetActive(true);
                spawned.Add(row.gameObject);
            }
        }

        private void ClearSpawned()
        {
            foreach (GameObject spawnedObject in spawned)
            {
                if (spawnedObject != null)
                {
                    spawnedObject.SetActive(false);
                    Destroy(spawnedObject);
                }
            }

            spawned.Clear();
        }

        /// <summary>
        /// MagicBook 표는 마법의 snake_case 이름 하나로만 키를 잡는다.
        /// 모든 마법이 설명을 갖는 것은 아니므로 없으면 빈 문자열이다.
        /// </summary>
        private static async System.Threading.Tasks.Task<string> GetDescriptionAsync(CombinedMagicData data)
        {
            string key = data.textLocalizationKey;
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string description = await LocaleUtils.GetStringAsync(MagicBookTable, key);
            return !string.IsNullOrWhiteSpace(description) && description != key
                ? description
                : string.Empty;
        }

        private static async System.Threading.Tasks.Task<string> GetText(string table, string key, string fallback)
        {
            string text = await LocaleUtils.GetStringAsync(table, key);
            return string.IsNullOrWhiteSpace(text) || text == key ? fallback : text;
        }

        // 목업은 물 칩만 #CFE6FF 로 칠했다. 나머지 원소는 같은 밝기로 원소 색을 옅게 맞춘다.
        private static Color GetElementChipColor(ElementType element)
        {
            return element switch
            {
                ElementType.Fire => new Color32(0xFF, 0xD9, 0xC2, 0xFF),
                ElementType.Water => new Color32(0xCF, 0xE6, 0xFF, 0xFF),
                ElementType.Nature => new Color32(0xD6, 0xF2, 0xC8, 0xFF),
                ElementType.Lightning => new Color32(0xFF, 0xF1, 0xB8, 0xFF),
                ElementType.Rock => new Color32(0xE6, 0xDC, 0xCF, 0xFF),
                ElementType.Wind => new Color32(0xD9, 0xF2, 0xEC, 0xFF),
                _ => new Color32(0xEE, 0xF3, 0xF8, 0xFF),
            };
        }
    }
}
