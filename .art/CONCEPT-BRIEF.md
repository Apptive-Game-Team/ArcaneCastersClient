# Concept Art Brief

Prompts and generated outcomes for the project's image-generation workflow.
Current comparison boards are published through the project art-direction Site.

## 비전 보병 C안 선택 — 2026-10-26

사용자는 `.art/concept/arcane-infantry-restyle-20261026/C-arcane-blades.png`를
선택했다. 넓은 어깨와 전완 장갑, 양손의 짧은 푸른 마력 칼날을 적용한다.
이후 걷기 두 프레임도 요청했다. 기본·공격과 걷기 두 장을 함께 적용하며,
master-v2 스타일 자체는 바꾸지 않는다. A 헬멧·B 후드 안은 비교용으로 남긴다.

두 장을 함께 요청해도 생성기가 같은 발 자세를 반복할 수 있다. 이번 걷기는
한 발을 든 프레임에서 반대 발을 드는 단일 프레임을 별도로 생성했다.
걷는 발의 위치로 정렬하면 몸통도 좌우로 흔들리므로 머리 중심과 지지하는 발의
바닥을 기준으로 정규화했다. 출력과 측정값은 같은 concept 폴더에 둔다.

## How concept art is used

| Artifact | Role | Fed to the generator as an image reference? |
|---|---|---|
| `.art/anchors/master-v2/*.png` | Defines *how* to draw | **Yes** — same format as the output: transparent, single subject |
| `.art/concept/*.png` | Defines *what* to draw | **No** |
| Palettes in `STYLE.md` | Color lock | No — passed as hex text |

Concept art carries backgrounds, multiple views, and framing. Feed it to the
generator as a reference image and the generator copies that composition too.
Concept art is for a human to look at while writing a prompt.

## Workflow

1. Generate from a prompt below.
2. Save to `.art/concept/<faction>-<variant>.png`.
3. Run `./.art/make-sheets.sh` — it builds a comparison sheet of the variants.
4. Pick a direction; record the decision in `STYLE.md`.
5. Once a direction is chosen, produce one *clean sprite* in that style
   (transparent, single subject) and promote it to `.art/anchors/master-v2/`. The concept
   image itself never becomes an anchor.

Current comparison Site:
<https://wordonline-hellfire-art.dev-yunseong.chatgpt.site>

## Master style selection

Selected shared rendering technique:

- A — `.art/concept/master-style-key-v2.png`: 2.5D cut-paper

Selected: **A — 2.5D cut-paper**, approved 2026-07-28.

Canonical anchor set: `.art/anchors/master-v2/`.

B, C, and D were rejected and removed. Do not restore rejected exploration
files or mix them into the rendering reference set.

---

## Priority 1 — Hellfire legion

The only faction with no valid anchor, and the whole fire set is a redesign
target. Three directions; generate all three, pick one.

The three initial exploration images were rejected after the shared master style
was selected and were removed. The retained direction is
`.art/concept/hellfire-lineup-cut-paper-v2.png`.

Shared prefix for all three:

> Flat cartoon game sprite icon, single subject centered on a fully transparent
> background, no outer contour line, broad flat color blocks with soft internal
> gradient, soft light from upper left, high saturation, no fine detail noise, no
> spark or ember scatter, no background, no ground, no frame, no text, facing
> right, silhouette readable at 64 pixels.

### Variant A — Molten primitive

> ...A demonic molten creature from a hellfire dimension. Dark charred basalt
> crust as the primary mass, glowing magma visible through deep cracks in the
> crust. Jagged asymmetric silhouette, no face, eyes are two small glowing slits.
> Mass reads as rock first and fire second. Colors `#2A1512` `#5A291E` `#9B3014`
> `#E65E08`.

### Variant B — Horned demon

> ...A small demon soldier from a hellfire dimension. Deep red skin, black curved
> horns, visible fangs, angular spiked shoulders, clawed hands. Clear character
> anatomy with head, torso and limbs. Narrow glowing yellow slit eyes, hostile
> expression. Colors `#9B3014` `#5A291E` `#2A1512` with `#E65E08` accents.

### Variant C — Ash wraith

> ...A wraith of burnt ash from a hellfire dimension. Near-black smoky silhouette
> with a torn ragged lower edge dissolving into ash, inner core glowing orange
> through the body. Tall and narrow, hollow eye sockets with orange points
> inside. No limbs, no visible face. Colors `#2A1512` `#6D4A39` `#9B3014`
> `#E65E08`.

