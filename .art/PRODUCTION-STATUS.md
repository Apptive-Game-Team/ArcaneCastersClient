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
| `ArcaneInfantry.png`, `ArcaneInfantryAttacking.png`, `ArcaneInfantryWalkLeft.png`, `ArcaneInfantryWalkRight.png` | 클라이언트 이슈 #372, 2026-10-26 C안 선택 | 양손 마력 칼날 보병. 기본·공격·걷기 2프레임, 128x81 RGBA, PPU 70, Bottom Center. InfantryPoseController가 이동과 공격 우선순위를 관리 |
| `ArcaneInfantryWalk02–04.png`, `ArcaneInfantryWalk06–08.png`, `ArcaneInfantryRecover01–03.png` | 클라이언트 이슈 #648 | 기존 128×81 RGBA, PPU 70, 하단 중앙 피벗. 원본 양발 자세 2장에 체중 이동과 지나가기 6장을 잇고 기존 타격 뒤 칼날 회수 3장을 표시한다. |
| `CraterCarrier.png`, `CraterCarrierDeployed.png`, `CraterCarrierRecovering.png` | 클라이언트 이슈 #334, Unity EditMode 검증 | 분화구 운반수. 192x162 RGBA 공통 캔버스, PPU 110, Bottom Center. 운반·설치 완료·설치/회수 자세. 서버 상태로 전환하며 기본 hopping/attack swap 제거 |
| `AquaArcher.png` | `concept-art/aqua-archer-drawn.webp` | 물의 궁수 기본 프레임 |
| `AquaArcherAttack.png` | `concept-art/aqua-archer-release.webp` | 화살을 놓은 공격 프레임 |
| `ChickenCommando.png` | `concept-art/chicken-commando.webp`, `game-assets/chicken-commando.webp` | 인간 공수 특공대 방향 적용 |
| `DimensionToad.png` | `concept-art/dimension-toad.webp`, `game-assets/dimension-toad.webp` | 차원 유랑종 경계 운반자 방향 적용 |
| `EmberSpiritSwarm.png` | `concept-art/ember-spirit-swarm.webp` | 지옥불 악마병 무리로 교체 |
| `EvilEnt.png` | 웹사이트 PR #29 `game-assets/evil-ent-idle.webp` | 사악한 나무 골렘 기본 자세 |
| `EvilEnt2.png` | 웹사이트 PR #29 `game-assets/evil-ent-attack.webp` | 팔을 뻗은 공격 자세 |
| `FireChildSpirit.png` | `concept-art/fire-child-spirit.webp` | 하급 악마 방향 적용 |
| `FireChildSpiritHover01–06.png`, `FireChildSpiritRelease01–04.png` | 클라이언트 PR (이슈 #634) | 228×256 RGBA, PPU 200, 하단 중앙 피벗. 날개 판과 꼬리의 대기 6장 및 불꽃 발사·복귀 4장. |
| `FireLordSpirit.png` | `concept-art/fire-lord-spirit.webp` | 지옥불 군단 지휘관으로 교체 완료 |
| `FireLordSpiritHover01–06.png`, `FireLordSpiritApproach01–06.png` | 클라이언트 PR (이슈 #636) | 256×162 RGBA, PPU 100, 하단 중앙 피벗. 외투와 갑각의 대기 6장, 수평 위치 갱신에 맞춘 접근 6장. |
| `FireSpirit.png` | `concept-art/fire-spirit.webp` | 하급 뿔 악마 방향 적용 |
| `FireSpiritWalk01–08.png`, `FireSpiritSpray01–04.png` | 클라이언트 이슈 #640 | 256×190 RGBA, PPU 200, 하단 중앙 피벗. 앞뒤 발 교대 보행 8장과 분사·복귀 4장. |
| `FireTadpole.png` | `concept-art/fire-tadpole.webp` | 차원 유랑종 화산편 방향 적용 |
| `RockGolem.png` | `concept-art/rock-golem.webp` | 이끼바위 골렘 기본 자세 |
| `RockGolem2.png` | `game-assets/rock-golem-attack.webp` | 이끼바위 골렘 공격 자세. 이슈 #514에서도 공격 이벤트의 첫 자세로 사용 |
| `RockGolemWalk01–08.png`, `RockGolemWalk04To05.png`, `RockGolemRecover01–03.png` | 클라이언트 이슈 #514 | 256×244, PPU 100, Bottom Center. 보행 9장 중 4번과 5번 사이에 먼 발을 들어 앞으로 내미는 자세가 들어간다. 지면 이동 0.66칸마다 보행 한 바퀴. 펀치 복귀 3장을 추가해 기존 0.2초 안에서 재생한다. |
| `RockRemnant.png` | `game-assets/rock-remnant.webp` | 사망 후 이동 방해 잔해 |
| `EarthCall.png` | 클라이언트 PR #181 검수 | 돌 골렘 재사용 이미지를 돌무더기 각성 마법 전용 아트로 교체. `MasterStyleKey.png`·`RockGolem.png`·`ArcaneImpact.png` 고정 앵커를 사용한 2.5D cut-paper 효과다. v2는 내용 비율이 1.01이라 기존 0.89보다 넓어 반려했고, v3는 생성물 0.867·최종 내용 207x238로 원본 212x238과 2.4% 이내다. 최종 256x244 RGBA, Bottom Center, 바닥 여백 0px, 네 모서리 alpha 0, 투명 60.8%, 불투명 픽셀의 magenta 잔색 0, 64px 실루엣 검수 완료 |
| `TitanRemnant.png` | 클라이언트 이슈 #593 재작업 검수 | 거신의 잔해 본체 |
| `TitanFist.png` | 클라이언트 이슈 #593 재작업 검수 | 주변 적 위치에서 솟구치는 거대한 돌주먹 |
| `LightningTadpole.png` | `concept-art/lightning-tadpole.webp` | 차원 유랑종 폭풍편 방향 적용 |
| `MagmaSpirit.png` | `concept-art/magma-spirit-idle.webp` | 기본 자세 |
| `MagmaSpiritSpawn01–04.png`, `MagmaSpiritWalk01–08.png`, `MagmaSpiritCast01–04.png` | 클라이언트 PR (이슈 #638) | 256×253 RGBA, PPU 80, 하단 중앙 피벗. 지면 돌파 4장, 양발 보행 8장, 주먹 소환 자세 4장. |
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
| `PlayerCharacterBase.png`, `PlayerCharacterStaffRaised.png`, `PlayerCharacterAttack.png` | 클라이언트 PR (이슈 #116) | 지팡이 머리만 앱 아이콘의 말린 지팡이로 교체. 981x1245 RGBA 캔버스와 몸 픽셀은 원본 그대로, 세 머리는 `.art/tools/transplant-staff-head.py` 로 붙인 같은 픽셀. 비교 시트 `.art/concept/player-staff/staff-compare.png`, `staff-compare-64.png`. Unity Editor 검수는 남아 있다 |
| `Assets/Resources/PlayerAppearances/<storm,blaze,summoner,golem,grass,tide>/{idle,raised,attacking}.png`, `default/` | 클라이언트 PR (bot 외형) | 상대 bot 용 외형 여섯 벌과 default 복사본. 981x1245 RGBA, 눈 중심과 발 아래 끝을 default 자세에 맞춤. 기존 `PlayerCharacter*.png` 는 그대로다. 비교 시트 `.art/concept/bot-appearances/contact-sheet-128.png`. Unity Editor 검수는 남아 있다 |
| `Assets/Resources/PlayerAppearances/corrupted_finn/{idle,raised,attacking}.png` | 클라이언트 이슈 #450 | 불차원 시작 외형. 세 자세에서 맨손으로 지팡이를 쥐고, 공격할 때 그 팔이 가슴 앞을 가로지른다. 눈 중심과 발의 높이를 맞췄다. |
| `SeaSerpent.png` | 클라이언트 PR (이슈 #672) | 201x256 RGBA. magic book 이 빈 칸이던 신규 아이콘. 물 소환수 문법, 곧추선 몸통에 두 겹 똬리. Unity Editor 검수는 남아 있다 |
| `BoulderStrike.png` | 클라이언트 PR (이슈 #672) | 256x146 RGBA. 밀려 나가는 바위와 뒤따르는 초승달 카드 아트. 전투 프리팹은 `Game/sprites/BoulderStrike.png` 를 사용한다 |
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
| `electric_field.png`, `electric_field_frame_02.png`–`electric_field_frame_04.png` | 클라이언트 이슈 #488 | 192×84 RGBA, PPU 100, Bottom Center. 기존 타원 알파와 중앙 그을음을 유지하고 가장자리 전기를 0.18초 간격으로 4프레임 반복한다. Unity 플레이 검수 대기 |
| `fire_field.png`, `fire_field_frame_02.png`–`fire_field_frame_04.png` | 클라이언트 이슈 #484 | 192×68 RGBA, PPU 100, Bottom Center. 기존 중앙 그을음 위로 테두리 불꽃이 0.18초 간격으로 4프레임 반복한다. Unity 플레이 검수 대기 |
| `leaf_field.png`, `leaf_field_frame_02.png`–`leaf_field_frame_04.png` | 클라이언트 이슈 #490 | 192×79 RGBA, PPU 100, Bottom Center. 중앙의 기존 잎과 공통 바닥선을 유지하고 가장자리 잎을 0.24초 간격으로 4프레임 반복한다. Unity 플레이 검수 대기 |
| `water_field.png`, `water_field_frame_02.png`–`water_field_frame_04.png` | 클라이언트 이슈 #486 | 192×69 RGBA, PPU 100, Bottom Center. 기존 타원 알파와 중앙 물빛을 유지하고 가장자리 물결을 0.24초 간격으로 4프레임 반복한다. Unity 플레이 검수 대기 |
| `crater_ember.png` | 클라이언트 이슈 #681 재작업 검수 | 64x58 RGBA. 분화구에서 튄 불씨 하나, 둥근 물방울 실루엣에 면 3~4개, 가장 밝은 면이 중앙 |
| `crater_ember_frame_01.png`–`crater_ember_frame_04.png` | 클라이언트 이슈 #494 | 64×58 RGBA, PPU 200, Bottom Center. 비행 중 주황색 면이 밝아졌다 잦아드는 4프레임을 0.08초 간격으로 반복한다. 서버 비행 경로와 착지 후 불 장판은 기존 동작을 따른다 |
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
| `razor_gale_frame_0.png`–`razor_gale_frame_3.png` | 클라이언트 이슈 #496 | 192×155 RGBA, PPU 100, Bottom Center. 공통 하단에 맞춘 민트색 회전 4프레임을 기존 0.12초 간격으로 반복한다 |
| `SandStorm.png` | 클라이언트 PR (이슈 #694) | 159x256 RGBA, 원본 크기 유지. 모래 폭풍, Wind 진영 소속이지만 모래라는 재질 때문에 따뜻한 황토색(`#D8B978`~`#6E4E22`)을 그대로 유지 — 바위 골렘의 이끼 낀 황갈과는 톤을 다르게 잡아 구분된다. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `SandStormFrame01.png`–`SandStormFrame04.png` | 클라이언트 이슈 #492 | 159×256 RGBA, 기존 PPU와 Bottom Center. 지면 끝을 같은 위치에 두고 내부 모래 띠를 0.22초 간격으로 반복한다. 기존 흔들림·불투명도 적용. Unity 플레이 검수 대기 |
| `TornadoStrike.png` | 클라이언트 PR (이슈 #694) | 224x256 RGBA, 원본 크기 유지. 회오리 정령, 위가 넓고 아래로 갈수록 좁아지는 깔때기. enclosed_pixels=26453는 리본 사이 트인 중심부, 불투명 픽셀 magenta 잔색 0px. magenta 키 제거, 네 모서리 alpha 0, `shrink.py`/`bottom.py` 검수 통과 |
| `WindBlade.png` | 클라이언트 PR (이슈 #694) | 256x163 RGBA, 원본 크기 유지. 바람 칼날 투사체, 원본과 같은 방향(가는 꼬리 좌하단, 날카로운 끝 우측)으로 고정. 트림 경계에 걸린 파편 조각 끝 픽셀이 alpha 8~80 사이를 오가며 한동안 모서리 alpha 0을 못 맞춰, 리사이즈 후 alpha 임계값을 8에서 26(`key-out-background.py`의 BACKGROUND_ALPHA 0.10과 동일 기준)으로 올려 해결했다. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `TideCall.png` | 클라이언트 PR (이슈 #694) | 256x193 RGBA, 원본 크기 유지. 밀려드는 해일 투사체, Water 팔레트 겹친 반투명 종이층. 원본의 커브 방향을 그대로 유지(투사체 4종 방향 고정 지시 대상은 아니었음). magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `WaterExplosion.png` | 클라이언트 PR (이슈 #694) | 198x256 RGBA, 원본 크기 유지. 간헐천 폭발, 물기둥 받침의 돌 테두리를 찬 회색에서 따뜻한 황갈+이끼로 이동 — STYLE.md의 바위 진영 규칙을 물 마법의 부속 오브젝트에도 적용. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `RockDrop.png` | 클라이언트 PR (이슈 #694) | 125x256 RGBA, 원본 크기 유지. 거석 낙하 투사체, 찬 회색-청색에서 따뜻한 황갈+이끼로 이동. 원본과 같은 방향(넓은 상단, 뾰족한 끝 하단)으로 고정. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `Leafair.png` | 클라이언트 PR (이슈 #694) | 178x256 RGBA, 원본 크기 유지. 잎바람 요정, 개별 잎 다면체 수십 장이 겹쳐 쌓인 테두리가 톱니처럼 보이는 눈물방울형 더미, 위쪽에 흩날리는 낱장 잎 유지. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `Overgrowth.png` | 클라이언트 PR (이슈 #694) | 256x245 RGBA, 원본 크기 유지. 세계수의 과생장, 잎 다면체와 짧은 덩굴 마디가 뒤섞여 둥근 덩어리를 이루고 가장자리로 잎·덩굴손이 삐져나온다. 부드러운 뭉게구름형 원본에서 다면체로 교체. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `RainCloud.png` | 클라이언트 PR (이슈 #694) | 127x256 RGBA, 원본 크기 유지. 비 구름 설치물, 상단의 작은 뭉게구름 + 빗줄기 리본 + 하단의 옅은 지면 타원으로 원본 구도 유지, 페인트 질감 구름에서 다면체로 교체. magenta 키 제거, 네 모서리 alpha 0, magenta 잔색 0px, `shrink.py`/`bottom.py` 검수 통과 |
| `BubbleSpirit.png` | 클라이언트 PR (이슈 #694) | 250x256 RGBA. 물 슬라임 계열, 눈 오른쪽 +35% |
| `BubbleSpiritHover01–06.png`, `BubbleSpiritRelease01–04.png` | 클라이언트 PR (이슈 #630) | 250×256 RGBA, PPU 200, 하단 중앙 피벗. 물결과 뒤 물방울의 대기 6장과 거품 발사·복귀 4장. |
| `SeedSpiritSwarm.png` | 클라이언트 PR (이슈 #694) | 256x224 RGBA. 세계수 풀 정령, 씨앗 몸통에 잎과 꽃 |
| `ThunderSpirit.png` | 클라이언트 PR (이슈 #694) | 256x226 RGBA. 세계수 전기 정령, 금색 팔레트, 눈 오른쪽 +25% |
| `ThunderSpiritHover01–06.png`, `ThunderSpiritRelease01–04.png` | 클라이언트 PR (이슈 #632) | 256×226 RGBA, PPU 200, 하단 중앙 피벗. 금빛 윗부분과 꼬리 대기 6장 및 지상 발사·복귀 4장. |
| `VineSpirit.png` | 클라이언트 PR (이슈 #694) | 180x256 RGBA. 세계수 풀 정령, 덩굴 꼬투리 |
| `VineSpiritWalk01–08.png`, `VineSpiritRelease01–04.png` | 클라이언트 PR (이슈 #628) | 180×256 RGBA, PPU 200, 하단 중앙 피벗. 이동 거리로 밑동 여덟 자세를 반복하고, 개별 공격 이벤트로 씨앗 발사와 복귀 네 자세를 재생한다. |
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
| `CloudDragon.png`, `CloudDragonHover01–06.png`, `CloudDragonRelease01–04.png` | 클라이언트 이슈 #626 | 대기 6장과 발사·회수 4장. 256×182 RGBA, PPU 116.363636, Bottom Center. 눈과 구름 중심을 맞추고 날개와 턱을 그렸다. 실제 물·번개 발사 틱에서 01부터 재생한다. 기존 540×182 물 분사 그림은 운룡 변형에서 연결을 해제했다. |
| `shoot/spirit_bomb_beam_segment.png` | 클라이언트 PR (이슈 #714) | 256x139 RGBA, 전부 불투명. spirit bomb 빔의 가운데에서 가로로 반복되는 띠. 새 자산이라 대조할 원본이 없다. 위아래 가장자리가 직선이고 좌우 끝의 단면이 같아 이어 붙여도 이음매가 없다 — 생성 원본에서 셰브런 주기 543px 를 재고 547px 창을 잘랐다. 반복되는 띠는 좌우로 흘러 나가므로 alpha 트림 대상이 아니고, 네 모서리 alpha 0 규칙도 적용하지 않는다. magenta 키 제거, enclosed_pixels=0, 불투명 픽셀에 magenta 잔색 0 |
| `shoot/spirit_bomb_beam_cap.png` | 클라이언트 PR (이슈 #714) | 227x226 RGBA. spirit bomb 빔의 밑동과 끝이 배수만 달리해 함께 쓰는 여덟 갈래 별. 새 자산이라 대조할 원본이 없다. `master-v2/ArcaneImpact.png` 의 결정 배치를 그대로 따르고 색만 금색·풀색으로 바꿨다. 방향이 없어 회전해도 같게 읽힌다. magenta 키 제거, enclosed_pixels=0, 네 모서리 alpha 0, 불투명 픽셀에 magenta 잔색 0, 투명 71.3% |
| `RockTurret.png` | 클라이언트 PR (이슈 #694) | 169x256 RGBA, 원본 크기 유지. 인간 마법 문명, `Cannon.png`·`Tower.png`와 같은 재질 앵커(찬 회색 석재, 스틸블루 금속, 청동 이음쇠, 금색 마름모). 장전 자세, 팔은 넓은 M 자로 벌려 캔버스 폭을 채운다. 이전 시도들은 twin-panel 로 두 자세를 한 캔버스에 같이 뽑다가 공격 프레임의 날아가는 돌이 공유 crop 을 넓혀 본체가 계속 줄었다 — 이번엔 장전 자세를 먼저 단독으로 확정하고, 확정한 이미지를 레퍼런스로 넣어 공격 자세를 별도로 생성했다 |
| `RockTurretAttacking.png` | 클라이언트 PR (이슈 #694) | 169x256 RGBA, 원본 크기 유지. 발사 직후 자세, 탑 자체는 `RockTurret.png`와 픽셀 단위로 동일한 위치·크기다 — 두 프레임을 하나의 공유 crop box(두 alpha bounding box의 합집합)와 하나의 공유 배율로 함께 내보내 지면 접점이 사후 대조가 아니라 제작 방식으로 이미 동일하다. 팔 각도만 낮추고 슬링을 비운 채 돌 하나를 캔버스 안에서 날아가는 중으로 추가했다. `check-frame-pair.py` 통과: 세로 차이 0.000 unit, 가로 차이 -0.001 unit |
| `RockTurretFire01–04.png` | 클라이언트 이슈 #646 | 169×256 RGBA, PPU 133⅓, 하단 중앙 피벗. 고정 몸체 위에서 빈 슬링 발사·반동·복귀·재장전을 표시한다. 발사 돌은 런타임 `RockShot`으로 그린다. |
| `TreeGolem.png`, `TreeGolem2.png`, `TreeGolemWalk01–08.png`, `TreeGolemRecover01–03.png` | 클라이언트 이슈 #482·#512 | 240×256 보행 8장과 312×256 펀치 복귀 3장을 추가했다. PPU 85, Bottom Center. 기존 펀치를 공격 이벤트에서 즉시 표시하고 0.2초 안에 복귀한다. XZ 이동 0.9칸마다 보행 한 바퀴를 재생한다. |
| `DragonTower.png` | `game-assets/dragon-tower.webp` | 웹사이트 동기화 PR #41/#44 로 올라간 뒤 등재를 뒤늦게 기록했다 |
| `DragonTowerFlame01–04.png` | 클라이언트 이슈 #704 | 161×256 RGBA, PPU 133.333333, 하단 중앙 피벗. 화염탄 발사 때 턱과 입 안의 불빛이 0.48초 동안 네 자세를 거친다. |
| `FireworkTower.png` | `game-assets/firework-tower.webp` | 위와 같음 |
| `FireworkShell.png` | `game-assets/firework-shell.webp` | 위와 같음 |
| `FireworkShellFlight01–04.png` | 클라이언트 이슈 #700 | 160×109 RGBA, PPU 100, 피벗 (96, 0). 비행 진행률에 맞춰 발사·상승·정점·하강 때 포탄 뒤 불티를 바꾼다. |
| `FireworkExplosion.png` | `game-assets/firework-explosion.webp` | 위와 같음 |
| `FireworkBurst01–07.png` | 클라이언트 이슈 #702 | 254×238 RGBA, PPU 100, 하단 중앙 피벗. 착탄 때 다각형 파편과 불씨의 확산을 재생하고 0.80초부터 투명해진다. |
| `FrenzyTotem.png` | `game-assets/frenzy-totem.webp` | 위와 같음 |
| `GrassGenerator.png` | `game-assets/grass-generator.webp` | 위와 같음 |
| `RepairTotem.png` | `game-assets/repair-totem.webp` | 위와 같음 |
| `RockMage.png` | `game-assets/rock-mage.webp` | 위와 같음 |
| `RockMageWalk01–08.png`, `RockMageCast01–04.png` | 클라이언트 이슈 #642 | 234×256 RGBA, PPU 200, 하단 중앙 피벗. 두 발 보행 8장과 지팡이 발사·복귀 4장. |
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
| `pve/pve_nature_slime_nest.png` | 클라이언트 PR (이슈 #169) | 192x142 RGBA, 원본 캔버스 유지. `PveNatureSlimeNest` 모험 보스. 애니메이션풍 유광 렌더링을 다면체 cut-paper 로 다시 그렸다. v1 반려(둥근 실루엣 가로세로비 0.997, 옛 1.35 대비 지나치게 좁고 높음), v2 통과(비 1.54, 바닥 정렬 0px). alignment 7(bottom-center) 유지 |
| `pve/pve_water_slime_nest.png` | 클라이언트 PR (이슈 #169) | 192x158 RGBA, 원본 캔버스 유지. `PveWaterSlimeNest` 모험 보스. 위와 같은 이유로 다시 그렸다. v1 반려(비 1.59, 옛 1.22 대비 지나치게 낮고 넓음), v2 통과(비 1.21, 폭 99.5%·세로 100%). alignment 7 유지 |
| `pve/pve_vine_witch.png` | 클라이언트 PR (이슈 #169) | 192x155 RGBA, 원본 캔버스 유지. `PveVineWitch` 모험 보스, 숲의 마녀. 애니메이션풍 인물이라 화풍 자체가 달랐던 것을 다면체 cut-paper 로 다시 그렸다. v1 한 번에 통과 — 빨간 장발·잎 후드·지팡이 실루엣 유지, `facing.py` 기준 눈 중심이 몸통 중심보다 오른쪽(오른쪽 방향 확인). alignment 7 유지 |
| `Adventure/gate_adventure_icon.png` | 클라이언트 PR (이슈 #508) | **모험 화면 검수 대기.** 5·6단계 지도와 같은 무광 석재를 적용하고 주황 불꽃과 보라 번개를 유지했다. 256×183 RGBA와 GateAdventure의 Sprite 참조를 확인했다. 32px와 64px에서 원소 모양을 구분할 수 있고 불투명 초록 픽셀과 가장자리 초록 번짐은 검출되지 않았다. |
| `Adventure/gate_adventure_map_stage5.png` | 클라이언트 PR (이슈 #504) | **모험 화면 검수 대기.** 무광 절개면으로 회색 암반을 다시 그리고 불꽃·번개 차원문 다섯 개의 배치를 유지했다. 867×256 RGB, Stage5와 준비 화면 참조 유지. 비교한 가로 단면 6곳에서 길 중심 차이는 최대 2px이고 폭은 기존 79–83px에서 19–21px로 줄었다. |
| `Adventure/gate_adventure_map_stage6.png` | 클라이언트 PR (이슈 #506) | **모험 화면 검수 대기.** 5단계 지도와 같은 무광 암반을 적용했다. 길 끝의 속 빈 그루터기와 주황·보라 차원문 두 개를 유지했다. 867×256 RGB와 Stage6의 Sprite 참조를 확인했다. 비교한 가로 단면 6곳에서 길 중심 차이는 최대 2.5px이고 폭은 기존 72–75px에서 21–22px로 줄었다. |
| `Background/Gate/background.png` | 클라이언트 PR (이슈 #510) | **플레이 검수 대기.** 차원문 5·6단계 지도와 같은 무광 암반 면을 적용하고 어두운 균열의 밀도를 줄였다. 1254×1254 RGB와 Repeat 임포트 설정을 유지했다. 양쪽 가장자리의 RGB 값이 행·열 단위로 일치하며 2×2 반복 위에 차원문 유닛 4종을 합성해 확인했다. |
| `Background/Gate/tree_1.png` | 클라이언트 PR (이슈 #520) | **실제 경기 검수 대기.** 끝이 부러진 줄기와 기울어진 실루엣을 살린 무광 컷페이퍼 아트다. 277×242 RGBA, 기존 GUID·임포트와 알파 가로 외곽 및 바닥 높이를 유지했다. 알파 외곽의 위쪽은 3px 낮아졌다. |
| `Background/Gate/tree_2.png` | 클라이언트 PR (이슈 #519) | **실제 경기 검수 대기.** 양쪽으로 갈라지는 가지와 그을린 줄기를 살린 무광 컷페이퍼 아트다. 265×222 RGBA, 기존 GUID·임포트와 알파 가로 외곽 및 바닥 높이를 유지했다. 알파 외곽의 위쪽은 1px 낮아졌다. |
| `Background/Gate/tree_3.png` | 클라이언트 PR (이슈 #515) | **실제 경기 검수 대기.** 차원문 6단계 지도와 맞춘 무광 그루터기다. 열린 윗부분과 돌출된 뿌리, 어두운 주황 균열을 남겼다. 234×218, PPU 100, 하단 중앙 피벗. 알파 바닥선은 기존과 같고 외곽 폭은 207px에서 201px로 줄었다. |
| `Background/Gate/tree_4.png` | 클라이언트 PR (이슈 #518) | **실제 경기 검수 대기.** 굽은 큰 가지와 넓게 뻗은 뿌리을 살린 무광 컷페이퍼 아트다. 328×264 RGBA, 기존 GUID·임포트와 알파 가로 외곽 및 바닥 높이를 유지했다. 알파 외곽의 위쪽은 4px 낮아졌다. |
| `Background/Gate/rock.png` | 클라이언트 PR (이슈 #521) | **실제 경기 검수 대기.** 낮고 넓게 쌓인 바위와 옅은 균열을 살린 무광 컷페이퍼 아트다. 452×166 RGBA, 기존 GUID·임포트와 알파 가로 외곽 및 바닥 높이를 유지했다. 알파 외곽의 위쪽은 5px 낮아졌다. |
| `Background/Gate/grass_1.png` | 클라이언트 PR (이슈 #526) | **실제 관전 화면 검수 대기.** 차원문 암반에 맞는 무광 재색 풀과 작은 불씨 두 개. 238×117, PPU 100, 하단 중앙 피벗. 알파 좌우와 바닥 높이는 같고 끝은 기존보다 11px 낮다. GATE 관전 테마의 `grass_1` 배치 5곳에서 사용한다. |
| `Background/Gate/grass_2.png` | 클라이언트 PR (이슈 #235) | **검수 대기, 자리표시 아트.** 차원문 모험(adventure 3) 전투 배경 theme 의 새 파일(`Background/Fortress/grass_2.png` 와 같은 이름·크기). 466x99 RGBA, 마른 풀 다섯 포기 줄. `GateBattleTheme.asset` 이 참조하고 `GateAdventure.asset` 의 `battleTheme` 이 이 asset 을 가리킨다. `agy` 생성, 초록 배경 key 제거. Editor 미검증 |
| `UI/Card/type_fire.png`, `type_water.png`, `type_nature.png`, `type_lightning.png`, `type_rock.png`, `type_wind.png` | 클라이언트 PR (이슈 #304) | **검수 대기.** 원소 아이콘 6장. 마법서 상성표, 도감 원소 칩, 덱 카드가 `CardImageMapper.asset` 으로 쓴다. 181x256 · 180x256 · 136x256 · 161x256 · 256x192 · 256x195 RGBA(옛 128 긴 변의 두 배), 여백 없이 잘랐다. 여섯 장을 1536x1024 한 캔버스의 3x2 격자에 같이 그리게 해 한 번에 통과했다(`codex` `image_gen` 1회). 레퍼런스는 `MasterStyleKey.png`, `ArcaneImpact.png`, 원소 룬 여섯 장 합성. magenta key 제거, 네 모서리 alpha 0, 불투명 픽셀에 magenta 잔색 0. 쓰는 곳이 모두 `preserveAspect` 인 UI `Image` 라 화면 크기는 RectTransform 과 가로세로 비로만 정해지고 픽셀 수·PPU 와 무관하다 — `.meta` 는 그대로 뒀다. 가로세로 비는 다섯 장이 옛 비의 8% 안이다. `type_wind` 만 1.88 에서 1.31 로 바뀌어 상성표 칸 안에서 옛 그림보다 세로로 커진다(옛 바람이 여섯 중 가장 작아 보였다). 프롬프트는 1.9 를 숫자로 요구했지만 격자 한 칸 안에서는 지켜지지 않았다 |
| `PveBrickGolem.png`, `PveBrickGolem2.png` | 클라이언트 PR (이슈 #329) | **검수 대기, 새 파일.** 요새 마지막 보스 벽돌 골렘의 기본 자세(두 주먹을 땅에 댄 자세)와 공격 자세(한 팔을 높이 든 자세). 256x251 RGBA, PPU 70, Bottom Center. 두 자세를 한 캔버스의 twin panel 로 한 번에 생성(v1 한 번에 통과)하고 공유 crop box `(0, 19, 884, 887)` 와 공유 배율 0.2896 으로 잘랐다. 눈은 몸통 중심보다 오른쪽(파일은 오른쪽을 보고, 경기에서는 RightPlayer 라 뒤집혀 왼쪽을 본다). magenta key 제거, 불투명 픽셀의 magenta 잔색 0, 네 모서리 alpha 0. 공격 자세는 잔해 바닥이 기본 자세보다 3px 위에서 끝난다. `.art/concept/brick-golem/README.md` |
| `PveBrickGolemSweep01–04.png` | 클라이언트 PR (이슈 #530) | **실제 경기 검수 대기.** 벽돌 골렘이 낮게 팔을 휩쓸고 바닥으로 돌려놓는 네 자세. 256×251 RGBA, PPU 70, 하단 중앙 피벗과 바닥 높이를 맞췄다. `BrickGolemSweepPose`의 출발 ID로 보스를 찾아 도착점 방향을 보고 신호의 지속 시간 동안 재생한다. |
| `BeanShooter.png`, `BeanShooterAttack.png`, `BeanShooterWalk01–08.png`, `BeanShooterRecover01–02.png` | 클라이언트 PR (이슈 #532) | **실제 경기 검수 대기.** 네 뿌리발이 각각 들리고 바닥을 짚는 보행 여덟 자세와 줄기 복귀 두 자세를 적용했다. 238×256 RGBA, PPU 200, 하단 중앙 피벗. `BeanShooter.prefab`이 0.6칸 단위로 보행하고 0.08초 발사 자세 뒤 두 복귀 자세를 표시한다. |
| `explode/brick_golem_crack_ring.png`, `shoot/brick_golem_sweep.png` | 클라이언트 PR (이슈 #329) | **검수 대기.** 내려찍기 충격의 바닥 균열 고리(255×256, 중심 피벗)와 휩쓸기 궤적(256×144, 중심 피벗). 한 캔버스에서 두 장을 생성했다. 마젠타 잔색 0, 네 모서리 알파 0. |
| `explode/brick_golem_slam_warning_01–04.png` | 클라이언트 PR (이슈 #528) | **실제 경기 검수 대기.** 벽돌 골렘의 내려찍기 예고 전용 황록색 균열 4장. 서버가 전송한 예고 시간을 네 구간으로 나눠 표시한다. 255×256, PPU 100, 중앙 피벗. 프레임별 알파 영역의 첫 프레임 대비 겹침 비율은 97.9% 이상이다. |
| `sprites/StoneGargoyle.png` | 돌 가고일 신규 유닛, 2026-11-05 사용자 승인 | 192x176 RGBA, PPU 100, Bottom Center. 돌 부족 70%와 지옥불 차원 영향 30%. 날개 끝·어깨·발톱의 그을린 돌과 붉은 균열, 붉은 눈·이끼 유지. `StoneGargoyle.prefab`에 연결. blue key `--deadzone 4`, enclosed_pixels=0, 네 모서리 alpha 0. `.art/concept/stone-gargoyle/README.md` |
| `sprites/StoneGargoyleFly1.png`, `StoneGargoyleFly2.png`, `StoneGargoyleFly3.png` | 2026-11-05 사용자 요청 및 12321 왕복 선택 | 날개 올림·수평·내림 3장, 192x165 RGBA, PPU 100, Bottom Center, Full Rect. 기존 `SpriteFrameAnimator`의 `1,2,3,2` 반복, 프레임 0.16초. 공유 크롭·배율·발톱 하단 정렬. `.art/concept/stone-gargoyle-wings/README.md` |

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
아래 한 장은 이슈 #475에서 애니메이션으로 교체하고 플레이 검수를 기다린다.

| 리소스 | 사용처 | 필요한 작업 |
|---|---|---|
| `Resources/Projectiles/wind.png`, `wind_totem_flow_02.png`–`wind_totem_flow_08.png` | `WindTotem.prefab`의 `Wind` SpriteRenderer | 클라이언트 이슈 #475. 승인한 민트색 다면체 바람 8프레임 연결. 512×91 RGBA, PPU 140, 중앙 피벗. `SpriteFrameAnimator`가 0.175초 간격으로 1.4초간 반복한다. Unity 플레이 검수 대기 |
| `Effect/AreaOfEffect/RallyZone.png`, `RallyZoneEmber01–08.png` | `RallyingTotem.prefab`의 `RallyZone` SpriteRenderer | 클라이언트 이슈 #534. 기존 돌 고리의 알파와 512×512 RGBA, PPU 400, 중앙 피벗을 기준으로 불빛 여덟 장을 그렸다. 실효 범위의 크기를 고정하고 0.22초 간격으로 불빛을 이어 표시한다. 실제 경기 검수 대기 |
| `Effect/AreaOfEffect/RepairAura.png`, `RepairAuraLight02–06.png` | `RepairTotem.prefab`의 `RepairAura` SpriteRenderer | 클라이언트 이슈 #536. 512×512 RGBA, PPU 400, 중앙 피벗과 같은 알파를 가진 잎 불빛 여섯 장. 바깥 잎끝을 중심에서 약 252px에 정렬했다. `effect_radius` 값에 따라 고리를 XZ 바닥에 배치하고 0.28초 간격으로 재생한다. 실제 경기 검수 대기 |

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
| `TreeGolem.png`, `TreeGolem2.png` | 이슈 #482에서 펀치의 발 간격과 지면 접점을 맞췄다. 이슈 #512에서 보행 8장과 펀치 복귀 3장을 프리팹에 연결했다. 실제 경기 검수 대기 |

### 기법이 갈린 항목

| 리소스 | 판단할 것 |
|---|---|
| `LightningCloud.png`, `LightningCloudStrike0.png`~`LightningCloudStrike5.png` | 이슈 #479에서 `RainCloud.png`와 같은 무광 다면체 기법으로 먹구름을 다시 그렸다. 320×640 RGBA와 구름 영역 312×153을 유지했고, 일곱 장의 y=168 아래 픽셀이 기존 PNG와 일치한다. Unity 플레이 검수 대기 |

### 통일 범위에 넣을지 정할 자산 27장

아래는 baseline 그대로지만 게임 플레이 스프라이트가 아니다. 같은 기준을 댈
대상인지 이 문서에서 정한 적이 없어 목록만 남긴다.

- `Art/Images/UI/Card/` 5장 — `magic_build`·`magic_drop`·`magic_explode`·`magic_shoot`·`magic_spawn`. 평면 vector 아이콘으로 자기들끼리는 일관돼 있다. 같은 폴더의 원소 아이콘 `type_*` 6장은 이슈 #304 에서 다시 그려 완료 표로 옮겼다
- `Art/Images/Customize/` 9장 — 모자·망토와 그 아이콘, `check.png`
- `Art/Images/Adventure/` — 숲 모험 공용 지도 `forest_adventure_stage_1.png`에 이슈 #498의 무광 컷페이퍼 아트를 적용했다. 867×256 RGBA, 마을·숲과 길 위치 유지. 직선 길과 남쪽 갈림길의 중앙은 대조한 표본에서 기존과 최대 2px 차이다. 모험 화면 검수 대기. 요새 모험 아이콘 `stone_fortress.png`는 이슈 #500에서 무광 석재와 기존 붉은 깃발을 적용했다. 256×183 RGBA와 기존 참조를 유지했다. 숲 모험 아이콘 `tree_icon.png`는 이슈 #502에서 지도와 같은 녹색 컷페이퍼로 교체했다. 기존 104×128 RGBA와 참조 두 곳을 유지했다. 나머지 지도·배경은 추가 대조 대상이다. `Resources/Game/pve/` 3장 (`pve_nature_slime_nest.png`, `pve_water_slime_nest.png`, `pve_vine_witch.png`)은 이슈 #169에서 교체했다
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

## 2026-11-05 전기 창병 추가

- `ElectricSpearman.png`, `ElectricSpearmanAttacking.png`: 사용자 승인 후 적용. 같은 128×102 RGBA 캔버스, PPU 90, Bottom Center. 런타임 프리팹과 카드 아이콘 연결. 기본·찌르기 자세 완료.
- `ElectricSpearmanWalk01–08.png`, `ElectricSpearmanRecover01–03.png`: 클라이언트 이슈 #644. 같은 128×102 RGBA 캔버스, PPU 90, 하단 중앙 피벗. 양발 보행 8장과 기존 찌르기 뒤 창 회수 3장.
- `RockGolem.prefab`: 기존 그림을 유지하고 본체·그림자를 60%로 축소.

## 성장 UI 골드 아이콘 — 2026-10-29

`Assets/Resources/UI/Progression/Gold.png`: 승인된 흰 패널 시안에 사용하는 단일 골드 아이콘. master-v2 기준으로 생성했으며, 로비·레벨 보상·골드 보상 타일에서 공유한다. 투명 배경, Unity 최대 텍스처 256, 작은 화면에서 실루엣 확인. 클라이언트 #393 / PR #396 검토 대상.

## 세력 선택 배경 — 2026-11-05

`Assets/Art/Images/UI/FactionSelection/Environments.png`: 승인된 세력 대치 시안에서 분리한 기본·불타는 군단·세계수 배경 atlas. 유닛과 글자는 포함하지 않는다. 선택 prefab은 같은 비율로 잘라 겹치고 alpha를 전환한다. 유닛은 기존 원본 스프라이트를 그대로 사용한다. Sprite와 prefab은 `FactionSelectionSetup.Build`로 재생성한다.

## 마법 상자 — 2026-11-05

`Assets/Resources/RewardChest/Chests/default/closed.png`, `animation_closed.png`, `open.png`: 사용자 요청으로 금색 목재 상자를 master-v2의 청색 판넬·크림 석재·청록 결정 마법 상자로 교체했다. 목록과 로비에는 여백을 줄인 closed 아이콘을 사용하고, 개봉 연출에는 같은 공통 크롭·배율·바닥 기준의 animation_closed/open 256×256 RGBA 프레임을 사용한다. 원본과 프롬프트는 `.art/concept/chest-redesign-20261105/`에 보관한다. Unity Editor 프리뷰에서 목록·닫힘·열림 프레임을 검수했고 프레임 연결과 바닥 정렬 테스트를 통과했다. 연속 개봉 동작의 실제 플레이 검증은 남아 있다.

## 타락한 바람 정령 공중 모션

`PveCorruptedWindSpirit.png`, `PveCorruptedWindSpiritHover01–04.png`, `PveCorruptedWindSpiritGlide01–04.png`, `PveCorruptedWindSpiritDescent01–04.png`에 해당한다. 이슈 #538. 206×256 RGBA, PPU 200, 하단 중앙 피벗. 정지 상태의 말린 바람과 활공 자세에 각각 네 장, 하강량에 따라 표시할 네 장을 `PveCorruptedWindSpirit.prefab`에 연결했다. 실제 모험 전투 화면 검수는 남아 있다.

## 타락한 덩굴 정령 지면 모션

`PveCorruptedVineSpirit.png`, `PveCorruptedVineSpiritCrawl01–08.png`, `PveCorruptedVineSpiritLash01–04.png`에 해당한다. 이슈 #540. 180×256 RGBA, PPU 200, 하단 중앙 피벗. 뿌리 이동 여덟 장과 덩굴 타격 및 복귀 네 장을 `PveCorruptedVineSpirit.prefab`에 연결했다. 실제 모험 전투 화면 검수는 남아 있다.

## 구르는 바위 아트와 회전

`RockRolling.png`와 `RockRollingRoll02–08.png`. 이슈 #542. 무광 돌 표면을 여덟 방향에서 그렸다. 256×244 RGBA, PPU 133.333333, 하단 중앙 피벗. `RockRolling.prefab`에서 이동 거리와 진행 방향에 따라 자세를 바꾼다. `RockShot.prefab`의 돌탄도 기본 그림을 공유한다.

## 치킨 특공대 낙하·착지·달리기·타격

이슈 #544. 공중의 파란 낙하산과 지상의 짧은 마법 지팡이 복장을 master-v2의 큰 무광 판면으로 통일했다. `ChickenCommando.prefab`에 낙하 네 자세, 착지 네 자세, 달리기 여덟 자세, 지팡이 타격과 복귀 네 자세를 연결했다. 256×256 RGBA, 하단 중앙 피벗. 공중 PPU 124.271845, 지상 PPU 200. 기존 공중과 지상 기본 Sprite의 GUID를 유지했다.

## 사악한 고목 뻗는 주먹

이슈 #546. `EvilEntPunchArm`에 반복되는 가지 팔과 주먹 준비·조이기·타격·회복 네 자세를 적용했다. 팔 64×96, 주먹 120×80, 모두 RGBA와 왼쪽 중앙 피벗, PPU 100이다. 가지의 가로 반복 경계를 맞췄다. 타격 자세는 서버 피해와 같은 전체 시간의 55%에 시작한다. 주먹 끝이 대상에 닿도록 손 그림의 길이를 팔 길이에 반영했다.

## 사악한 고목 잡기 발톱

이슈 #548. 마른 가지 팔과 발톱을 펼침·접촉·움켜쥠·놓기의 네 자세로 그렸다. 팔 64×96, 발톱 120×80, 왼쪽 중앙 피벗, PPU 100이다. 전체 시간의 22% 시점에 닿고 대상이 끌려오는 동안 발톱이 대상의 움직임을 따라간다.

## 사악한 고목 불주먹

이슈 #550. 탄 가지의 반복 무늬와 주먹 내부의 불빛을 준비·예열·타격·회수 네 자세로 그렸다. 팔 64×96, 불주먹 120×92, 왼쪽 중앙 피벗, PPU 100이다. 타격 자세는 전체 시간의 45%에 시작하고 80% 회수까지 유지한다.

## 사악한 고목 양발 보행과 공격 몸짓

이슈 #552. 일반 고목에 양쪽 뿌리발의 접촉·체중 이동·지나가기·뗌을 여덟 장으로 연결했다. 차원문 보스와 기본 및 상체 공격 세 장을 공유한다. 224×256 RGBA, 하단 중앙 피벗, PPU 100이다. 가지 팔 시작점이 위치하는 바닥에서 1.55칸 위를 평상시·보행·공격 그림에서 몸통에 맞췄다.

## 충격 사슴 네 발 질주와 돌진 충돌 자세

이슈 #554. 앞발이 체중을 받고 뒷발이 지면을 밀어낸 뒤 네 발을 모아 다음 발을 내딛는 여덟 자세와 뿔 충돌 및 체중 회수 세 자세를 추가했다. 256×256 RGBA, 하단 중앙 피벗, PPU 100이다. 기존 기본 그림과 같은 높이와 중심에 맞췄다. `HitEvent`에 맞춰 뿔을 낮추고 복귀한 다음 이동 방향을 따른다.

## 물결 궁수 양발 보행과 발사 복귀 자세

이슈 #556. 활을 든 채 두 발을 번갈아 내딛는 여덟 장과 시위를 놓은 뒤 손을 내려 다음 화살을 거는 두 장을 추가했다. 256×246 RGBA, 하단 중앙 피벗, PPU 200이다. 전투 프리팹은 실제 이동에 맞춰 보행 자세를 재생하고 발사 이벤트에서 기존 발사 그림과 복귀 두 장을 연달아 쓴다. 계정 튜토리얼 전투도 같은 프리팹을 사용한다.

## 방벽 골렘 양발 보행과 주먹 타격 자세

이슈 #558. 석판과 이끼, 붉은 균열을 유지하면서 한 발로 체중을 받는 보행 여덟 자세와 주먹 타격·복귀 네 자세를 제작했다. 새 그림은 256×254 RGBA, 하단 중앙 피벗, PPU 100이다. 방벽 골렘의 전투 프리팹이 보행과 공격을 재생한다. 파괴에서 서버가 생성한 돌 잔해는 본체 페이드와 파편 뒤에도 남는다.

## 포탑등이 양발 보행과 지상 타격·대공 발사 자세

이슈 #560. 등에 포탑을 결속한 꼬마돌에 양발 보행 여덟 장, 주먹 타격과 회수 네 장, 대공 발사와 반동 네 장을 추가했다. 새 그림은 256×256 RGBA, 하단 중앙 피벗, PPU 133.33이다. 대공탄은 첫 발사 자세에서 포구 중심에 맞춰 출발한다.

## 바다뱀 머리 이동과 물줄기 발사 자세

이슈 #562. 하늘색 등지느러미와 옅은 청록색 배를 기준으로 목을 굽혔다 펴는 여덟 자세와 물줄기 발사·회수 네 자세를 추가했다. 220×214 RGBA, 하단 중앙 피벗, PPU 100이다. 몸통 다섯 구간이 머리가 지나간 경로를 따르고, 같은 서버 틱의 공격 위치와 빔 시작점을 대조해 발사원이 한 마리로 확인될 때 입에서 물줄기를 그린다.

## 차원 두꺼비 여섯 발 도주 자세

이슈 #564. 두 조가 번갈아 바닥을 딛는 여덟 자세를 추가했다. 가까운 앞발과 뒷발, 먼 중간발이 첫 조를 이룬다. 다음 네 장에서 반대편 세 발이 체중을 받는다. 통로와 눈 위치를 각 그림에서 맞췄다. 256×162 RGBA, 하단 중앙 피벗, PPU 100이다. 카드의 전투 프리팹은 지면 이동으로 자세를 고르고 고정형 모험 프리팹은 대기 자세를 사용한다.

## 꼬마돌 양발 보행과 지상 타격·복귀 자세

이슈 #566. 붉은 균열과 이끼를 유지한 채 두 발 보행 여덟 장과 짧은 주먹 타격·회수 네 장을 추가했다. 보행은 288×256, 타격은 320×256 RGBA다. 하단 중앙 피벗과 200 PPU에 맞추고, 주먹을 내미는 그림의 가로 여백을 확보했다. 꼬마 돌 무리와 대지 소환은 `MiniRock.prefab`의 같은 자세를 사용한다. 두 개체가 동시에 공격해도 타격 방향과 재생은 개체별로 정한다.

## 폭풍 기수 구름 질주와 창 타격 자세

이슈 #568. 구름에 탄 기사의 질주 여덟 장과 창 타격·회수 다섯 장을 추가했다. 구름의 앞뒤 덩어리를 차례로 부풀리고 눌러 망토와 플룸이 뒤따르게 했다. 새 그림은 288×232 RGBA, 하단 중앙 피벗, PPU 133.333333이다. 지상 피해가 들어오는 프레임에 기수가 몸을 숙여 창끝을 낮게 내민다.


## 불씨 정령 네 발 보행과 앞발 타격

이슈 #570. 대각선 앞발·뒷발이 번갈아 체중을 받는 보행 여덟 장과 앞발 타격·회수 네 장을 추가했다. 어깨와 골반 아래 네 발의 위치를 그려 4→5번과 8→1번에서 지지가 바뀐다. 새 그림은 304×176 RGBA, 하단 중앙 피벗과 PPU 200이다. 다섯 정령은 개체별 피해 이벤트에서 타격을 시작한다.

## 씨앗 정령 밑동 보행과 잎 타격

이슈 #572. 아래쪽 잎이 번갈아 접지하는 보행 여덟 장과 앞 잎 타격·회수 네 장을 추가했다. 새싹과 흰 꽃은 밑동보다 늦게 기울고, 각 정령의 잎 타격은 자기 피해 이벤트에서 시작한다. 새 그림은 304×240 RGBA, 하단 중앙 피벗, 200 PPU다. 씨앗 정령 무리와 씨앗 둥지가 같은 프리팹을 사용한다.

## 찌릿쥐 네 발 질주와 가시 타격

이슈 #574. 짧은 네 발의 대각선 지지가 교대하는 질주 여덟 장과 머리·가시 타격, 몸통 회수 네 장을 추가했다. 먼 쪽 앞발과 뒷발이 몸통 아래에서 각각 보인다. 새 그림은 304×184 RGBA, 하단 중앙 피벗과 200 PPU다. 두 마리는 각자가 피해를 준 틱에 타격 자세를 시작한다.

## 천둥 새 날갯짓과 급강하

이슈 #576. 날갯짓 여덟 장과 급강하·부리 타격과 회수·재상승·사망 낙하를 각각 네 장씩 추가했다. 양 날개가 약간 시차를 두고 움직이며 새 세 마리가 각자의 고도와 피해 이벤트에 따라 자세를 바꾼다. 마법 아이콘의 색과 번개 무늬를 이어받은 전투 스프라이트 24장은 368×280 RGBA, 200 PPU다.

## 바람 정령 공중 대기와 충돌

이슈 #578. 공중 대기, 수평 활공, 하강과 몸을 모으는 자세를 네 장씩 추가했다. 중앙의 얼굴과 몸통을 기준으로 청록색 띠의 두께와 꼬리의 모임이 바뀐다. 12장은 기존과 같은 206×256 RGBA, 하단 중앙 피벗, 200 PPU다. 공중 표적에 닿으면 마지막 자세에서 기존 소멸 효과로 이어진다.

## 화산편 보행과 앞발 타격

이슈 #580. 화산편의 용암 코어와 낮은 돌 외장을 살린 대기 한 장, 네 발 보행 여덟 장, 앞발 타격과 회수 네 장을 추가했다. 프레임은 288×152 RGBA, 200 PPU, 하단 중앙 피벗이다. 지면에 닿은 뒤 멀고 가까운 발을 번갈아 디디고 각 개체의 피해 틱에 앞발을 내민다.

## 폭풍편 보행과 전격 타격

이슈 #582. 폭풍편의 어두운 남색 몸체와 세로로 긴 청색 코어를 살린 대기 한 장, 가까운 발과 먼 발의 대각선 지지 보행 여덟 장, 전격 타격과 회수 네 장을 추가했다. 프레임은 224×280 RGBA, 200 PPU, 하단 중앙 피벗이다. 착지 뒤 걷고 개체별 피해 틱에서 코어와 앞발의 타격 자세를 표시한다.

## 전기 슬라임 지면 이동과 접촉 타격

이슈 #584. 금빛 젤과 번개 모양 돌기를 살린 대기 한 장, 지면에 붙어 눌리고 펴지는 이동 여덟 장, 타격·회수 네 장을 추가했다. 13장은 원본과 같은 128×111 RGBA, 100 PPU, 하단 중앙 피벗이다. 소환된 슬라임마다 자신의 이동과 피해 틱에 맞춰 자세를 표시한다.

## 화염 슬라임 지면 이동과 접촉 타격

이슈 #586. 어두운 재 몸체와 불빛이 비치는 균열을 살린 대기 한 장, 접지 이동 여덟 장, 접촉 타격과 회수 네 장을 추가했다. 13장은 128×108 RGBA, 100 PPU, 하단 중앙 피벗을 공유한다. 몸통이 눌릴 때 균열이 낮아지고 뒤쪽이 따라온다. 불 장판은 지나온 자리에 남는다.

## 나뭇잎 슬라임 지면 이동과 접촉 타격

- 나뭇잎 슬라임의 녹색 몸체와 두 잎을 유지한 대기 1장, 지면 이동 8장, 접촉 타격과 복귀 4장을 적용했다. 미리보기에서 피해 시점에 타격 자세를 표시하고 잎 장판은 각각 생긴 위치에서 재생된다.

## 바위 슬라임 지면 이동과 접촉 타격

- 갈색 바위 몸과 꼭대기 돌의 대기 1장, 지면 이동 8장, 타격과 복귀 4장을 적용했다. 미리보기에서 바위 슬라임마다 상대에게 피해를 준 프레임에 타격 자세를 재생한다.

## 바람 슬라임 지면 이동과 접촉 타격

- 바람 슬라임의 낮은 청록색 몸과 작은 바람 끝을 유지했다. 대기 1장, 지면 이동 8장, 타격과 복귀 4장을 적용했다. 이동 중 몸통의 두께 변화와 피해 틱에 따른 개체별 타격을 확인한다.

## 물 슬라임 지면 이동과 물 발사

- `WaterSlime` 대기에 이동 8장과 물 뱉기·복귀 4장을 연결했다. `WaterShot`은 새 발사 자세의 입에서 나간다. `WaterSlimeSwarm` 카드와 물 장판 그림은 기존 에셋을 쓴다.

## 공용 물 투사체 비행 자세

- 밝은 물방울의 코끝에 맞춰 비행 4장을 등록했다. 참조형 투사체는 0.125초·0.5초의 수명에 맞춰 네 장을 보여주고 직접 시전은 충돌 전까지 반복한다.

## 바람 칼날 관통 비행

- 바람 줄기의 낮은 꼬리와 코끝을 원래 방향에 맞춘 비행 8장을 제작했다. 둥근 두께가 뒤에서 앞으로 이어지고 기존의 관통 이동과 종료 동안 반복된다.

## 화염탄 비행 자세

- 열이 갑각 사이를 따라 앞쪽으로 이어지는 비행 4장을 추가했다. 공용 발사와 직접 시전, 튜토리얼이 같은 네 장을 사용한다. 카드와 공유 기본 프리팹의 그림은 기존 파일을 쓴다.

## 공용 전기 투사체 비행 자세

- 금색 번개에 네 비행 자세를 추가했다. 전기 탑·천둥 정령의 발사와 흡수 투사체는 기록된 비행 수명에 맞춰 재생하고, 번개탄 직접 시전은 비행 중 반복한다.

## 폭탄 정령의 투하 폭탄 하강 자세

- 무광 검은 껍질과 우상향 도화선을 정렬하고 금색 불꽃의 하강 자세 여섯 장을 추가했다. 각 폭탄이 자기 생성 시점부터 재생한다.

## 바위 강타 비행 자세

- 따뜻한 돌 면과 녹색 이끼, 청록색 바람 자국을 여섯 비행 자세로 그렸다. 바람의 두께가 뒤에서 바위까지 순차적으로 변하며 투사체는 녹화의 명중과 밀침을 따른다.

## 해일 탄두 비행 자세

- 꼬리와 지느러미가 차례로 굽는 여섯 장을 그렸다. 지상형과 공중형은 같은 여섯 자세와 알파를 공유하며 눈 색만 다르다.

## 드래곤 타워 불길 비행 자세

- 기존 화염탄을 상속하던 드래곤 타워 불길에 전용 투사체 여섯 장을 적용했다. 둥근 주황 불길에서 밝은 열이 뒤에서 앞으로 번진다.

## 도깨비불 이동 자세

- 얼굴 없는 연두색 불꽃에 이동 자세 네 장을 적용했다. 네 장의 둥근 바닥 위치를 맞추고 불꽃 끝과 안쪽 밝은 면을 바꿨다.
- `WillOWispFlight01.png` · `WillOWispFlight02.png` · `WillOWispFlight03.png` · `WillOWispFlight04.png`

## 파도 소환 이동 자세

- 파란 물층이 뒤에서 올라와 오른쪽 물마루를 따라 말리는 여섯 자세를 적용했다. 긴 밑단의 높이와 중심을 맞췄다.
- `TideCallFlight01.png` · `TideCallFlight02.png` · `TideCallFlight03.png` · `TideCallFlight04.png` · `TideCallFlight05.png` · `TideCallFlight06.png`

## 토네이도 회전 자세

- 청록색 띠의 밝은 면이 둘레를 돌아가는 여덟 자세를 적용했다. 아래쪽 뾰족한 끝의 위치와 위쪽의 트인 고리를 맞췄다.
- `TornadoStrikeSpin01.png` · `TornadoStrikeSpin02.png` · `TornadoStrikeSpin03.png` · `TornadoStrikeSpin04.png` · `TornadoStrikeSpin05.png` · `TornadoStrikeSpin06.png` · `TornadoStrikeSpin07.png` · `TornadoStrikeSpin08.png`

## 대지의 부름 발동 자세

- 파도 그림을 표시하던 전투 프리팹에 황토색 진동 고리와 돌 조각 발동 자세를 적용했다. 카드 그림의 재질을 살리고 유닛이 나타날 중앙에 여백을 뒀다.
- `EarthCallAwaken01.png` · `EarthCallAwaken02.png` · `EarthCallAwaken03.png` · `EarthCallAwaken04.png` · `EarthCallAwaken05.png` · `EarthCallAwaken06.png` · `EarthCallAwaken07.png` · `EarthCallAwaken08.png`

## 연쇄 번개 비행 자세

- 금색 번개의 굴절과 매듭을 맞추고 밝은 면이 앞으로 옮겨가는 여섯 자세를 적용했다.
- `ChainLightningFlight01.png` · `ChainLightningFlight02.png` · `ChainLightningFlight03.png` · `ChainLightningFlight04.png` · `ChainLightningFlight05.png` · `ChainLightningFlight06.png`

## 분화구 운반수 보행

- 기존 분화구와 몸통의 실루엣을 유지하고, 발을 번갈아 딛는 `CraterCarrierWalk01–08.png`을 적용했다.
- 접근 중에는 지면 이동 거리에 맞춰 보행한다. 설치, 발사, 회수 중에는 기존 단계별 자세를 표시한다.

## 분화구 포탄의 비행 자세

- 용암 바위의 실루엣과 앞쪽 균열에 맞춘 여섯 자세를 적용했다. `CraterShellFlight01–06.png`은 전투 `CraterShell`에서 재생한다.

## 콩탄의 비행 자세

- 무광 갈색 씨앗의 축과 길이를 맞추고, 비행 중 드러나는 면을 `BeanFlight01–06.png`에 그렸다. 전투 `Bean`의 기존 렌더러에서 재생한다.

## 차원문 모험 화산편

- 모험 `PveFireTadpole`에 카드 화산편의 네 발 보행 8장과 앞발 타격·복귀 4장을 연결했다. 서버 이동과 개체별 피해에 반응한다.

## 차원문 모험 폭풍편

- 모험 `PveLightningTadpole`에 카드 폭풍편의 네 발 보행 8장과 앞발 전격·복귀 4장을 연결했다. 서버 이동과 개체별 피해에 반응한다.

## 덩굴 투척의 성장 자세

- 녹색 다면체 줄기의 뿌리를 고정하고 새순에서 줄기 마디가 연달아 뻗어 오르는 `VineGrowth01–06.png`을 적용했다. 직접 시전과 숲의 마녀가 생성하는 전투 `Vine`에서 재생한다.


## 덩굴 세계의 거대 덩굴 성장 자세

- 넓은 뿌리를 제자리에 두고 녹색 다면체 줄기의 마디와 두 개의 덩굴손이 솟는 `GiantVineGrowth01–06.png`을 전투 `GiantVine`에 적용했다.

## 덩굴 군락의 둥지 성장 자세

- 직접 설치와 모험의 덩굴 군락에 `VineColonyGrowth01–06.png`을 적용했다. 낮은 새순에서 여러 줄기가 솟아 서로 얽히면 둥지가 완성된다.

## 폭탄 정령 본체의 비행·투하

- 폭탄 정령 몸체의 공중 자세 여섯 장과 서버 Attack에 맞춘 투하 자세 네 장을 적용했다.

## 바람 폭발과 폭탄 정령 착탄의 확산

- 갑자기 열리고 끝부터 흩어지는 바람 결 여섯 장을 직접 시전의 밀치기와 폭탄의 착탄에 연결했다.

## 감전 덫 본체의 결정 예열

이슈 #682. 돌 받침이 고정된 `ShockTrapArming01–06.png`을 전투 덫 본체에 적용했다. 대기는 01을 유지하며 서버의 예열 상태가 도착하면 한 번 재생한다. 254×164 RGBA, PPU 133.333333, 하단 중앙 피벗이다.

## 회복 토템의 잎과 회복광

이슈 #684. 서버의 회복 신호에 뿌리가 고정된 `HealingTotemPulse02–06.png`을 한 번 재생한 뒤 대기 자세 01로 돌아온다. 188×256 RGBA, PPU 133.333333, 하단 중앙 피벗이다.

## 생명의 나무 성장과 회복광

이슈 #686. 같은 뿌리에서 새순이 올라와 가지와 잎을 펼치는 성장 여섯 자세를 적용했다. 다 자란 나무는 서버의 회복 신호를 받아 마나 옹이를 밝히는 자세 네 장을 재생한다. 256×207 RGBA, PPU 133.333333, 하단 중앙 피벗이다.

## 씨앗 둥지의 성장 자세

이슈 #688. 낮은 새순의 줄기가 위로 뻗고 잎이 벌어져 빈 그릇 테두리를 이룬다. 다섯 성장 자세 뒤 기존 SeedNest.png를 표시한다. 256×179 RGBA, PPU 100, 하단 중앙 피벗이다.

## 마나 우물의 수면 흐름

이슈 #690. 충전 중인 마나 우물의 테두리 안에서 물결이 시계 방향으로 돈다. 수면 여섯 자세는 0.14초마다 반복하며, 원본의 알파와 테두리 픽셀을 사용한다. 256×179 RGBA, PPU 133.333333, 하단 중앙 피벗이다.

## 광란의 토템 낙하 자세

이슈 #692. 곧게 떨어지는 몸체의 날개가 위로 젖혀지고 열이 아래 끝으로 모인다. 서버 높이로 여섯 자세를 고른다. 163×256 RGBA, PPU 133.333333, 하단 중앙 피벗이다.

## 거품 생성기의 보호막 자세

이슈 #696. 아군에게 보호막을 보내는 순간 고리 안 거품이 부풀고, 납작해졌다가 대기 자세로 돌아온다. 175×256 RGBA, PPU 133.333333, 하단 중앙 피벗이다.

## 전기 탑의 연쇄 방전 자세

이슈 #698. 석재 받침 위의 금색 구체와 날개가 서버 Attack에 맞춰 네 자세를 재생한다. 최초 방전이 보이도록 설치 순간부터 몸체를 곧게 세운다. 200×256 RGBA, PPU 133.333333, 하단 중앙 피벗이다.
