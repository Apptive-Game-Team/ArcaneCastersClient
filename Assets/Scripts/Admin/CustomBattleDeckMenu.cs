using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data.Deck;
using DeckScene;
using TMPro;
using UnityEngine;

namespace Admin {
    public sealed class CustomBattleDeckMenu : MonoBehaviour {
        [SerializeField] private TMP_Dropdown human;
        [SerializeField] private TMP_Dropdown bot;
        [Serializable] public sealed class SupportedCard { public long id; public string kind; }
        private SupportedCard[] supported = Array.Empty<SupportedCard>();
        private DeckResponseDto[] decks = Array.Empty<DeckResponseDto>();
        private CardDto[] owned;
        public string Notice { get; private set; } = "덱 정보를 불러오는 중입니다.";
        public long[] HumanDeck => Selected(human);
        public long[] BotDeck => Selected(bot);
        public object[] OwnedCards => owned?.Select(c => (object)new {id=c.id,count=c.count}).ToArray();
        public void SetSupported(SupportedCard[] cards) { supported = cards ?? Array.Empty<SupportedCard>(); }
        public void SetBusy(bool busy) { human.interactable = bot.interactable = !busy && owned != null; }
        private void Awake() { Populate(); SetBusy(true); StartCoroutine(Load()); }
        private IEnumerator Load() {
            var api = new DeckApiClient();
            yield return api.GetOwnedCards(cards => owned = cards);
            yield return api.GetDecks(result => {
                decks = result ?? Array.Empty<DeckResponseDto>();
                Notice = result == null ? "저장 덱 조회 실패: 랜덤만 선택할 수 있습니다." : "";
            });
            if (owned == null) Notice = "보유 카드 조회 실패: 페이지를 다시 열어 주세요.";
            Populate(); SetBusy(false);
        }
        private void Populate() {
            foreach (var pair in new[] { (human, "내 덱"), (bot, "봇 덱") }) {
                pair.Item1.ClearOptions();
                pair.Item1.AddOptions(new List<string> { pair.Item2 + ": 랜덤" }.Concat(decks.Select(d => pair.Item2 + ": " + d.name)).ToList());
                pair.Item1.SetValueWithoutNotify(0);
                pair.Item1.RefreshShownValue();
            }
        }
        private long[] Selected(TMP_Dropdown dropdown) {
            if (dropdown.value == 0) return null;
            return decks[dropdown.value - 1].cards?.Select(c => c.id).ToArray() ?? Array.Empty<long>();
        }
        public string ValidationError() {
            if (owned == null) return "보유 카드 정보를 기다리는 중입니다.";
            var problem = Check(HumanDeck);
            if (problem != null) return "내 덱: " + problem;
            problem = Check(BotDeck);
            return problem == null ? null : "봇 덱: " + problem;
        }
        private string Check(long[] selected) {
            var ids = new HashSet<long>(supported.Select(c => c.id));
            var inventory = owned.GroupBy(c => c.id).ToDictionary(g => g.Key, g => g.First().count);
            if (selected == null) {
                if (inventory.Where(c => ids.Contains(c.Key)).Sum(c => Math.Min(15, Math.Max(0, c.Value))) < 15)
                    return "지원 보유 카드가 15장 미만입니다.";
                var spawn = new HashSet<long>(supported.Where(c => c.kind == "Spawn").Select(c => c.id));
                if (inventory.Where(c => spawn.Contains(c.Key)).Sum(c => Math.Min(15, Math.Max(0, c.Value))) < 3)
                    return "랜덤 덱에는 유닛 소환 카드 3장이 필요합니다.";
                return null;
            }
            if (selected.Length != 15) return "15장 덱이어야 합니다.";
            var unsupported = selected.Where(c => !ids.Contains(c)).Distinct().ToArray();
            if (unsupported.Length > 0) return "모델 미지원: " + string.Join(", ", unsupported.Select(id => owned.FirstOrDefault(c => c.id == id)?.name ?? id.ToString()));
            if (selected.GroupBy(c => c).Any(g => !inventory.ContainsKey(g.Key) || g.Count() > inventory[g.Key]))
                return "보유 수량을 초과한 카드가 있습니다.";
            return null;
        }
    }
}