**Judging criteria.** Reject any variant that (a) reads cute, (b) has large round
friendly eyes, (c) is flame-shaped rather than mass-shaped, or (d) shows
painterly ember scatter. The current `FireSpirit` fails (a)(b)(c); the current
`MagmaSpirit` fails (d).

---

## Priority 2 — Human / golem separation

Both factions currently sample to the same grey. Generate one of each, side by
side, to confirm the split reads.

### Human device

> [shared prefix] ...A built defensive device of the human faction. Cut stone
> blocks, riveted steel plates, wooden beams and rope. Straight lines, right
> angles, bilateral symmetry. No face, no eyes — a machine, not a creature. Cool
> grey stone `#C4CCC8` `#A09C92` `#616662` with steel blue `#6191A3` and bronze
> `#5D4032` accents.

### Rock golem

> [shared prefix] ...A rock golem tribe warrior. Body built from stacked blocky
> boulders with visible seams, heavy wide stance, small head on a large torso.
> Green moss and small plants growing across the stone. Warm tan stone `#CDC8B8`
> `#A3A29D` `#8A857A` with moss `#55572D` `#374725`.

Reject if the two read as the same material at thumbnail size.

Approved on 2026-08-03 for Unity production:

- `.art/concept/human-golem-separation/Cannon-v2.png` — cool cut stone,
  steel-blue braces, bronze fasteners, and timber carriage.
- `.art/concept/human-golem-separation/RockMage-v2.png` — warm block stone,
  moss growth, stone hood, and a small crystal staff.
- `.art/concept/human-golem-separation/ElectricTower-v2.png` — human device
  with cool masonry, steel-blue panels, bronze conductors, and chunky cyan
  lightning.
- `.art/concept/human-golem-separation/MiniRockSwarm-v3-limbs.png` — rock-golem
  creature with warm stacked boulders, restrained moss, short boulder arms,
  and wide walking legs. This supersedes the legless v2 candidate.
- `.art/concept/human-golem-separation/Towerback-v3-compact.png` — compact warm
  living rock carrier with visible arms and legs, restrained moss, and a
  mechanically separate cool-grey, steel-blue, and bronze anti-air cannon
  harness. This supersedes the oversized v2 exploration.
- `.art/concept/human-golem-separation/Tower-v2.png` — shared human ground-tower
  sprite with cool fitted masonry, steel-blue braces, bronze fasteners, and a
  short heavy cannon. `GroundTower` uses it directly and `RockTurret` also
  retains a serialized reference to it.
- `.art/concept/human-golem-separation/RockTurret-v3-single-shot.png` — human
  stone-launching turret with fitted cool masonry, steel-blue fork arms, bronze
  axle hardware, one central sling pouch, and exactly one projectile.
- `.art/concept/human-golem-separation/RockTurretAttacking-v1.png` — paired
  post-release frame with the projectile removed and the single empty sling
  snapped forward. Its `127x192` production export matches the idle frame size
  and bottom-center baseline.

Production mapping: `Cannon.png`, `ElectricTower.png`, `Tower.png`, and
`Towerback.png` at middle tier; `RockTurret.png` at middle tier;
`RockMage.png` and `MiniRockSwarm.png` at small tier. Tier-size review exports
retain their silhouettes at 64px. `RockTurretAttacking.png` is the paired
middle-tier attack frame wired through `AttackSpriteSwapController`.

Generated on 2026-08-03, pending user approval for Unity production:

- None in the current batch.

---

## Priority 3 — Faction lineup boards

Not for generation reference — for a human to hold the world in their head. One
per faction, backgrounds allowed here since these are never fed back in.

> Character lineup sheet, four to six creatures of one faction standing in a row
> on a neutral flat background, consistent scale showing size tiers from small to
> large, flat cartoon style, no text.

Factions: spirits (lightning/nature/wind), water slimes, rock golems, humans,
hellfire legion.

### World Tree lightning spirit — Storm Stag

Approved on 2026-08-30 for Unity production:

- `.art/concept/storm-stag/StormStag-approved-256.png` — a friendly,
  right-facing storm stag rebuilt in the canonical master-v2 2.5D cut-paper
  technique. The compact body, large readable antlers, lightning-spirit yellow
  palette, and restrained cyan paper accents remain legible at 64px without a
  baked aura.

Production mapping: `Assets/Resources/Game/sprites/StormStag.png`, big
`256x256` tier. The existing Unity `.meta` GUID and prefab reference are
preserved.

Generated on 2026-07-28:

- `.art/concept/world-tree-spirits-lineup.png`
- `.art/concept/water-slimes-lineup.png`
- `.art/concept/rock-golems-lineup.png`
- `.art/concept/human-magic-civilization.png`

Additional character and lore exploration:

- `.art/concept/apprentice-player.png` — player appearance is not canonized
- `.art/concept/word-world-tree-key-art.png` — Word remains symbolic because
  the character's established `WordVenture` appearance was not available as a
  reference

## Dimensional wanderers

Approved 2026-07-31: **B — moving world fragments**.

- Board: `.art/concept/dimensional-wanderers/direction-b-world-fragments.png`
- 경계 운반자: `.art/concept/dimensional-wanderers/boundary-carrier.png`
- 화산편: `.art/concept/dimensional-wanderers/cinder-shard.png`
- 폭풍편: `.art/concept/dimensional-wanderers/storm-shard.png`

The three are independent species. Shared dark-violet shell, pale stone bands,
and a visible carried world identify the faction. Do not use frog/tadpole
anatomy or parent/child staging.

Production mapping:

- `DimensionToad.png` — 경계 운반자, big `256x256` tier
- `FireTadpole.png` — 화산편, small `128x128` tier
- `LightningTadpole.png` — 폭풍편, small `128x128` tier

### World Tree key art

The lore center — Word died and the tree grew from where he fell, spirits arising
around it. Worth one piece even though no sprite depends on it directly.

> Key art of a colossal ancient world tree, glowing runes in the bark, small
> elemental spirits drifting around its roots, flat cartoon style, warm light, no
> text.

---

## Lightning strike — cloud and bolt on one canvas

`LightningDropMagic` draws the bolt on the storm cloud itself: on every strike
the cloud's sprite swaps through frames where the bolt grows further down out of
its underside. Cloud and bolt therefore share one canvas.

Two transparent sources, generated separately so the bolt can be revealed
progressively without redrawing the cloud:

> `.art/concept/lightning-strike/storm_cloud_source.png` — a single dark storm
> cloud seen straight on, wider than tall, chunky overlapping matte paper shapes,
> three value bands, soft light from upper-left, no outer contour line, no
> sparks, restrained `#F6DB5F` underglow along the bottom edge only, flat-ish
> underside a bolt can drop out of. Storm gray `#4A4F63` `#6E7488` `#2E3242`.

> `.art/concept/lightning-strike/lightning_bolt_source.png` — one thick jagged
> vertical bolt, tall canvas, wide at the very top edge and tapering to a point
> at the very bottom edge so it can be revealed top-to-bottom, 4 to 6 chunky
> zigzag segments, at most two short chunky forks, no hairline strokes and no
> scattered fragments. `#FCFBD5` `#F6DB5F` `#D7A313` `#996B07`.

The tapering-to-a-point requirement is what makes progressive reveal work: a
bolt that ends in a blunt edge reads as a cut-off shape, not a descending
leader. The old `LightningDrop.png` failed the 64px silhouette rule and was
replaced by this set.

Composition into the shipped sprites is deterministic, not generated — see the
canvas invariant in `ANIMATION-ASSETS.md`.

---

## 플레이어 캐릭터 리스타일 — issue #586

인게임 플레이어 `Assets/Art/Images/Customize/PlayerCharacterBase.png` 는 굵은
검은 외곽선과 흰 스티커 테두리를 두른 정면 스티커 일러스트다. master-v2 의
외곽선 없음, faceted low-poly papercraft, 3/4 각도, 3등신, 인간 진영 팔레트를
전부 어긴다.

같은 인물을 규칙대로 그린 앵커가 이미 있다. `.art/anchors/master-v2/ApprenticeMage.png`
가 그 인물이고, `.art/WORLD.md` 의 플레이어 설정과도 맞는다. 마법사의 탑에서
내려온 수습 마법생이고, 워드가 남긴 마법 카드를 좇는다.

시안은 앵커의 rendering technique 을 고정하고 실루엣과 정체성만 바꾼다.

### 생성 방법

`codex exec` 의 `image_gen` 을 쓰고 앵커를 `-i` 로 붙인다. 참조 이미지는
`master-v2/MasterStyleKey.png` 와 `master-v2/ApprenticeMage.png` 두 장이다.
concept 파일은 배경과 여러 뷰를 담고 있어 참조로 붙이지 않는다.

출력은 mode `RGBA` 에 alpha 최소값 0 이어야 한다. codex 가 파일마다 아래를
직접 확인한 뒤 보고하게 한다. 시스템 `python3` 에는 Pillow 가 없으므로
scratchpad 의 venv 를 준다.

