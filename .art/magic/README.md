# 마법 컨셉 페이지

2026-10-17 신규 소환 마법: [비전 보병](pages/arcane_infantry.md),
[분화구 운반수](pages/crater_carrier.md). 두 페이지는 아래 기존 생성 목록에 추가한 구현 계약이다.

game 서버의 concrete `*Magic.java`를 기준으로 생성한 아트·동작 문서다.

- 생성 페이지: 83개
- 생성기: `../tools/generate-magic-pages.py`
- 서버 수치: 미포함. 런타임 DB 파라미터 키만 기록
- 재생성: `python3 .art/tools/generate-magic-pages.py`

## 진영별 수

- 귀속 미정: 9개
- 돌 골렘 부족: 7개
- 물 슬라임: 8개
- 세계수 바람 정령: 12개
- 세계수 전기 정령: 11개
- 세계수 풀 정령: 16개
- 인간 마법 문명: 5개
- 지옥불 군단: 13개
- 차원 유랑종: 1개
- 타락한 정령: 1개

## 설치 (26)

| 이름 | 서버 키 | 진영 | 주 프리팹 |
|---|---|---|---|
| [거품 생성기](pages/bubble_generator.md) | `bubble_generator` | 물 슬라임 | `BubbleGenerator` |
| [대포](pages/cannon.md) | `cannon` | 인간 마법 문명 | `GroundCannon` |
| [분화구](pages/crater.md) | `crater` | 지옥불 군단 | `Crater` |
| [드래곤 타워](pages/dragon_tower.md) | `dragon_tower` | 인간 마법 문명 | `DragonTower` |
| [전기 타워](pages/electric_tower.md) | `electric_tower` | 세계수 전기 정령 | `ElectricTower` |
| [화염 슬라임 둥지](pages/fire_slime_nest.md) | `fire_slime_nest` | 지옥불 군단 | `FireSummon` |
| [폭죽 타워](pages/firework_tower.md) | `firework_tower` | 지옥불 군단 | `FireworkTower` |
| [풀 생성기](pages/grass_generator.md) | `grass_generator` | 귀속 미정 | `GrassGenerator` |
| [회복 토템](pages/healing_totem.md) | `healing_totem` | 세계수 풀 정령 | `HealingTotem` |
| [생명의 나무](pages/life_tree.md) | `life_tree` | 세계수 풀 정령 | `LifeTree` |
| [번개 슬라임 둥지](pages/lightning_slime_nest.md) | `lightning_slime_nest` | 세계수 전기 정령 | `ElectricSummon` |
| [마나 샘](pages/mana_well.md) | `mana_well` | 세계수 풀 정령 | `ManaWell` |
| [자연 슬라임 둥지](pages/nature_slime_nest.md) | `nature_slime_nest` | 세계수 풀 정령 | `PveNatureSlimeNest` |
| [PVE 물 슬라임 둥지](pages/pve_water_slime_nest.md) | `pve_water_slime_nest` | 물 슬라임 | `PveWaterSlimeNest` |
| [집결의 토템](pages/rallying_totem.md) | `rallying_totem` | 지옥불 군단 | `RallyingTotem` |
| [수리 토템](pages/repair_totem.md) | `repair_totem` | 귀속 미정 | `RepairTotem` |
| [바위 슬라임 둥지](pages/rock_slime_nest.md) | `rock_slime_nest` | 돌 골렘 부족 | `RockSummon` |
| [돌 포탑](pages/rock_turret.md) | `rock_turret` | 돌 골렘 부족 | `RockTurret` |
| [씨앗 둥지](pages/seed_nest.md) | `seed_nest` | 세계수 풀 정령 | `SeedNest` |
| [감전 함정](pages/shock_trap.md) | `shock_trap` | 세계수 전기 정령 | `ShockTrap` |
| [거신의 잔해](pages/titan_remnant.md) | `titan_remnant` | 귀속 미정 | `TitanRemnant` |
| [타워](pages/tower.md) | `tower` | 인간 마법 문명 | `GroundTower` |
| [포탑등이](pages/towerback.md) | `towerback` | 인간 마법 문명 | `Towerback` |
| [덩굴 군집](pages/vine_colony.md) | `vine_colony` | 세계수 풀 정령 | `VineColony` |
| [바람 슬라임 둥지](pages/wind_slime_nest.md) | `wind_slime_nest` | 세계수 바람 정령 | `WindSummon` |
| [바람 토템](pages/wind_totem.md) | `wind_totem` | 세계수 바람 정령 | `WindTotem` |

