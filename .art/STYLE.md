# Art Style

Two axes, kept strictly separate:

- **Rendering technique** — identical across every faction. This is what makes
  the game read as one game.
- **Shape language + palette** — deliberately different per faction. This is
  what makes factions readable at a glance.

Collapsing the two is the failure mode that produced the current drift: "hellfire
should look menacing" turned into "generate hellfire with a different model,"
which broke technique instead of shape.

## Master-style status

One master rendering reference must apply to every character, creature,
building, environment object, and effect. Faction anchors may define subject
identity, shape, and palette, but must not override the master rendering method.

Current status: **A selected and frozen on 2026-07-28**.

Canonical master reference:
`.art/anchors/master-v2/MasterStyleKey.png`

Decision reason: A gives every faction the same popup-book-compatible material,
camera, value structure, and detail density while preserving faction identity
through silhouette and palette. It also fits the existing 2.5D direction better
than vector, clay, or gouache alternatives.

Selected concept:

- A — `.art/concept/master-style-key-v2.png`: 2.5D cut-paper

Use A for all new canonical production art. Rejected B, C, and D exploration
files were removed so they cannot be mixed into generation prompts.

## Shared rendering technique (never varies)

- 2.5D cut-paper cartoon. Build forms from large overlapping matte paper shapes.
- Use three value bands per material: base, broad upper-left highlight, broad
  lower-right shadow. Allow only a very subtle gradient inside a large shape.
- Keep one consistent three-quarter camera facing right.
- Characters use chunky proportions around three heads tall. Buildings and
  environment objects use equally chunky modular forms.
- Use the same simple oval-eye grammar: dark oval with one tiny highlight.
- No painterly brushwork, fine detail noise, or ember/spark scatter.
- No outer contour line. Forms separate by value, not by stroke.
- One soft light source from upper-left. Highlight on top, shade underneath.
- Use short, soft contact shadows only when the asset is placed in a scene.
  Transparent production sprites and anchors carry no baked shadow.
- Restrained saturation, mid-to-high value. Nothing muddy or neon.
- Single subject, centered, transparent background. No scene, no ground plane,
  no framing, no text.
- Characters face right.
- Silhouette must survive downscale to 64px. Thin strokes and scattered
  fragments are rejects — that is why `ChainLightning` and `LightningDrop` fail.

### Named exception — `EvilEnt`

`EvilEnt` is allowed a gaunt, lanky silhouette where every other creature is
chunky. Its concept is a World Tree spirit dried out by the hellfire dimension
(see [`WORLD.md`](WORLD.md) 타락한 정령), and chunky proportions read as a
healthy tree golem, which is the wrong unit — `TreeGolem` already exists and the
two must not be confusable at a glance.

The exemption is narrow and applies to this one sprite set:

- **Exempt from**: chunky proportions around three heads tall, and the 64px
  silhouette rule.
- **Still bound by**: everything else. Faceted flat planes, three value bands,
  no outer contour, no texture or grain, no ember scatter, transparent
  background, upper-left light. Thin limbs are built from the same big flat
  facets as a chunky one — long and narrow, never wispy, never fragmented, and
  always one connected shape.

Do not extend this to other units. Anything else gaunt needs its own decision.
- Max size by tier, aspect preserved, then trimmed tight:
  small `128x128`, middle `192x192`, big `256x256`.

### Named exception — `TutorialHand`

The tutorial pointer hand (`Assets/Art/Images/UI/Tutorial/TutorialHand.png`)
carries a bold dark outline (`#1E1A2B`, about 4% of the drawing width). It is
drawn over lobby and battle screens at about 64px on light and dark
backgrounds, and the outlined candidates read on both where the unoutlined one
did not. The user picked the outlined bare-hand candidate on 2026-10-11.

- **Exempt from**: no outer contour line, and the three-quarter camera facing
  right. It is the back of a bare right hand in a mouse-cursor pose: index
  fingertip at the top-left, wrist leaving at the bottom-right, curled fingers
  hidden behind the knuckle row.