```
python -c "from PIL import Image; im=Image.open(P); print(im.mode, im.convert('RGBA').getchannel('A').getextrema())"
```

### 공통 prefix

> Faceted low-poly papercraft game character, single subject centered on a fully
> transparent background. Build the entire figure from flat polygonal planes, one
> flat color per plane, hard creases between planes — a whole forearm is five or
> six planes. No texture, no grain, no noise, no gradients, no gloss, no
> painterly brushwork. No outer contour line: forms separate by value only. One
> soft light from the upper left. Three-quarter camera, facing right. Chunky
> proportions, about three heads tall. Eyes are dark ovals with one tiny
> highlight. Restrained saturation, mid to high value, nothing muddy or neon. No
> background, no ground plane, no contact shadow, no frame, no text. Silhouette
> readable at 64 pixels. If it could be mistaken for a rendered 3D model or a
> digital painting, it is wrong.

공통 주체와 색:

> The subject is the player character: an apprentice mage who came down from the
> mage tower, chasing the magic cards Word left behind. Human magic civilization
> palette — cool grey robe `#C4CCC8` `#A09C92` `#616662`, steel blue cloak
> `#6191A3`, bronze and wood `#5D4032`, arcane crystal `#2D92E4`.

### 시안 A — 지팡이 수습생

앵커를 그대로 플레이어로 승격시키는 가장 안전한 안.

> Brown spiky hair cut into large angular planes, blue hooded cloak clasped at
> the throat, grey robe, brown belt with a diamond buckle, wooden staff with a
> blue crystal in the left hand, a satchel of cards on the hip. Standing at rest,
> weight on both feet.

### 시안 B — 카드 시전자

이 게임의 행동은 카드를 던지는 것이다. 실루엣에서 가장 밝은 덩어리가 카드가
되게 해서 한눈에 시전자로 읽히게 한다.

> The staff is slung across the back. The right hand holds three glowing arcane
> cards fanned out, the left hand is thrown forward in a casting gesture. The
> fanned cards are the brightest mass in the silhouette. Same cloak, robe, belt
> and satchel.

### 시안 C — 후드 수습생

64px 에서 가장 강한 실루엣을 노리는 안. 얼굴 윗부분은 후드 그림자 plane 하나로
덮고 눈만 남긴다.

> The hood is up and deep, covering the upper face with a single dark plane, only
> the two oval eyes catching light inside it. Cloak falls in four or five large
> creased planes to a wide triangular base. Wooden staff in the right hand, cards
> at the hip.

### 시안 D — 긴 머리 수습생

현재 인게임 캐릭터의 정체성을 잇는 안. 갈색 긴 머리를 유지하되 각진 plane 으로
자른다.

> A girl apprentice with long brown hair cut into five or six large angular
> planes falling past the shoulders, no strands and no hair texture. Same blue
> hooded cloak worn back off the head, grey robe, brown belt, wooden staff with a
> blue crystal, card satchel. Standing at rest.

### 시안 E — 그리모어 학생

지팡이 대신 펼친 마법서를 든 안. 탑에서 막 내려온 학생으로 읽힌다.

> No staff. A thick open grimoire rests on the left forearm, its pages two flat
> planes with a faint blue glow, the right hand gestures over the page. A leather
> shoulder bag with cards sticking out. Grey robe shorter at the knee, blue cloak
> narrower.

### 마감 조건

고른 시안은 `PlayerCharacterBase.png` 를 그대로 대체해야 하므로 화면 크기와
접지점이 바뀌면 안 된다. 현재 값은 캔버스 2048x2048, PPU 100, pivot Center,
몸통 bbox `(404, 539, 1831, 1679)` 다.

- 같은 2048x2048 캔버스에 합성한다.
- 몸통 높이 1140px, 발끝 y=1679, 가로 중심 x=1117.
- 상대편은 `ServedObject` 가 같은 스프라이트를 `flipX` 로 뒤집는다. 좌우 반전에도
  읽혀야 하고 한쪽으로 치우친 소품은 반전에서 어색해진다.
- 머리 위에 `MagicFailEffect` 가 뜬다. 머리 위 공간을 소품으로 채우지 않는다.
- 공격은 `DOTweenAction.SwingMobAttack`, 사망은 `DOTweenAction.FallForward` 라
  스프라이트 하나를 통째로 돌린다. 별도 공격 frame 은 필요 없다.

---

## Recording the outcome

After picking, write down in `STYLE.md` which variant won and why. A chosen
direction with no recorded reason gets re-litigated every time someone adds art.