## 투하 (9)

| 이름 | 서버 키 | 진영 | 주 프리팹 |
|---|---|---|---|
| [치킨 특공대](pages/chicken_commando.md) | `chicken_commando` | 인간 마법 문명 | `ChickenCommando` |
| [화염 투하](pages/fire_drop.md) | `fire_drop` | 지옥불 군단 | `FireDrop` |
| [광란의 토템](pages/frenzy_totem.md) | `frenzy_totem` | 지옥불 군단 | `FrenzyTotem` |
| [술이파리](pages/leafair.md) | `leafair` | 세계수 풀 정령 | `Leafair` |
| [번개 투하](pages/lightning_drop.md) | `lightning_drop` | 세계수 전기 정령 | `LightningCloud` |
| [유성우](pages/meteor_shower.md) | `meteor_shower` | 지옥불 군단 | `MeteorShower` |
| [자연 투하](pages/nature_drop.md) | `nature_drop` | 세계수 풀 정령 | `NatureDrop` |
| [바위 투하](pages/rock_drop.md) | `rock_drop` | 돌 골렘 부족 | `RockDrop` |
| [바람 투하](pages/wind_drop.md) | `wind_drop` | 세계수 바람 정령 | `WindDrop` |

## 범위 폭발 (9)

| 이름 | 서버 키 | 진영 | 주 프리팹 |
|---|---|---|---|
| [번개 폭발](pages/lightning_explosion.md) | `lightning_explosion` | 세계수 전기 정령 | `ElectricExplode` |
| [마그마 폭발](pages/magma_explosion.md) | `magma_explosion` | 지옥불 군단 | `MagmaExplosion` |
| [과생장](pages/overgrowth.md) | `overgrowth` | 세계수 풀 정령 | `Overgrowth` |
| [칼바람 회오리](pages/razor_gale.md) | `razor_gale` | 세계수 바람 정령 | `RazorGale` |
| [모래 폭풍](pages/sand_storm.md) | `sand_storm` | 세계수 바람 정령 | `SandStorm` |
| [전격 과부하](pages/shock_overload.md) | `shock_overload` | 세계수 전기 정령 | `ShockOverload` |
| [덩굴 세계](pages/vine_world.md) | `vine_world` | 세계수 풀 정령 | `GiantVine` |
| [간헐천 폭발](pages/water_explosion.md) | `water_explosion` | 물 슬라임 | `WaterExplosion` |
| [돌풍](pages/wind_explosion.md) | `wind_explosion` | 세계수 바람 정령 | `WindExplode` |

## 투사체 (13)

| 이름 | 서버 키 | 진영 | 주 프리팹 |
|---|---|---|---|
| [바위 강타](pages/boulder_strike.md) | `boulder_strike` | 귀속 미정 | `BoulderStrike` |
| [연쇄 번개](pages/chain_lightning.md) | `chain_lightning` | 세계수 전기 정령 | `ChainLightning` |
| [화염탄](pages/fire_shot.md) | `fire_shot` | 지옥불 군단 | `FireShot` |
| [전격탄](pages/lightning_shot.md) | `lightning_shot` | 세계수 전기 정령 | `ElectricShot` |
| [돌 굴리기](pages/rock_rolling.md) | `rock_rolling` | 돌 골렘 부족 | `RockRolling` |
| [정령 폭탄](pages/spirit_bomb.md) | `spirit_bomb` | 귀속 미정 | `Player` |
| [해일 탄두](pages/tidal_warhead.md) | `tidal_warhead` | 귀속 미정 | `TidalWarhead` |
| [파도 소환](pages/tide_call.md) | `tide_call` | 물 슬라임 | `TideCall` |
| [덩굴 부채](pages/vine_fan.md) | `vine_fan` | 세계수 풀 정령 | `-` |
| [덩굴 투척](pages/vine_toss.md) | `vine_toss` | 세계수 풀 정령 | `Vine` |
| [물 탄환](pages/water_shot.md) | `water_shot` | 물 슬라임 | `WaterShot` |
| [도깨비불](pages/will_o_wisp.md) | `will_o_wisp` | 세계수 풀 정령 | `WillOWisp` |
| [바람 칼날](pages/wind_blade.md) | `wind_blade` | 세계수 바람 정령 | `WindBlade` |

