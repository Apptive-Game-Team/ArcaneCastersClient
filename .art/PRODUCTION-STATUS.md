# 프로덕션 아트 교체 현황

게임에 적용한 새 아트를 한 곳에서 추적하는 정본 문서다. 새 이미지 교체 작업을
시작하기 전에 이 문서를 확인하고, 적용을 마친 같은 변경에서 해당 행을 갱신한다.
이 목록에 `완료`로 기록된 리소스는 다시 교체 후보로 제안하지 않는다. 재작업이
필요하면 먼저 상태를 `재작업 필요`로 바꾸고 구체적인 이유를 적는다.

## 상태 판정 기준

- `완료`: 새 아트가 클라이언트에 적용됐고, 웹사이트에 해당 이미지가 등록됐거나
  클라이언트 PR에서 검수까지 끝났다.
- `재작업 필요`: 새 아트를 적용했지만 프레임 정렬, 투명 배경, 스타일 등 확인된
  문제가 남았다.
- `미확인`: 새 이미지처럼 보여도 교체 완료 근거를 아직 대조하지 못했다.
- 웹사이트 기준은 [`Apptive-Game-Team/theevilent`](https://github.com/Apptive-Game-Team/theevilent)의
  `main`에 있는
  `public/concept-art/`와 `public/game-assets/`다. 웹사이트 파일은 표시용 WebP이고,
  실제 런타임 정본은 `Assets/Resources/Game/sprites/` 아래 PNG다.
- 최초 목록은 웹사이트 커밋 `860526f`을 기준으로 대조했다. 2026-08-31의
  후속 동기화는 웹사이트 PR
  [`#29`](https://github.com/Apptive-Game-Team/theevilent/pull/29)에 기록했다.
  이후에는 최신 `main`을 다시 확인해 새로 추가된 파일을 반영한다.

## 완료

| 런타임 리소스 | 웹사이트/검수 근거 | 비고 |
|---|---|---|
| `AquaArcher.png` | `concept-art/aqua-archer-drawn.webp` | 물의 궁수 기본 프레임 |
| `AquaArcherAttack.png` | `concept-art/aqua-archer-release.webp` | 화살을 놓은 공격 프레임 |
| `ChickenCommando.png` | `concept-art/chicken-commando.webp`, `game-assets/chicken-commando.webp` | 인간 공수 특공대 방향 적용 |
| `DimensionToad.png` | `concept-art/dimension-toad.webp`, `game-assets/dimension-toad.webp` | 차원 유랑종 경계 운반자 방향 적용 |
| `EmberSpiritSwarm.png` | `concept-art/ember-spirit-swarm.webp` | 지옥불 악마병 무리로 교체 |
| `EvilEnt.png` | 웹사이트 PR #29 `game-assets/evil-ent-idle.webp` | 사악한 나무 골렘 기본 자세 |
| `EvilEnt2.png` | 웹사이트 PR #29 `game-assets/evil-ent-attack.webp` | 팔을 뻗은 공격 자세 |
| `FireChildSpirit.png` | `concept-art/fire-child-spirit.webp` | 하급 악마 방향 적용 |
| `FireLordSpirit.png` | `concept-art/fire-lord-spirit.webp` | 지옥불 군단 지휘관으로 교체 완료 |
| `FireSpirit.png` | `concept-art/fire-spirit.webp` | 하급 뿔 악마 방향 적용 |
| `FireTadpole.png` | `concept-art/fire-tadpole.webp` | 차원 유랑종 화산편 방향 적용 |
| `RockGolem.png` | `concept-art/rock-golem.webp` | 이끼바위 골렘 기본 자세 |
| `RockGolem2.png` | `game-assets/rock-golem-attack.webp` | 이끼바위 골렘 공격 자세 |
| `RockRemnant.png` | `game-assets/rock-remnant.webp` | 사망 후 이동 방해 잔해 |
| `TitanRemnant.png` | 클라이언트 이슈 #593 재작업 검수 | 거신의 잔해 본체 |
| `TitanFist.png` | 클라이언트 이슈 #593 재작업 검수 | 주변 적 위치에서 솟구치는 거대한 돌주먹 |
| `LightningTadpole.png` | `concept-art/lightning-tadpole.webp` | 차원 유랑종 폭풍편 방향 적용 |
| `MagmaSpirit.png` | `concept-art/magma-spirit-idle.webp` | 기본 자세 |
| `MagmaSpiritAttacking.png` | `concept-art/magma-spirit-attack.webp` | 공격 자세 |
| `MagmaSpiritSpawn.png` | `concept-art/magma-spirit-spawn.webp` | 소환 자세 |
| `WaterSlimeSwarm.png` | `concept-art/water-slime.webp` | 물방울 생존자 무리 기본 자세 |
| `WaterSlimeAttackSpit.png` | `game-assets/water-slime-attack.webp` | 물 뱉기 공격 프레임 |
| `TidalWarhead.png` | 클라이언트 이슈 #597 재작업 검수 | 공중 표적용, 어두운 눈, 256x256 RGBA |
| `GroundTidalWarhead.png` | 클라이언트 이슈 #597 재작업 검수 | 지상 표적용, 밝은 눈, 본체와 동일 실루엣 |
| `cloud.png` | `game-assets/cloud-dragon-water-aura.webp`, 클라이언트 PR (이슈 #76) | 운룡 전용 구형 물 아우라. 웹사이트 근거로 완료였으나 이슈 #65 대조에서 런타임 PNG 는 광택 있는 파란 구체로 남아 있는 것이 드러나 이슈 #76 에서 다시 그렸다. 510x512 RGBA, 원본 캔버스 유지, 내용 508x497. v1 반려 — 가로세로 비 1.53 의 옆으로 퍼진 구름이라 원본 구체(1.00)에 비율을 지켜 넣으면 세로가 35% 줄었다. v2 에서 "정사각 캔버스를 네 변까지 채운다"를 프롬프트에 넣어 1.025 로 통과 |
| `fire_aura.png` | `game-assets/fire-aura.webp` | 지옥불 공용 오라 |
| `LightningCloud.png` | 웹사이트 PR #29 `game-assets/lightning-cloud-idle.webp` | 번개 구름 대기 프레임 |
| `LightningCloudStrike0.png` | 웹사이트 PR #29 `game-assets/lightning-cloud-strike-0.webp` | 강타 1 프레임 |
| `LightningCloudStrike1.png` | 웹사이트 PR #29 `game-assets/lightning-cloud-strike-1.webp` | 강타 2 프레임 |
| `LightningCloudStrike2.png` | 웹사이트 PR #29 `game-assets/lightning-cloud-strike-2.webp` | 강타 3 프레임 |
| `LightningCloudStrike3.png` | 웹사이트 PR #29 `game-assets/lightning-cloud-strike-3.webp` | 강타 4 프레임 |
| `LightningCloudStrike4.png` | 웹사이트 PR #29 `game-assets/lightning-cloud-strike-4.webp` | 강타 5 프레임 |
| `LightningCloudStrike5.png` | 웹사이트 PR #29 `game-assets/lightning-cloud-strike-5.webp` | 강타 6 프레임 |
| `LightningDrop.png` | 웹사이트 PR #29 `game-assets/lightning-drop.webp` | 번개 투하 인게임 스프라이트 |
| `ChainLightning.png` | 클라이언트 PR #557, 웹사이트 PR #29 | 256x76 RGBA, 64px 실루엣 검수 완료 |
| `MagmaExplosion.png`, `MagmaExplosionStrike1.png`–`MagmaExplosionStrike4.png` | 클라이언트 PR #565 | 217x256 RGBA, 갑각 파편 우선 실루엣과 4프레임(균열→개방→피크→냉각) 검수 완료 |
| `PlayerCharacterBase.png`, `PlayerCharacterAttack.png` | 클라이언트 PR (이슈 #586) | 플레이어 수습 마법생 D안. 2048x2048 RGBA, 캐릭터 키 1140px, 발끝 y=1679 로 두 프레임 정렬. Unity Editor 검수는 남아 있다 |
| `SeaSerpent.png` | 클라이언트 PR (이슈 #672) | 201x256 RGBA. magic book 이 빈 칸이던 신규 아이콘. 물 소환수 문법, 곧추선 몸통에 두 겹 똬리. Unity Editor 검수는 남아 있다 |
| `BoulderStrike.png` | 클라이언트 PR (이슈 #672) | 256x146 RGBA. 밀려 나가는 바위와 뒤따르는 초승달. 전투 화면 투사체는 여전히 `RockRolling.png` 를 빌려 쓴다 |
| `SpiritBomb.png` | 클라이언트 PR (이슈 #672) | 256x183 RGBA. 필드 오브젝트가 없는 마법이라 책 아이콘 전용. LIGHTNING 과 NATURE 두 원소를 금색과 풀색으로 같이 쓴다 |
| `ligtning_aura.png` | 클라이언트 PR (이슈 #680) | 512x469 RGBA. `fire_aura.png` 기법으로 재작업한 전기 공용 오라. 64px 실루엣 검수 완료 |
| `nature_aura.png` | 클라이언트 PR (이슈 #680) | 512x484 RGBA. `fire_aura.png` 기법으로 재작업한 자연 공용 오라. 64px 실루엣 검수 완료 |
| `rock_aura.png` | 클라이언트 PR (이슈 #680) | 506x512 RGBA. `fire_aura.png` 기법으로 재작업한 바위 공용 오라. 64px 실루엣 검수 완료 |
| `water_aura.png` | 클라이언트 PR (이슈 #680) | 512x463 RGBA. `fire_aura.png` 기법으로 재작업한 물 공용 오라. `cloud.png`(운룡 전용 구형 오라)와는 별개. 64px 실루엣 검수 완료 |
| `wind_aura.png` | 클라이언트 PR (이슈 #680) | 512x269 RGBA. `fire_aura.png` 기법으로 재작업한 바람 공용 오라. 64px 실루엣 검수 완료 |
| `BombSprite.png`, `BombSpriteBomb.png` | 클라이언트 PR (이슈 #679) | 222x256 / 231x256 RGBA. 채색 그림체에서 master-v2 로 재작업. 도화선 불꽃을 양쪽 다 금색으로 맞췄다 |
| `explode.png` | 클라이언트 PR (이슈 #682) | 841x769 RGBA. 마법 폭발/실패 표시. 남색-보라 결정 파편 다발, 가운데 비움, 원본의 분홍·보라 정체성은 파편 두 개에 악센트로 남김. green 키 제거, 네 모서리 alpha 0 |
| `hit_effect.png` | 클라이언트 PR (이슈 #682) | 771x700 RGBA. 피격 표시, 8방향 쐐기 방사형 파편, 가운데 비움. magenta 키 제거, 네 모서리 alpha 0 |
| `heal_effect.png` | 클라이언트 PR (이슈 #682) | 826x818 RGBA. 치유 표시, 다면체 십자가. 대각선 세 면(하이라이트·베이스·그림자)으로만 분할, 두께·챔퍼 없음 — 1차 시도는 입체 블록으로 읽혀 revert 후 재시도. magenta 키 제거, 네 모서리 alpha 0 |
| `Burn.png`, `Burn2.png` | 클라이언트 PR (이슈 #682) | 238x256 RGBA 두 장. 화상 상태 표시, 불꽃 여섯 덩이. 두 프레임 alpha content bbox 완전 일치(겹쳐서 확인). magenta 키 제거, 네 모서리 alpha 0 |
| `Overcharge.png`, `Overcharge2.png` | 클라이언트 PR (이슈 #682) | 512x512 RGBA 두 장. 전기 과충전 표시, 번개 조각 여섯 개. 두 프레임 겹침 확인, 끝부분 미세한 흔들림만 차이. magenta 키 제거, 네 모서리 alpha 0 |
| `Bubble.png` | 클라이언트 PR (이슈 #682) | 119x119 RGBA. 물방울 표시, 저폴리 보석형 단일 도형. magenta 키 제거, 네 모서리 alpha 0 |
| `Snared.png` | 클라이언트 PR (이슈 #682) | 256x191 RGBA. 속박 표시, X자로 교차한 다면체 덩굴, 끝에 가시 악센트. magenta 키 제거, 네 모서리 alpha 0 |
| `frenzy.png` | 클라이언트 PR (이슈 #682) | 256x256 RGBA. 광폭화 표시, 진홍 발톱/번개 표식 3개, 발광 없이 면 대비로만 표현. magenta 키 제거, 네 모서리 alpha 0 |
| `panic.png` | 클라이언트 PR (이슈 #682) | 250x256 RGBA. 공포 표시, 다면체 느낌표. magenta 키 제거, 네 모서리 alpha 0 |
| `magic_fail.png` | 클라이언트 PR (이슈 #682) | 629x559 RGBA. 시전 실패 표시, 검은 연기 뭉치. 1차 시도는 베이지색 돌무더기로 나와 revert 후 재시도 — 숯색·짙은 회색 계열, 둥글게 굴린 뭉게구름 로브, 돌처럼 보이지 않게 못박음. magenta 키 제거, 네 모서리 alpha 0 |
| `explode/fire_explode.png` | 클라이언트 PR (이슈 #683) | 192x168 RGBA, 원본 크기 유지. 화염탄 계열 공용 폭발. Hellfire legion 팔레트(어두운 갑각+주황-빨강 갈라진 빛)로 교체, 기존의 밝은 주황-노랑 그라데이션에서 이동. magenta 키 제거, enclosed_pixels=3, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `explode/leaf_explode.png` | 클라이언트 PR (이슈 #683) | 192x132 RGBA, 원본 크기 유지. 나뭇잎 파편 부채꼴, ArcaneImpact.png 결정 파편 기법 적용. magenta 키 제거, enclosed_pixels=0, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `explode/rock_explode.png` | 클라이언트 PR (이슈 #683) | 192x124 RGBA, 원본 크기 유지. 돌 파편 폭발. STYLE.md 규칙대로 찬 회색에서 이끼바위 골렘의 따뜻한 황갈+이끼 팔레트로 교체, 중심 섬광도 시안에서 미색으로 바꿔 물 계열과 겹치지 않게 했다. magenta 키 제거, enclosed_pixels=0, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `explode/water_explode.png` | 클라이언트 PR (이슈 #683) | 192x134 RGBA, 원본 크기 유지. 물보라 파편, 물결 밑동에서 물방울 결정이 솟는 구도 유지. magenta 키 제거, enclosed_pixels=1, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `explode/wind_explode.png` | 클라이언트 PR (이슈 #683) | 192x144 RGBA, 원본 크기 유지. 첫 생성이 4방향 완전 대칭 바람개비로 나와 "아이템처럼 보인다"는 기준으로 탈락시키고, 길이·각도·간격이 제각각인 낱개 파편으로 다시 생성해 통과시켰다. magenta 키 제거, enclosed_pixels=2, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `explode/lava_fist.png` | 클라이언트 PR (이슈 #683) | 130x192 RGBA, 원본 크기 유지. 용암 주먹, HellfireDemon.png 재질(어두운 갑각+주황 갈라진 빛)로 교체. magenta 키 제거, enclosed_pixels=0, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `electric_field.png` | 클라이언트 이슈 #681 재작업 검수 | 192x84 RGBA. 번개 정령 팔레트, 굵은 아크 서너 줄기가 타원 바깥쪽 1/3에 몰리고 가운데 1/3은 거의 맨 그을린 땅. 소환수 스프라이트를 얹어 실루엣이 읽히는지 확인했다 |
| `fire_field.png` | 클라이언트 이슈 #681 재작업 검수 | 192x68 RGBA. 지옥불 팔레트, 그을린 땅에서 솟은 불꽃이 타원 전체를 덮는다. 테두리가 가장 높고 중앙으로 갈수록 낮아져 소환수 자리가 남는다 |
| `leaf_field.png` | 클라이언트 이슈 #681 재작업 검수 | 192x79 RGBA. 자연 정령 팔레트, 땅에서 자란 풀잎이 타원 전체를 덮는다. 테두리가 가장 높고 중앙으로 갈수록 낮아져 소환수 자리가 남는다 |
| `water_field.png` | 클라이언트 이슈 #681 재작업 검수 | 192x69 RGBA. 물 슬라임 팔레트, 물에 잠긴 얕은 웅덩이가 타원 전체를 덮는다. 물결은 테두리에 몰리고 중앙은 잔잔하다 |
| `crater_ember.png` | 클라이언트 이슈 #681 재작업 검수 | 64x58 RGBA. 분화구에서 튄 불씨 하나, 둥근 물방울 실루엣에 면 3~4개, 가장 밝은 면이 중앙 |
| `Shock.png`, `Shock2.png` | 클라이언트 PR (이슈 #682) | 247x256 / 244x256 RGBA. 감전 표시, 다면체 번개 조각 다섯 개, 크기를 다르게 흩뿌리고 가운데를 비웠다. 이전 세 차례 시도는 개별 생성이라 프레임 2의 구도·크기가 10% 어긋나 실패했다. 이번은 한 이미지에 좌우 두 패널로 같은 배율·같은 기준선으로 나란히 생성한 뒤 반으로 잘라 각각 마무리했다 — 원본 패널(887x887) 기준 두 프레임의 alpha content bbox 크기 차이 2.0%/2.4%, 위치 차이 16~17px(패널 대비 약 2%). magenta 키 제거, 네 모서리 alpha 0, 불투명 픽셀에 magenta 잔색 0 |
| `Wet.png`, `Wet2.png` | 클라이언트 PR (이슈 #682) | 245x256 / 225x256 RGBA. 젖음 표시, 다면체 물 튀김 덩어리 세 개와 물방울 두 개. Shock 과 같은 좌우 쌍둥이 패널 생성으로 처음부터 시도해 한 번에 통과했다 — 원본 패널(887x887) 기준 두 프레임의 alpha content bbox 크기 차이 1.6%/1.3%, 위치 차이 0~3px. magenta 키 제거, 네 모서리 alpha 0, 불투명 픽셀에 magenta 잔색 0 |
| `rune/fire_rune.png` | 클라이언트 PR (이슈 #683) | 192x109 RGBA, 원본 크기 유지. 어두운 숯색 돌 원반 + Hellfire 팔레트 불꽃 문양. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `rune/lightning_rune.png` (크기 교정) | 클라이언트 PR (이슈 #683) | 192x106 RGBA, 원본 크기 유지. 어두운 숯색 돌 원반 + 금색 번개 문양. 기존 파란 계열에서 STYLE.md 의 금색 Lightning 팔레트로 이동. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `rune/nature_rune.png` | 클라이언트 PR (이슈 #683) | 192x105 RGBA, 원본 크기 유지. 어두운 숯색 돌 원반 + Nature 팔레트 잎 문양. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `rune/rock_rune.png` | 클라이언트 PR (이슈 #683) | 192x98 RGBA, 원본 크기 유지. 이끼바위 골렘의 따뜻한 황갈+이끼 팔레트 바위 문양. 1차 생성이 베이지 원반에 원반과 명도가 비슷한 문양으로 나와 세트에서만 튀어 반려, 어두운 숯색 원반(나머지 다섯과 동일) + 황갈+이끼 고리 + 밝게 읽히는 문양으로 재생성해 통과시켰다. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `rune/water_rune.png` | 클라이언트 PR (이슈 #683) | 192x105 RGBA, 원본 크기 유지. 어두운 숯색 돌 원반 + Water 팔레트 물방울 문양. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `rune/wind_rune.png` | 클라이언트 PR (이슈 #683) | 192x106 RGBA, 원본 크기 유지. 어두운 숯색 돌 원반 + Wind 팔레트 소용돌이 문양. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `spawn/fire_slime.png` | 클라이언트 PR (이슈 #683) | 128x108 RGBA, 원본 크기 유지. 소환 시 표시되는 살아있는 mob(HP·speed·상태 기계 보유). 밝고 명랑한 주황 젤 몸통에서 Hellfire 팔레트의 어두운 재 몸통 + 갈라진 틈으로 비치는 주황으로 이동. 1차 생성은 얼굴 없는 다면체 언덕으로 나와 반려 — 이 자산은 효과가 아니라 소환수라 STYLE.md 의 단순 타원 눈 문법이 적용된다는 지적을 받고, `WaterSlime.png` 를 눈 문법 레퍼런스로 추가해 어두운 타원 눈 두 개와 작은 입을 얹어 재생성했다. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `spawn/leaf_slime.png` | 클라이언트 PR (이슈 #683) | 128x108 RGBA, 원본 크기 유지. Nature 팔레트 몸통, 두 잎 새싹 장식 유지. fire_slime 과 같은 이유로 1차 생성(눈·입 없음)을 반려하고 `WaterSlime.png` 눈 문법 레퍼런스로 재생성했다. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 — 이 눈·입 추가 과정에서 방향이 회귀해 `facing.py` 기준 -18%(왼쪽, main 원본 +26%)로 PR #696 에 올라갔었다. 좌우 반전으로 교정해 +18%(오른쪽)로 확인, 광원이 좌상단 고정이라 반전 후에도 명암 배치가 자연스러운지 눈으로 재확인했다 |
| `spawn/lightning_slime.png` | 클라이언트 PR (이슈 #683) | 128x111 RGBA, 원본 크기 유지. 금색 Lightning 팔레트 몸통, 번개 스파크 장식 유지. leaf_slime 과 같은 방향 회귀(-18%)를 좌우 반전으로 교정, `facing.py` 재확인 +18%(오른쪽). magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `spawn/rock_slime.png` | 클라이언트 PR (이슈 #683) | 128x105 RGBA, 원본 크기 유지. 이끼바위 골렘의 따뜻한 황갈+이끼 팔레트 몸통. 눈·입을 추가한 재생성 요청이 output moderation 에 두 번 걸려(요청 ID 는 로그 참고) 레퍼런스를 `MasterStyleKey.png` + `ArcaneImpact.png` 조합으로 바꾼 세 번째 시도로 통과시켰다. leaf_slime 과 같은 방향 회귀(-14%)를 좌우 반전으로 교정, `facing.py` 재확인 +14%(오른쪽). magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `spawn/wind_slime.png` | 클라이언트 PR (이슈 #683) | 128x65 RGBA, 원본 크기 유지. Wind 팔레트 몸통, 원본대로 다른 넷보다 낮고 평평한 실루엣 유지. leaf_slime 과 같은 방향 회귀(-16%)를 좌우 반전으로 교정, `facing.py` 재확인 +16%(오른쪽). magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `shoot/leaf_shoot.png` | 클라이언트 PR (이슈 #683) | 192x74 RGBA, 원본 크기 유지. 오른쪽으로 날아가는 나뭇잎 투사체, 넓고 밝은 잎날이 오른쪽, 가늘어지는 잎맥 꼬리가 왼쪽. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `shoot/lightning_shoot.png` | 클라이언트 PR (이슈 #683) | 192x51 RGBA, 원본 크기 유지. 금색 지그재그 번개 투사체, 오른쪽 뭉툭한 화살촉에서 왼쪽으로 갈수록 가늘어짐. 기존 청록/흰색 조합에서 STYLE.md 의 금색 Lightning 팔레트로 이동. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `shoot/sprayed_flame.png` | 클라이언트 PR (이슈 #683) | 192x95 RGBA, 원본 크기 유지. Hellfire 팔레트로 이동 — 갑각 먼저, 불꽃은 갈라진 틈으로만. 밝은 주황-노랑 그라데이션 화염구에서 어두운 재 덩어리 + 주황 균열 갈라짐으로 교체, 균열이 갈라지는 결로 오른쪽이 뭉치고 왼쪽이 가늘어짐. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료. `fire_drop.png` 과 재질이 비슷해 보인다는 지적이 있어 둘을 나란히 놓고 보는 후속 판단이 남아 있다 |
| `shoot/water_shoot.png` | 클라이언트 PR (이슈 #683) | 192x61 RGBA, 원본 크기 유지. Water 팔레트 물방울/파도머리 투사체, 넓고 밝은 머리가 오른쪽, 잔물결 꼬리가 왼쪽. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `drop/fire_drop.png` | 클라이언트 PR (이슈 #683) | 104x192 RGBA, 원본 크기 유지. Hellfire 팔레트로 이동한 낙하 불꽃, 위가 넓고 아래가 뾰족한 형태 유지. 밝은 주황-노랑 불꽃 아이콘에서 어두운 재 + 갈라진 틈으로 비치는 주황으로 교체. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `drop/leaf_drop.png` | 클라이언트 PR (이슈 #683) | 103x192 RGBA, 원본 크기 유지. 위가 넓고 아래가 뾰족한 잎사귀, 중앙맥을 면 경계로 표현(밝은 왼쪽 절반 + 어두운 오른쪽 절반)하고 보조 잎맥을 대각선 크리스로 뻗었다. 1차 생성은 잎맥 없는 밋밋한 초록 물방울로 나와 주제를 잃었다는 이유로 반려 — 프롬프트를 중앙맥이 면 경계로 드러나는 잎사귀로 다시 써서(v2) 재생성해 통과시켰다. `fire_drop`(용암 덩어리)·`wind_drop`(소용돌이)과 나란히 놓아도 셋 다 정체가 분명하다. magenta 키 제거, enclosed_pixels=0, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `drop/wind_drop.png` | 클라이언트 PR (이슈 #683) | 106x192 RGBA, 원본 크기 유지. Wind 팔레트 소용돌이 낙하 이펙트, 위쪽 고리가 넓고 아래로 갈수록 좁아지는 형태 유지. magenta 키 제거, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `GiantVine.png` | 클라이언트 PR (이슈 #694) | 135x256 RGBA, 원본 크기 유지. 덩굴 세계 마법의 필드 오브젝트, 바닥에서 솟은 굵은 마디형 덩굴 줄기. `VineColony`/`VineToss`/`VineWorld`와 같은 덩굴 문장(마디 링·낱장 잎·말린 덩굴손)을 프롬프트 문장 단위로 공유. magenta 키 제거, enclosed_pixels=0, 네 모서리 alpha 0, magenta 잔색 0px, 64px 실루엣 검수 완료 |
| `VineToss.png` | 클라이언트 PR (이슈 #694) | 135x256 RGBA, 원본 크기 유지. 덩굴 투척 투사체, 원본과 같은 방향(바닥 좌측에서 상단 우측으로 기울어 솟구침)으로 고정. magenta 키 제거, enclosed_pixels=0, 네 모서리 alpha 0, magenta 잔색 0px, 64px 실루엣 검수 완료 |
| `VineColony.png` | 클라이언트 PR (이슈 #694) | 256x220 RGBA, 원본 크기 유지. 덩굴 군락 설치물, 같은 덩굴 줄기 여러 가닥을 엮은 공 모양 둥지. 엮인 틈으로 magenta 배경이 다수 보여 enclosed_pixels=17018로 나왔으나 잔색 검사(불투명 픽셀 중 magenta 성분 0px)와 눈으로 본 결과 모두 실제 구멍이지 키 오염이 아님을 확인했다. 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `VineWorld.png` | 클라이언트 PR (이슈 #694) | 256x256 RGBA, 원본 크기 유지. 덩굴 세계 마법책 아이콘, 중앙의 큰 덩굴과 그 둘레를 감싼 작은 덩굴 8개로 구성된 기존 다중 피사체 구도를 유지했다(책 아이콘 예외). magenta 키 제거, enclosed_pixels=0, 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `LightningExplosion.png` | 클라이언트 PR (이슈 #694) | 256x155 RGBA, 원본 크기 유지. 번개 폭발, 기존 파란 계열에서 STYLE.md 의 금색 Lightning 팔레트로 이동. 중심에서 방사형으로 뻗는 갈래 다발, 가장자리는 낱개 가지 끝이 열려 있어 닫힌 원판이 아니다. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, 64px 실루엣 검수 완료 |
| `ShockOverload.png` | 클라이언트 PR (이슈 #694) | 256x193 RGBA, 원본 크기 유지. 전격 과부하, 원래도 금색이었으나 촘촘한 뭉게구름형 텍스처에서 다면체 갈래 다발로 다시 그렸다. 트림 후 원본 비율(가로세로 1.96)과 목표 비율(1.33)의 차이로 가로 방향 압축이 더 크게 들어갔으나 눈으로 본 실루엣은 자연스럽다. enclosed_pixels=11145는 갈래 사이 트인 틈으로 확인, 불투명 픽셀 magenta 잔색 0px. 네 모서리 alpha 0, 64px 실루엣 검수 완료 |
| `FireShot.png` | 클라이언트 PR (이슈 #694) | 256x88 RGBA, 원본 크기 유지. 화염탄 투사체, Hellfire 재 갑각 + 갈라진 틈으로 비치는 용암광으로 이동. 원본과 같은 방향(넓은 톱니머리 우측, 가는 꼬리 좌측)으로 고정. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과(바닥여백 0→0) |
| `MeteorShower.png` | 클라이언트 PR (이슈 #694) | 207x256 RGBA, 원본 크기 유지. 지옥불 군단 유성, 갈라진 재 갑각 사이로 비치는 비대칭 용암 균열. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과(바닥여백 13→0, 하락 방향이라 미해당) |
| `RazorGale.png` | 클라이언트 PR (이슈 #694) | 256x207 RGBA, 원본 크기 유지. 칼바람 회오리, 두세 겹 겹친 다면체 곡선 리본이 감아 도는 소용돌이. 리본 사이 트인 틈으로 enclosed_pixels=16368가 나왔으나 불투명 픽셀 magenta 잔색은 0px. magenta 키 제거, 네 모서리 alpha 0, `shrink.py`/`bottom.py` 검수 통과 |
| `SandStorm.png` | 클라이언트 PR (이슈 #694) | 159x256 RGBA, 원본 크기 유지. 모래 폭풍, Wind 진영 소속이지만 모래라는 재질 때문에 따뜻한 황토색(`#D8B978`~`#6E4E22`)을 그대로 유지 — 바위 골렘의 이끼 낀 황갈과는 톤을 다르게 잡아 구분된다. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `TornadoStrike.png` | 클라이언트 PR (이슈 #694) | 224x256 RGBA, 원본 크기 유지. 회오리 정령, 위가 넓고 아래로 갈수록 좁아지는 깔때기. enclosed_pixels=26453는 리본 사이 트인 중심부, 불투명 픽셀 magenta 잔색 0px. magenta 키 제거, 네 모서리 alpha 0, `shrink.py`/`bottom.py` 검수 통과 |
| `WindBlade.png` | 클라이언트 PR (이슈 #694) | 256x163 RGBA, 원본 크기 유지. 바람 칼날 투사체, 원본과 같은 방향(가는 꼬리 좌하단, 날카로운 끝 우측)으로 고정. 트림 경계에 걸린 파편 조각 끝 픽셀이 alpha 8~80 사이를 오가며 한동안 모서리 alpha 0을 못 맞춰, 리사이즈 후 alpha 임계값을 8에서 26(`key-out-background.py`의 BACKGROUND_ALPHA 0.10과 동일 기준)으로 올려 해결했다. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `TideCall.png` | 클라이언트 PR (이슈 #694) | 256x193 RGBA, 원본 크기 유지. 밀려드는 해일 투사체, Water 팔레트 겹친 반투명 종이층. 원본의 커브 방향을 그대로 유지(투사체 4종 방향 고정 지시 대상은 아니었음). magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `WaterExplosion.png` | 클라이언트 PR (이슈 #694) | 198x256 RGBA, 원본 크기 유지. 간헐천 폭발, 물기둥 받침의 돌 테두리를 찬 회색에서 따뜻한 황갈+이끼로 이동 — STYLE.md의 바위 진영 규칙을 물 마법의 부속 오브젝트에도 적용. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `RockDrop.png` | 클라이언트 PR (이슈 #694) | 125x256 RGBA, 원본 크기 유지. 거석 낙하 투사체, 찬 회색-청색에서 따뜻한 황갈+이끼로 이동. 원본과 같은 방향(넓은 상단, 뾰족한 끝 하단)으로 고정. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `Leafair.png` | 클라이언트 PR (이슈 #694) | 178x256 RGBA, 원본 크기 유지. 잎바람 요정, 개별 잎 다면체 수십 장이 겹쳐 쌓인 테두리가 톱니처럼 보이는 눈물방울형 더미, 위쪽에 흩날리는 낱장 잎 유지. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `Overgrowth.png` | 클라이언트 PR (이슈 #694) | 256x245 RGBA, 원본 크기 유지. 세계수의 과생장, 잎 다면체와 짧은 덩굴 마디가 뒤섞여 둥근 덩어리를 이루고 가장자리로 잎·덩굴손이 삐져나온다. 부드러운 뭉게구름형 원본에서 다면체로 교체. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `RainCloud.png` | 클라이언트 PR (이슈 #694) | 127x256 RGBA, 원본 크기 유지. 비 구름 설치물, 상단의 작은 뭉게구름 + 빗줄기 리본 + 하단의 옅은 지면 타원으로 원본 구도 유지, 페인트 질감 구름에서 다면체로 교체. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `BubbleSpirit.png` | 클라이언트 PR (이슈 #694) | 250x256 RGBA. 물 슬라임 계열, 눈 오른쪽 +35% |
| `SeedSpiritSwarm.png` | 클라이언트 PR (이슈 #694) | 256x224 RGBA. 세계수 풀 정령, 씨앗 몸통에 잎과 꽃 |
| `ThunderSpirit.png` | 클라이언트 PR (이슈 #694) | 256x226 RGBA. 세계수 전기 정령, 금색 팔레트, 눈 오른쪽 +25% |
| `VineSpirit.png` | 클라이언트 PR (이슈 #694) | 180x256 RGBA. 세계수 풀 정령, 덩굴 꼬투리 |
| `WindSpirit.png` | 클라이언트 PR (이슈 #694) | 206x256 RGBA. 세계수 바람 정령, 회청록 팔레트, 눈 오른쪽 +22% |
| `ZapMouse.png` | 클라이언트 PR (이슈 #694) | 256x162 RGBA. 세계수 전기 정령, 금색 가시 등, 눈 오른쪽 +19% |
| `StormRider.png` | 클라이언트 PR (이슈 #694) | 256x221 RGBA. 구름 소환수와 탑승 기사를 같은 기법으로 그렸다 |
| `WillOWisp.png` | 클라이언트 PR (이슈 #694) | 256x180 RGBA, 원본 크기 유지. 세계수 풀 정령, 얼굴 없는 떠다니는 불빛. 사용자가 도깨비불에는 얼굴이 없어야 한다고 정해 눈·입을 넣지 않았다 — STYLE.md 의 눈 문법과 facing right 규칙은 이 자산에 적용하지 않는다, `facing.py` 는 "눈 픽셀 못 찾음"을 정상으로 보고한다. 위로 갈수록 가늘어지는 불꽃 혓바닥 서너 개가 캔버스 폭 전체에 넓게 퍼지고, 아래는 둥글게 뭉친 심지, 가장 밝은 면이 몸통 가운데 와서 안에서 타는 것처럼 읽힌다. 1차 생성은 세로로 좁고 긴 불꽃이라 256x180 가로 비율과 맞지 않아 반려하고, 캔버스 폭 전체로 퍼지는 넓은 실루엣을 프롬프트에 못박아 2차 생성으로 통과시켰다. 잎·덩굴·꽃·씨앗 없음, `SeedSpiritSwarm`·`VineSpirit`과 나란히 놓아도 구별됨. magenta 키 제거, enclosed_pixels=0, 네 모서리 alpha 0, 불투명 픽셀에 magenta 잔색 0, 64px 실루엣 검수 완료 |
| `Cannon.png` | 클라이언트 이슈 #694 재작업 검수 | 256x225 RGBA, 원본 크기 유지. 인간 마법 문명, 차가운 석재+스틸블루+청동 재질. `HumanMagicTower.png` 재질 앵커 사용 |
| `Tower.png` | 클라이언트 이슈 #694 재작업 검수 | 215x256 RGBA, 원본 크기 유지. 인간 마법 문명, `Cannon.png`과 같은 재질 앵커. 대공포탑 실루엣 유지 |
| `BubbleGenerator.png` | 클라이언트 이슈 #694 재작업 검수 | 175x256 RGBA, 원본 크기 유지. 물 슬라임, 틀 재질은 중립 사암색으로 두고 거품 구체에만 `WaterSlime.png` 젤 재질 적용해 세계수 정령과 구분 |
| `Crater.png` | 클라이언트 이슈 #694 재작업 검수 | 256x169 RGBA, 원본 크기 유지. 지옥불 군단, 기존 분화구+용암 균열 개념은 유지하고 `HellfireDemon.png` 재질로 렌더링 기법만 통일 |
| `RallyingTotem.png` | 클라이언트 이슈 #694 재작업 검수 | 109x256 RGBA, 원본 크기 유지. 지옥불 설치물 셰이프 랭귀지 적용 — 좌우 비대칭 뿔과 불꽃 왕관, `HellfireDemon.png` 재질 |
| `HealingTotem.png` | 클라이언트 이슈 #694 재작업 검수 | 188x256 RGBA, 원본 크기 유지. 세계수 풀 정령, 잎 꽃받침에 감싸인 치유광 |
| `LifeTree.png` | 클라이언트 이슈 #694 재작업 검수 | 256x207 RGBA, 원본 크기 유지. 세계수 풀 정령, 축소판 세계수 형태에 마나 옹이 유지 |
| `SeedNest.png` | 클라이언트 이슈 #694 재작업 검수 | 256x179 RGBA, 원본 크기 유지. 세계수 풀 정령, 잎·씨앗·덩굴 둥지. 안쪽 그릇은 비워 소환된 개체가 읽히게 함 |
| `ManaWell.png` | 클라이언트 이슈 #694 재작업 검수 | 256x179 RGBA, 원본 크기 유지. **진영 정정**: 이슈에서 물 계열로 추정했으나 `.art/magic/pages/mana_well.md`의 진영이 세계수 풀 정령이고 기존 아트도 이끼 녹색이라 물이 아니었다. 나뭇잎·덩굴 테두리와 마나빛 웅덩이로 재작업 |
| `ElectricTower.png` | 클라이언트 이슈 #694 재작업 검수 | 200x256 RGBA, 원본 크기 유지. **진영 확인**: `.art/magic/pages/electric_tower.md`와 `.art/magic/README.md`의 진영별 표는 세계수 전기 정령이라고 적지만, 그 표는 `generate-magic-pages.py`의 `faction_for()`가 이름의 부분 문자열로 찍는 값이라 부정확하다고 이미 PR #690에서 확인됐다("electric"이 전기 정령 규칙에 걸림). `STYLE.md:144`의 Humans 절이 "Anchors: `Cannon`, `ElectricTower`"로 이 자산을 인간 진영의 정의 기준으로 못박고 있고 원본 아트도 리벳 박힌 금속+석재였으므로, 자동 생성 표 대신 STYLE.md를 따라 인간 진영 찬 회색 석재+스틸블루+청동으로 그렸다(1차 시도는 세계수 나무+황금 결정으로 잘못 그려 반려). `Cannon.png`·`Tower.png`와 같은 재질, 꼭대기의 전기 구슬과 번개 파편만 Lightning 금색 팔레트로 남겨 원소를 표시한다 |
| `WindTotem.png` | 클라이언트 이슈 #694 재작업 검수 | 256x137 RGBA, 원본 크기 유지. 세계수 바람 정령, 박쥐막 날개를 넓은 곡선 리본으로 교체 |
| `Towerback.png` | 클라이언트 PR (이슈 #694) | 191x256 RGBA. 꼬마돌이 `Tower` 를 등에 결속당한 컨셉으로 다시 그렸다. `MiniRockSwarm.png` 와 `Tower.png` 를 레퍼런스로 넣어 업은 쪽과 실린 쪽이 각각 알아보이게 했다 |
| `CloudDragon.png`, `CloudDragonAttacking.png` | 클라이언트 PR (이슈 #694) | `CloudDragon.png` 256x182, `CloudDragonAttacking.png` 540x182, 둘 다 RGBA. 세계수 정령, 페인트풍 뭉게구름에서 다면체 구름 뭉치로 교체 — 뿔·박쥐막 날개·말린 꼬리·금색 등뼈 돌기는 유지. 공격 프레임은 입에서 물이 실제로 뿜어 나가는 순간이고(머금거나 준비하는 자세 아님), 물줄기는 몸 길이의 절반쯤 되는 굵기로 생성된 크기 그대로 썼다(축소하지 않음). 256x182 캔버스로는 이 굵기의 물줄기가 들어가지 않아 캔버스 폭만 540으로 늘렸다 — 몸통 크기·바닥선·세로 위치·PPU는 `CloudDragon.png`와 동일하고, 폭이 늘어난 만큼은 전부 물줄기 몫이다. `Bottom Center` 피벗은 각 캔버스 폭의 50%를 기준으로 삼으므로, 몸통은 두 캔버스에서 같은 픽셀 오프셋이 아니라 같은 world 오프셋에 오도록 배치했다(자세한 과정과 계산은 `.art/concept/frame-pairs/README.md`, `.art/ANIMATION-ASSETS.md`). `check-replacement.py origin/main`은 캔버스 크기 변경 한 줄만 표시(의도된 변경). `check-frame-pair.py`는 세로 차이 0.000으로 통과하지만 가로 차이가 +0.958 unit(한도 0.05)로 크게 나온다 — 캔버스 하단 10% 띠에 굵은 물줄기가 넓게 걸쳐 있어 "발" 위치 판정이 물 쪽으로 쏠리는 false positive이고, pivot 기준으로 겹쳐 보면 몸통은 픽셀 단위로 일치한다 |
| `shoot/spirit_bomb_beam_segment.png` | 클라이언트 PR (이슈 #714) | 256x139 RGBA, 전부 불투명. spirit bomb 빔의 가운데에서 가로로 반복되는 띠. 새 자산이라 대조할 원본이 없다. 위아래 가장자리가 직선이고 좌우 끝의 단면이 같아 이어 붙여도 이음매가 없다 — 생성 원본에서 셰브런 주기 543px 를 재고 547px 창을 잘랐다. 반복되는 띠는 좌우로 흘러 나가므로 alpha 트림 대상이 아니고, 네 모서리 alpha 0 규칙도 적용하지 않는다. magenta 키 제거, enclosed_pixels=0, 불투명 픽셀에 magenta 잔색 0 |
| `shoot/spirit_bomb_beam_cap.png` | 클라이언트 PR (이슈 #714) | 227x226 RGBA. spirit bomb 빔의 밑동과 끝이 배수만 달리해 함께 쓰는 여덟 갈래 별. 새 자산이라 대조할 원본이 없다. `master-v2/ArcaneImpact.png` 의 결정 배치를 그대로 따르고 색만 금색·풀색으로 바꿨다. 방향이 없어 회전해도 같게 읽힌다. magenta 키 제거, enclosed_pixels=0, 네 모서리 alpha 0, 불투명 픽셀에 magenta 잔색 0, 투명 71.3% |
| `RockTurret.png` | 클라이언트 PR (이슈 #694) | 169x256 RGBA, 원본 크기 유지. 인간 마법 문명, `Cannon.png`·`Tower.png`와 같은 재질 앵커(찬 회색 석재, 스틸블루 금속, 청동 이음쇠, 금색 마름모). 장전 자세, 팔은 넓은 M 자로 벌려 캔버스 폭을 채운다. 이전 시도들은 twin-panel 로 두 자세를 한 캔버스에 같이 뽑다가 공격 프레임의 날아가는 돌이 공유 crop 을 넓혀 본체가 계속 줄었다 — 이번엔 장전 자세를 먼저 단독으로 확정하고, 확정한 이미지를 레퍼런스로 넣어 공격 자세를 별도로 생성했다 |
| `RockTurretAttacking.png` | 클라이언트 PR (이슈 #694) | 169x256 RGBA, 원본 크기 유지. 발사 직후 자세, 탑 자체는 `RockTurret.png`와 픽셀 단위로 동일한 위치·크기다 — 두 프레임을 하나의 공유 crop box(두 alpha bounding box의 합집합)와 하나의 공유 배율로 함께 내보내 지면 접점이 사후 대조가 아니라 제작 방식으로 이미 동일하다. 팔 각도만 낮추고 슬링을 비운 채 돌 하나를 캔버스 안에서 날아가는 중으로 추가했다. `check-frame-pair.py` 통과: 세로 차이 0.000 unit, 가로 차이 -0.001 unit |
| `TreeGolem.png`, `TreeGolem2.png` | 클라이언트 PR (이슈 #694) | 240x256 / 256x215 RGBA. 공유 crop 과 배율로 몸 크기를 맞췄고, 서로 달랐던 spritePixelsToUnits 를 85 로 통일했다. **크기·튐 재작업 중** |
| `DragonTower.png` | `game-assets/dragon-tower.webp` | 웹사이트 동기화 PR #41/#44 로 올라간 뒤 등재를 뒤늦게 기록했다 |
| `FireworkTower.png` | `game-assets/firework-tower.webp` | 위와 같음 |
| `FireworkShell.png` | `game-assets/firework-shell.webp` | 위와 같음 |
| `FireworkExplosion.png` | `game-assets/firework-explosion.webp` | 위와 같음 |
| `FrenzyTotem.png` | `game-assets/frenzy-totem.webp` | 위와 같음 |
| `GrassGenerator.png` | `game-assets/grass-generator.webp` | 위와 같음 |
| `RepairTotem.png` | `game-assets/repair-totem.webp` | 위와 같음 |
| `RockMage.png` | `game-assets/rock-mage.webp` | 위와 같음 |
| `ShockTrap.png` | `game-assets/shock-trap.webp` | 위와 같음 |
| `StormStag.png` | `game-assets/storm-stag.webp` | 위와 같음 |
| `ThunderBirdSwarm.png` | `game-assets/thunder-bird-swarm.webp` | 위와 같음 |
| `WallGolem.png` | `game-assets/wall-golem.webp` | 위와 같음 |
| `MiniRockSwarm.png` | `game-assets/mini-rock-swarm.webp` | 위와 같음 |
| `Background/tree_1.png` | 클라이언트 PR (이슈 #54) | 277x242 RGBA, 원본 크기 유지. 쌍줄기 중간 크기 나무, 씬에서 가장 많이 쓰여 넷 중 가장 중립적인 실루엣이다. magenta 키 제거, 네 모서리 alpha 0, 바닥 여백 4px(옛 4px) |
| `Background/tree_2.png` | 클라이언트 PR (이슈 #54) | 265x222 RGBA, 원본 크기 유지. 오른쪽으로 기운 한 줄기에 둥근 수관. 바닥 여백 0px(옛 0px) |
| `Background/tree_3.png` | 클라이언트 PR (이슈 #54) | 234x218 RGBA, 원본 크기 유지. 넷 중 가장 작은 어린 나무. **내용 폭이 166px 로 옛 232px 보다 28% 좁다** — 프롬프트가 가는 줄기를 요구한 결과이고 높이와 바닥 접점은 맞으나, 나무 줄에서 옛 그림만큼 폭을 채우지 못한다. 같은 PR 에서 나무를 83 그루 더 심어 생긴 틈을 메웠다 |
| `Background/tree_4.png` | 클라이언트 PR (이슈 #54) | 328x264 RGBA, 원본 크기 유지. 넷 중 가장 크고 넓은 나무, 나무 줄의 기준점. 바닥 여백 4px(옛 4px) |
| `Background/rock.png` | 클라이언트 PR (이슈 #54) | 452x166 RGBA, 원본 크기 유지. 이끼 낀 바위 무더기. 찬 회색이 아니라 이끼바위 골렘과 같은 따뜻한 회색 계열을 썼다. 바닥 여백 2px(옛 2px) |
| `Background/background.png` | 클라이언트 PR (이슈 #60) | 1254x1254 RGB 불투명, 타일 가능. `PopupBookGround` 의 `ground.mat` `_MainTex` 이자 9개 씬의 UI 배경. 옛 아트는 1800x1000 비정방형이라 정방형 Plane 에 1.8:1 로 눌렸다. low-poly 테셀레이션에 성긴 흙을 섞어 다시 뽑았고, 색은 `match-colour-to.py` 로 옛 아트의 H/S/V 에 맞춰 스타일만 바뀌고 색감은 유지되게 했다. `ground.mat` 에서 2x 타일링하고 V 를 뒤집어 쓴다 |
| `Background/tree_1.png` | 클라이언트 PR (이슈 #60) | 277x242 RGBA, 원본 크기 유지. faceted low-poly 수관, `MasterStyleKey.png` 를 레퍼런스로 넣어 렌더링 기법을 마스터 스타일에 맞췄다. 색은 옛 아트 기준. 바닥 여백 4px(옛 4px) |
| `Background/tree_2.png` | 클라이언트 PR (이슈 #60) | 265x222 RGBA, 원본 크기 유지. 위와 같음. 바닥 여백 0px(옛 0px) |
| `Background/tree_3.png` | 클라이언트 PR (이슈 #60) | 234x218 RGBA, 원본 크기 유지. 위와 같음. 바닥 여백 4px(옛 4px) |
| `Background/tree_4.png` | 클라이언트 PR (이슈 #60) | 328x264 RGBA, 원본 크기 유지. 위와 같음. 바닥 여백 4px(옛 4px) |
| `Background/rock.png` | 클라이언트 PR (이슈 #60) | 452x166 RGBA, 원본 크기 유지. faceted low-poly 바위 무더기, 이끼가 facet 경계를 따라 각지게 들어간다. 바닥 여백 2px(옛 2px) |
| `SeaSerpentHead.png` | 이슈 #65 대조 | 다면체 새 스타일로 이미 그려져 있다. `다음 교체 후보`에 "대조하지 못했다"로 남아 있던 행을 옮겼다 |
| `SeaSerpentSegment.png` | 이슈 #65 대조 | 위와 같음 |
| `SeaSerpentTail.png` | 이슈 #65 대조 | 위와 같음 |
| `SeaSerpentHydroPump.png` | 이슈 #65 대조 | 위와 같음 |
| `explode/razor_gale_frame_0.png` | 클라이언트 PR (이슈 #76) | 192x155 RGBA, 원본 캔버스 유지. v1 한 번에 통과 — 민트 다면체 리본 넷이 열린 중심을 돌고 바깥 끝은 떨어진 날끝이다. 내용 192x141 로 캔버스 세로의 91.0%: 생성물 비가 1.36 이고 원본이 1.24 라 비율을 지켜 넣으면 세로가 9% 짧다. 늘이지 않고 그대로 뒀다. 바닥 여백 0px(옛 0px) |
| `explode/razor_gale_frame_1.png` | 클라이언트 PR (이슈 #76) | 192x155 RGBA. v1 한 번에 통과. 확정한 frame 0 을 `-i` 로 붙여 뽑았고 두 장의 bbox 가 x 31~1229 대 53~1224, y 203~1081 대 200~1080 으로 겹쳐 등록이 맞았다. 공유 crop box `(31, 200, 1229, 1081)` 와 배율 0.16027 하나로 두 장을 함께 내보냈다. `check-frame-pair.py` 세로 +0.000, 가로 +0.001 unit |
| `explode/electric_explode_frame_0.png` | 클라이언트 PR (이슈 #76) | 192x116 RGBA, 원본 캔버스 유지. v1 한 번에 통과 — 금색 다면체 번개 돔. 이 한 장만 `image_gen` 이 magenta 대신 실제 alpha 를 돌려줘서 key 제거 없이 그대로 썼다. 내용 192x108 로 세로 93.1%. 바닥 여백 0px(옛 0px) |
| `explode/electric_explode_frame_1.png` | 클라이언트 PR (이슈 #76) | 192x116 RGBA. v1 한 번에 통과, magenta 배경이라 key 를 제거했다. 공유 crop box `(19, 334, 1235, 1016)` 와 배율 0.15789. `check-frame-pair.py` 세로 +0.000, 가로 -0.002 unit |
| `Background/grass_1.png` | 클라이언트 PR (이슈 #76) | 238x117 RGBA, 원본 캔버스 유지, 내용 236x107 로 옛 내용과 정확히 같다. v1 반려(비 1.56, 폭이 옛 것의 76%), v2 반려(비 2.02, 폭 91.5% — 8% 한계를 넘었다), v3 통과. **비를 말이 아니라 숫자로 적어야 맞는다** — "twice as wide" 는 2.02 를 돌려줬고 "exactly 2.2 times as wide — 1760 x 800 pixels" 는 2.199 를 돌려줬다. 바닥 여백 5px(옛 5px) |
| `Background/grass_2.png` | 클라이언트 PR (이슈 #76) | 466x99 RGBA, 원본 캔버스 유지, 내용 462x82. v1 한 번에 통과 — 확정한 `grass_1` 을 `-i` 로 붙여 같은 잎 모양과 팔레트를 받았다. 폭은 옛 462 와 같고 세로가 옛 90 보다 8.9% 짧다(생성물 비 5.58 대 원본 5.13). 어느 씬에서도 참조가 없는 자산이라 재생성으로 창을 더 쓰지 않았다. 바닥 여백 7px(옛 7px) |

## 다음 교체 후보

아래는 아직 남은 항목이다. 순서는 실제 작업 시 세계관·프리팹 연결·현재 이미지를
다시 확인한 뒤 정한다. `MagmaExplosion.png`, `Crater.png`, `RallyingTotem.png` 은
완료 표에 행이 있으므로 여기서 뺐다. `drop/leaf_drop.png` 도 뺐다 — 후보 행은
`image_gen` 사용량 한도로 재생성을 못 했다고 적고 있었지만, 실제 파일은 중앙맥이
면 경계로 드러나는 v2 라서 완료 표 쪽이 맞다.

### 옛 스타일로 남은 게임 플레이 자산

2026-09-18 에 `origin/dev`(`94f6628a`)의 PNG 를 아트 pipeline 직전 commit
`8deaad14^`(2026-07-28)과 blob 단위로 대조해 찾았다. 여섯 장이 그때까지 이 문서
어느 표에도 행이 없었고, 이슈 #76 에서 다섯 장을 교체해 `완료` 로 옮겼다.
아래 한 장이 남았다.

| 리소스 | 사용처 | 필요한 작업 |
|---|---|---|
| `Resources/Projectiles/wind.png` | `WindTotem.prefab` 의 `Wind` object, alpha 0.396 | 파란 painterly 줄기다. 본체 `WindTotem.png` 는 민트색 다면체로 교체됐다. **이슈 #76 에서 세 번 생성하고 전부 반려해 옛 그림을 그대로 뒀다** — 아래 참조 |

#### `wind.png` 을 세 번 만에 미룬 이유

생성물 셋 다 스타일은 맞았고 전부 기하에서 반려했다. 원본은 511x91, 가로세로 비
5.615 로 캔버스를 꽉 채운다.

| 시도 | 캔버스 | 내용 비 | 반려 이유 |
|---|---|---|---|
| v1 | 2009x365 (5.5:1) | 11.06 | 날이 너무 얇다. 폭 512 에 맞춰 넣으면 두께가 46px 로 옛 91px 의 절반이다 |
| v2 | 2079x756 (2.75:1) | 2.85 | 두껍고 짧다. 게다가 `image_gen` 이 magenta 대신 alpha 를 돌려주면서 날 주변에 흰 얼룩이 남았다 — 불투명 픽셀의 13.0% |
| v3 | 2080x756 (2.75:1) | 2.81 | magenta 는 깨끗했지만 비가 v2 와 같다. 프롬프트에 "2002 x 364 pixels" 라고 숫자로 적었는데도 무시했다 |

`.art/concept/leftover-effects-prompts/out/projectile-wind-v*.png` 에 셋 다 있다.
v1 은 alpha 덩어리가 정확히 날 넷으로 갈라지므로(281x31, 450x68, 626x105,
671x164, 왼쪽에서 오른쪽으로 커진다) 다시 배치해 붙이는 방법이 남아 있다. 다만
날을 한 줄에 놓으면 가장 큰 날 하나가 671x164 = 4.09:1 이라 전체 5.615 를 만들려면
날들이 서로 깊게 겹친다. 세로로 엇갈려 놓아 띠를 두껍게 하는 쪽이 남은 길이다.

자산당 3회 상한에 걸려 여기서 멈췄다. 폭 512 에 두께 46px 로 넣는 것은 옛 그림
대비 49% 축소라 `check-replacement.py` 의 8% 한계를 크게 벗어나서 택하지 않았다.

`explode/lightning_explode.png` 도 이 표에 있었으나 이슈 #69 에서 지웠다. guid 로
`Assets` 를 grep 해도, 이름으로 client·database·game server 를 grep 해도 참조가
0건이라 교체가 아니라 삭제가 맞았다.

### 이미 등재됐으나 작업이 남은 항목

| 리소스 | 필요한 작업 |
|---|---|
| `TreeGolem.png`, `TreeGolem2.png` | 완료 표에 행이 있으나 **크기·튐 재작업 중**이다. 두 프레임의 지면 접점과 체감 크기를 다시 맞춘다 |

### 기법이 갈린 항목

| 리소스 | 판단할 것 |
|---|---|
| `LightningCloud.png`, `LightningCloudStrike0.png` ~ `LightningCloudStrike5.png` | 일곱 장 모두 둥글고 부드러운 구름 덩어리인데, 같은 구름 소재인 `RainCloud.png` 는 각진 다면체다. 한 게임 안에 구름 기법이 두 가지로 갈려 있다. 이슈 #548 에서 이 형태로 올라온 것이라 반려가 아니라 결정 사항이다 |

### 통일 범위에 넣을지 정할 자산 33장

아래는 baseline 그대로지만 게임 플레이 스프라이트가 아니다. 같은 기준을 댈
대상인지 이 문서에서 정한 적이 없어 목록만 남긴다.

- `Art/Images/UI/Card/` 11장 — `type_fire`·`type_lightning`·`type_nature`·`type_rock`·`type_water`·`type_wind`, `magic_build`·`magic_drop`·`magic_explode`·`magic_shoot`·`magic_spawn`. 평면 vector 아이콘으로 자기들끼리는 일관돼 있다
- `Art/Images/Customize/` 9장 — 모자·망토와 그 아이콘, `check.png`
- `Art/Images/Adventure/` 3장, `Resources/Game/pve/` 3장 — 모험 모드 자산. `pve_vine_witch.png` 는 애니메이션풍 인물이라 화풍 자체가 다르다
- 기타 UI 7장 — `Art/Images/Card.png`, `Art/Images/UI/Catalog.png`·`SpeechBubble.png`·`lockImage.png`, `Art/Images/Obstacle/CircleObstacle.png`, `Resources/UI/ObjectIndicator.png`·`SpeechBubble.png`
- `Art/Images/Background/background.png` — PR #61 에서 교체 중이다

## 전수 대조 방법

표에 행이 없는 자산은 이 문서만 봐서는 드러나지 않는다. 남은 것을 한 번에
세려면 아트 pipeline 이 생기기 직전 commit 과 blob 을 대조한다.

기준 commit 은 `8deaad14^`(2026-07-28) 이다. `8deaad14` 가 `.art/STYLE.md` 와
`.art/anchors/master-v2/` 를 처음 올린 commit 이라, 그 부모까지가 옛 아트다.

`git ls-tree -r <rev> -- Assets` 로 두 판의 PNG 경로와 blob 해시를 모아
비교한다. 해시가 같으면 손대지 않은 자산, 다르면 교체됐거나 크기만 바뀐 것,
기준 commit 에 없으면 pipeline 이후 신규다. `Assets/Plugins/`,
`Assets/WebGLTemplates/`, `Assets/Art/TextMesh Pro/`, `Assets/Art/Splash/` 는
게임 아트가 아니므로 뺀다.

해시가 다르다고 새 스타일인 것은 아니다. 리사이즈나 다른 작업으로 바뀌었을 수
있으므로 눈으로 한 번 더 본다. `Resources/Game/cloud.png` 가 그런 경우다 —
웹사이트에 새 그림이 올라와 완료 행이 있었지만 런타임 PNG 는 옛 구체 그대로였다.
웹사이트 근거만으로 완료를 적을 때는 런타임 PNG 를 한 번 열어 본다.

살아 있는 자산인지는 두 곳에서 확인한다. `Assets` 안에서 쓰는 것은 `.meta` 의
guid 로 prefab·scene 을 grep 하고, `Resources` 아래에서 이름으로 불러 쓰는 것은
database 의 `migration/V001_20260916__baseline.sql` 을 grep 한다. 두 곳 모두에서
안 나오면 죽은 자산이다.

2026-09-18 기준으로 게임 자산 221장 중 119장 교체, 60장 신규, 42장이 기준
commit 그대로였다.

## 크기 검증

캔버스 크기가 원본과 같아도 트림·리사이즈 과정에서 실제 그림이 캔버스 안에
작게 들어갈 수 있다. `image_gen` 사용량 한도로 세션이 끊겼다 이어받을 때도
확인 없이 놓치기 쉽다. commit 전에 항상 돌린다.

바뀐 PNG마다 alpha가 10을 넘는 픽셀의 바운딩 박스를 `origin/main` 판과
대조해서, 원본 박스를 새 박스 안에 비율 유지로 넣을 때 필요한 배율
(`min(원본가로/새가로, 원본세로/새세로)`)이 1.08 이상이면(=8% 이상 작게
그려졌으면) 반려 대상이다. 이 계산을 스크립트로 짜서 (세션 scratchpad는
휴면 세션 사이에 남지 않으므로 매번 새로 짜야 할 수 있다)
`git diff --name-only origin/main..HEAD -- '*.png'`로 바뀐 파일을 모으고
각각 `git show <rev>:<path>`로 두 버전을 읽어 대조한다. 0장이어야 통과다.
2026-09-15 세션에서 적용한 6장(`GiantVine`, `VineToss`, `VineColony`,
`VineWorld`, `LightningExplosion`, `ShockOverload`)은 이 검사를 통과했다.
## 갱신 규칙

1. 웹사이트에 새 이미지가 추가돼 있으면 대응하는 Unity 리소스명을 찾아 `완료`에
   기록한다.
2. 클라이언트에서 먼저 교체했다면 PR 번호와 검수 결과를 근거로 기록한다.
3. 기본/공격/소환 프레임과 오라는 각각 별도 행으로 기록한다.
4. 단순 업스케일이나 포맷 변환은 아트 교체 완료로 기록하지 않는다.
5. 웹사이트 표시용 이미지와 런타임 PNG가 실제로 대응하는지 파일명과 외형을 함께
   확인한다.