- **Still bound by**: faceted flat planes, three value bands, upper-left light,
  the apprentice mage's skin tone and blue cuff with bronze trim, transparent
  background.

Do not extend this to sprites. Another UI pointer or cursor needs its own
decision.

Animation frames and aura assets follow `.art/ANIMATION-ASSETS.md`. Numeric
state names such as `MagmaSpiritAttacking` identify attack frames, not separate units. Keep
body scale, PPU, Bottom Center pivot, and ground contact invariant across frames.
Generate aura as a separate transparent effect asset, never baked into the body.
`CloudDragon` uses a dedicated spherical **water** aura (`cloud.png`), not the
shared wind aura; keep its center quiet enough for the body silhouette to read.

```bash
magick input.png -resize 256x256 -trim +repage output.png
```

**Ground contact:** unresolved. `RockGolem` carries debris and a contact shadow;
most others float clean. Pick one rule per archetype before the next batch.

## Factions

Faction art derives from the confirmed lore in `WORLD.md`. Keep lore facts there
instead of expanding them independently in this style guide.

### Spirits — lightning / nature / wind

Born around the World Tree. The baseline cute register.

- Round, blobby, weightless. Flowing tails and wisps rather than limbs.
- Large friendly eyes, simple mouth. Reads as benign.
- No hard edges, no armor, no tools.
- Anchors: `ThunderSpirit`, `ZapMouse`, `SeedSpiritSwarm`, `VineSpirit`,
  `WindSpirit`, `CloudDragon`

| | Palette |
|---|---|
| Lightning | `#FCFBD5` `#F6DB5F` `#D7A313` `#996B07` |
| Nature | `#B8CE59` `#9DA74D` `#768738` `#384823` |
| Wind | `#E5EBE7` `#B3CCCA` `#9BB3B3` `#5EA191` `#2A6D64` |

### Water slimes — the moderates

Survivors, not spirits. Slightly more substance than the spirit set but still soft.

- Translucent, gel-like body. Volume implied by internal highlight.
- May carry simple gear (`AquaArcher` has a bow) — spirits may not.
- Anchors: `AquaArcher`, `BubbleSpirit`, `WaterSlimeSwarm`, `TideCall`

Palette: `#E8F7EE` `#98DEEB` `#6FCCF4` `#53B2EB` `#2D92E4`

### Rock golems — the tribe

Neither spirit nor demon. Neutral third party.

- Blocky stacked masses, visible seams between blocks.
- Heavy, grounded, wide stance. Small head relative to body.
- Moss and vegetation growing on stone — this is the tribe's signature and the
  thing that separates them from human masonry.
- Anchors: `RockGolem`, `RockMage`

Palette: warm tan stone `#CDC8B8` `#A3A29D` `#8A857A`, moss `#55572D` `#374725`

### Humans — the player's faction

Machinery and masonry. Built, not born.

- Straight lines, right angles, bilateral symmetry. Nothing organic.
- Cut-stone blocks, riveted metal, wood beams, rope.
- No face, no eyes. These are devices, not creatures.
- Anchors: `Cannon`, `ElectricTower`

Palette: cool grey `#C4CCC8` `#A09C92` `#616662`, steel blue `#6191A3`,
bronze/wood `#5D4032`

> **Conflict to fix.** Human grey and golem grey currently sample nearly
> identical (`#A09C92` vs `#A3A29D`). The two factions are not separable by
> color. Resolution: golems go warm tan + moss, humans go cool grey + steel blue
> + bronze. Existing sprites need a pass to enforce this.
>
`Towerback` is assigned to the human faction as a forced weapon: a kidnapped
MiniRock carries a human anti-air tower. Keep the warm living stone and moss of
the captive body visibly separate from the cool masonry, steel-blue metal, and
bronze restraints of the mounted tower.

### 플레이어 캐릭터 — 수습 마법생

