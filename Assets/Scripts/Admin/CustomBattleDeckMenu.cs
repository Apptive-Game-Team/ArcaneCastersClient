using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data.Deck;
using Data.Magic;
using Global;
using DeckScene;
using TMPro;
using UnityEngine;

namespace Admin {
    public sealed class CustomBattleDeckMenu : MonoBehaviour {
        [SerializeField] private TMP_Dropdown human;
        [SerializeField] private TMP_Dropdown bot;
        [Serializable] public sealed class SupportedCard { public long id; public string kind; public string name; public float cost; }
        public LocalMagicIdMap IdMap { get; private set; } = new();
        private SupportedCard[] supported = Array.Empty<SupportedCard>();
        private DeckResponseDto[] decks = Array.Empty<DeckResponseDto>();
        private CardDto[] owned;
        private string fixedBotDeckName;
        private bool isBusy;
        public string Notice { get; private set; } = "덱 정보를 불러오는 중입니다.";
        public long[] HumanDeck => Selected(human)?.Select(IdMap.Engine).ToArray();
        public long[] BotDeck => fixedBotDeckName != null ? null : Selected(bot)?.Select(IdMap.Engine).ToArray();
        public object[] OwnedCards => owned?.Where(c => supported.Any(s => s.id == c.id)).Select(c => (object)new {id=IdMap.Engine(c.id),count=c.count}).ToArray();
        public void SetSupported(SupportedCard[] cards) {
            var map=new LocalMagicIdMap();
            var compatible=new List<SupportedCard>();
            var catalog=MagicInfoDataSource.GetCachedMagics();
            if (catalog != null) foreach (var model in cards ?? Array.Empty<SupportedCard>()) {
                var matches=catalog.Where(c => c.name==model.name).ToArray();
                if (matches.Length!=1 || string.IsNullOrEmpty(model.name)) continue;
                var current=matches[0];
                map.Add(current.id,model.id); // Stable name identity is needed for opponent casts too.
                if (current.manaCost!=model.cost || current.castKind!=model.kind) continue;
                compatible.Add(new SupportedCard {id=current.id,kind=model.kind,name=model.name,cost=model.cost});
            }
            supported=compatible.ToArray(); IdMap=map;
        }
        public void SetBusy(bool busy) {
            isBusy = busy;
            human.interactable = !busy && owned != null;
            bot.interactable = !busy && owned != null && fixedBotDeckName == null;
        }
        public void SetFixedBotDeck(string name) {
            name = string.IsNullOrWhiteSpace(name) ? null : name;
            if (fixedBotDeckName != name) {
                fixedBotDeckName = name;
                PopulateBot();
            }
            SetBusy(isBusy);
        }
        private void PopulateBot() {
            bot.ClearOptions();
            bot.AddOptions(fixedBotDeckName != null
                ? new List<string> { "봇 덱: " + fixedBotDeckName + " (고정)" }
                : new List<string> { "봇 덱: 랜덤" }.Concat(decks.Select(d => "봇 덱: " + d.name)).ToList());
            bot.SetValueWithoutNotify(0);
            bot.RefreshShownValue();
        }
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
            foreach (var pair in new[] { (human, "내 덱") }) {
                pair.Item1.ClearOptions();
                pair.Item1.AddOptions(new List<string> { pair.Item2 + ": 랜덤" }.Concat(decks.Select(d => pair.Item2 + ": " + d.name)).ToList());
                pair.Item1.SetValueWithoutNotify(0);
                pair.Item1.RefreshShownValue();
            }
            PopulateBot();
        }
        private long[] Selected(TMP_Dropdown dropdown) {
            if (dropdown.value == 0) return null;
            return decks[dropdown.value - 1].cards?.Select(c => c.id).ToArray() ?? Array.Empty<long>();
        }
        public string ValidationError() {
            if (owned == null) return "보유 카드 정보를 기다리는 중입니다.";
            var problem = Check(Selected(human));
            if (problem != null) return "내 덱: " + problem;
            if (fixedBotDeckName != null) return null;
            problem = Check(Selected(bot));
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
            if (unsupported.Length > 0) return "모델 카드 ID 불일치(버전 차이 또는 미지원): " + string.Join(", ", unsupported.Select(id => owned.FirstOrDefault(c => c.id == id)?.name ?? id.ToString()));
            if (selected.GroupBy(c => c).Any(g => !inventory.ContainsKey(g.Key) || g.Count() > inventory[g.Key]))
                return "보유 수량을 초과한 카드가 있습니다.";
            return null;
        }
    }
}