## 소환 (26)

| 이름 | 서버 키 | 진영 | 주 프리팹 |
|---|---|---|---|
| [물의 궁수](pages/aqua_archer.md) | `aqua_archer` | 물 슬라임 | `AquaArcher` |
| [폭탄 정령](pages/bomb_sprite.md) | `bomb_sprite` | 귀속 미정 | `BombSprite` |
| [거품 정령](pages/bubble_spirit.md) | `bubble_spirit` | 물 슬라임 | `BubbleSpirit` |
| [운룡](pages/cloud_dragon.md) | `cloud_dragon` | 세계수 바람 정령 | `CloudDragon` |
| [차원 두꺼비](pages/dimension_toad.md) | `dimension_toad` | 차원 유랑종 | `DimensionToad` |
| [불씨 정령 무리](pages/ember_spirit_swarm.md) | `ember_spirit_swarm` | 지옥불 군단 | `EmberSpirit` |
| [사악한 나무 골렘](pages/evil_ent.md) | `evil_ent` | 타락한 정령 | `EvilEnt` |
| [불 대왕 정령](pages/fire_lord_spirit.md) | `fire_lord_spirit` | 지옥불 군단 | `FireLordSpirit` |
| [불의 정령](pages/fire_spirit.md) | `fire_spirit` | 지옥불 군단 | `FireSpirit` |
| [용암 정령](pages/magma_spirit.md) | `magma_spirit` | 지옥불 군단 | `MagmaSpirit` |
| [꼬마 돌 무리](pages/mini_rock_swarm.md) | `mini_rock_swarm` | 돌 골렘 부족 | `MiniRock` |
| [바위 골렘](pages/rock_golem.md) | `rock_golem` | 돌 골렘 부족 | `RockGolem` |
| [돌 마법사](pages/rock_mage.md) | `rock_mage` | 돌 골렘 부족 | `RockMage` |
| [바다뱀](pages/sea_serpent.md) | `sea_serpent` | 귀속 미정 | `SeaSerpent` |
| [씨앗 정령 무리](pages/seed_spirit_swarm.md) | `seed_spirit_swarm` | 세계수 풀 정령 | `SeedSpirit` |
| [폭풍 기수](pages/storm_rider.md) | `storm_rider` | 세계수 바람 정령 | `StormRider` |
| [폭풍 사슴](pages/storm_stag.md) | `storm_stag` | 세계수 바람 정령 | `StormStag` |
| [천둥 새 무리](pages/thunder_bird_swarm.md) | `thunder_bird_swarm` | 세계수 전기 정령 | `ThunderBird` |
| [번개 정령](pages/thunder_spirit.md) | `thunder_spirit` | 세계수 전기 정령 | `ThunderSpirit` |
| [토네이도](pages/tornado_strike.md) | `tornado_strike` | 세계수 바람 정령 | `TornadoStrike` |
| [나무 골렘](pages/tree_golem.md) | `tree_golem` | 세계수 풀 정령 | `TreeGolem` |
| [덩굴 정령](pages/vine_spirit.md) | `vine_spirit` | 세계수 풀 정령 | `VineSpirit` |
| [방벽 골렘](pages/wall_golem.md) | `wall_golem` | 귀속 미정 | `WallGolem` |
| [물 슬라임 무리](pages/water_slime_swarm.md) | `water_slime_swarm` | 물 슬라임 | `WaterSlime` |
| [바람 정령](pages/wind_spirit.md) | `wind_spirit` | 세계수 바람 정령 | `WindSpirit` |
| [찌릿쥐](pages/zap_mouse.md) | `zap_mouse` | 세계수 전기 정령 | `ZapMouse` |