인간 마법 문명의 무기와 달리 플레이어는 사람이다. 얼굴 없는 장치 규칙은 인간
진영의 구조물에만 적용하고, 플레이어는 `ApprenticeMage` 앵커의 피부·천·나무
재질과 눈 문법을 그대로 쓴다.

- 선택: **D — 긴 머리 수습생**, 2026-09-09 승인.
- 선택한 파일: `.art/concept/player-restyle/player-D-longhair.png`
- 이유: 지금 인게임 캐릭터의 갈색 긴 머리를 잇는 유일한 안이라, 스프라이트 교체가
  캐릭터 교체로 보이지 않는다. 나머지 네 갈래(A 지팡이·B 카드 시전자·C 후드·E
  그리모어)는 모두 앵커의 소년으로 돌아가야 했다.
- 탈락한 갈래와 생성 시도는 `.art/concept/player-restyle/` 에 남겨 두었다.

구성 고정값: 갈색 긴 머리를 각진 plane 대여섯 개로 자른다. 파란 후드 클록은
후드를 내린 채로 두르고, 회색 로브, 다이아몬드 버클이 달린 갈색 벨트, 허리의
카드 가방, 파란 결정이 달린 나무 지팡이를 든다. 3등신, 3/4 각도, 오른쪽 방향.

머리 plane 은 크게 자르되 하나의 덩어리로 뭉치지 않게 한다. 생성 시도 일곱 장 중
넷이 머리를 바위처럼 만들었다. 이것이 이 캐릭터의 유일한 반복 실패다.

### Dimensional wanderers — moving world fragments

Independent species carrying fragments of collapsed worlds. They are not
toads, tadpoles, spirits, or demons.

- Dark violet faceted shell frames a visible miniature world or elemental core.
- Pale stone bands and angular plates make one shared faction language.
- `DimensionToad` / 경계 운반자: broad six-legged carrier, stable portal chamber.
- `FireTadpole` / 화산편: low volcanic fragment with orange magma seams.
- `LightningTadpole` / 폭풍편: tall storm fragment with cyan lightning core.
- Keep silhouettes distinct; shared shell architecture establishes kinship,
  not matching anatomy.
- Approved direction: B — moving world fragments.
- Concept board: `.art/concept/dimensional-wanderers/direction-b-world-fragments.png`

Palette: shell `#25223B` `#484061` `#736982`, stone `#D2BFA7`,
portal cyan `#64D4E8`, magma `#E65E08`, lightning `#77E9F2`.

### Hellfire legion — the demons

**Not spirits.** Every current fire sprite violates this and is a redesign target.

- Angular, spiked silhouette. Horns, fangs, jagged shell, cracked crust.
- No large friendly eyes. Eyes are glowing slits, points, or absent.
- Dark base with internal glow through cracks — mass first, flame second.
  Current sprites are flame-first, which is why they read as spirits.
- Asymmetry allowed; the other factions stay symmetric.

Palette: `#E65E08` `#9B3014` `#5A291E` `#2A1512`, ash `#6D4A39`

Anchor: `master-v2/HellfireDemon.png`. `FrenzyTotem` remains a usable secondary
shape/palette reference (dark crimson, angular, tribal).

Redesign list: `FireSpirit`, `EmberSpiritSwarm`, `FireLordSpirit`,
`FireChildSpirit`, `MagmaSpirit`, `MagmaSpiritAttacking`, `MagmaExplosion`, `Crater`,
`FireShot`, `RallyingTotem`.

`MagmaSpirit` and `Crater` already have the right concept; only their technique
is wrong. `FireSpirit` and friends have the right technique and the wrong
concept. Different fixes.

## Palette provenance

Values above were sampled from shipped sprites via quantization, not authored.
They describe what the art currently is. Treat them as a starting point and
replace any that were pulled from a sprite on the redesign list.

```bash
magick Assets/Resources/Game/sprites/VineSpirit.png -background none -flatten \
  -trim +repage -alpha off -colors 12 -format %c histogram:info:- | sort -rn | head
```
