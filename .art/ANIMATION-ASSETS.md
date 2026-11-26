# 애니메이션 프레임과 오라 에셋

Unity 런타임에서 하나의 소환수 외형을 구성하는 기본 프레임, 공격 프레임, 오라를
구분하는 정본 문서다. 숫자 접미사는 별도 캐릭터가 아니라 상태 프레임일 수 있다.

## 의미 이름

| 실제 파일 | 의미 이름 | 런타임 연결 |
|---|---|---|
| `ElectricSpearman.png`, `ElectricSpearmanWalk01.png`–`ElectricSpearmanWalk08.png` | 전기 창병 · 대기와 양발 보행 | 지면 이동 1.12칸마다 여덟 장을 재생한다. 기준 속도 1.82칸/초에서 한 주기는 약 0.62초 |
| `ElectricSpearmanAttacking.png`, `ElectricSpearmanRecover01.png`–`ElectricSpearmanRecover03.png` | 전기 창병 · 찌르기와 창 회수 | 같은 서버 틱의 `HitEvent`에서 표적을 보고 `Attack`에서 첫 장을 표시한다. 네 장을 0.06초씩 재생한다 |
| `ArcaneInfantry.png` | 비전 보병 · 기본 준비 자세 | `InfantryPoseController` 정지 상태 |
| `ArcaneInfantryAttacking.png` | 비전 보병 · 마력 칼날 타격 | 공격 이벤트에서 0.16초 우선 표시 |
| `ArcaneInfantryWalkLeft.png`, `ArcaneInfantryWalkRight.png` | 비전 보병 · 두 발걸음 | 이동 이벤트가 이어질 때 0.16초마다 교대, 지지하는 발 기준 |
| `PveCorruptedWindSpiritHover01.png`–`PveCorruptedWindSpiritHover04.png`, `PveCorruptedWindSpiritGlide01.png`–`PveCorruptedWindSpiritGlide04.png` | 타락한 바람 정령 · 떠 있기와 활공 | 서버 위치로 수평 이동을 판정한다. 정지 1.20초와 활공 1.04초 주기 |
| `PveCorruptedWindSpiritDescent01.png`–`PveCorruptedWindSpiritDescent04.png` | 타락한 바람 정령 · 몸을 숙이며 하강 | 연속 위치 갱신에서 고도가 내려갈 때 각 하강 높이에 맞는 자세를 고른다. 제거가 시작되면 마지막 자세를 유지한다 |
| `VineSpiritWalk01.png`–`VineSpiritWalk08.png` | 덩굴 정령 · 밑동을 번갈아 딛는 이동 | 이동 0.6칸마다 여덟 자세를 반복한다. 0.5칸/초에서 1.2초 |
| `VineSpiritRelease01.png`–`VineSpiritRelease04.png` | 덩굴 정령 · 씨앗 발사와 복귀 | 각 정령이 공격 이벤트에서 첫 자세를 표시하고 0.08초 간격으로 전환한다. 총 0.32초 |
| `PveCorruptedVineSpiritCrawl01.png`–`PveCorruptedVineSpiritCrawl08.png` | 타락한 덩굴 정령 · 뿌리를 풀고 감아 지면을 이동 | 이동 0.6칸마다 여덟 자세 한 바퀴. 기준 속도 0.5칸/초에서 1.2초 |
| `PveCorruptedVineSpiritLash01.png`–`PveCorruptedVineSpiritLash04.png` | 타락한 덩굴 정령 · 덩굴 타격과 복귀 | 공격 이벤트에서 첫 자세를 즉시 표시하고 0.06초 간격으로 복귀한다. 총 0.24초 |
| `RockRolling.png`, `RockRollingRoll02.png`–`RockRollingRoll08.png` | 구르는 바위 · 기본과 회전 여덟 자세 | 화면 평면의 이동 거리를 바위 둘레로 나누어 자세를 고른다. 반사로 진행 방향이 바뀌면 같은 자세부터 역순으로 재생한다. `RockShot`은 기본 자세를 공유한다 |
| `ChickenCommando.png`, `ChickenCommandoDescent02.png`–`ChickenCommandoDescent04.png`, `ChickenCommandoLand01.png`–`ChickenCommandoLand04.png` | 치킨 특공대 · 낙하산 하강과 착지 | 보간된 고도가 내려가면 낙하산을 놓는 자세까지 고른다. 최초 지면 접촉 뒤 착지 네 장을 0.22초간 표시한다 |
| `chicken_commando_default.png`, `ChickenCommandoRun01.png`–`ChickenCommandoRun08.png`, `ChickenCommandoAttack01.png`–`ChickenCommandoAttack04.png` | 치킨 특공대 · 대기, 양발 달리기, 지팡이 타격과 복귀 | 실제 지면 이동 1.28칸마다 달리기 한 바퀴. 공격 이벤트에서 타격과 복귀를 0.2초간 표시한다 |
| `MagmaSpirit.png` | 용암 갑각 악마 · 선 기본 자세 | `MagmaSpirit.prefab` 기본 SpriteRenderer |
| `MagmaSpiritSpawn01–04.png` | 용암 갑각 악마 · 지면을 뚫고 일어나는 소환 4장 | `GroundedUnitPoseController.spawnFrames`에서 0.28초 동안 재생 |
| `MagmaSpiritWalk01–08.png` | 용암 갑각 악마 · 양발을 번갈아 딛는 걷기 8장 | `GroundedUnitPoseController.walkFrames`에서 지면 이동 0.64칸마다 반복 |
| `MagmaSpiritCast01–04.png` | 용암 갑각 악마 · 주먹으로 지면을 누르고 몸을 펴는 발동 4장 | `summonAttack`을 받은 소환자에서 0.20초 동안 재생 |
| `FireSpiritWalk01–08.png` | 불의 정령 · 앞발과 뒷발을 교대로 딛는 걷기 8장 | `GroundedUnitPoseController.walkFrames`에서 지면 이동 0.68칸마다 반복 |
| `FireSpiritSpray01–04.png` | 불의 정령 · 턱을 열어 분사하고 돌아오는 자세 4장 | `SprayedFlame.sourceId`의 시전자에서 분사 방향을 고정하고 0.50초 동안 재생 |
| `RockMageWalk01–08.png` | 돌 마법사 · 두 발을 번갈아 디디는 걷기 8장 | `GroundedUnitPoseController.walkFrames`에서 지면 이동 0.96칸마다 반복 |
| `RockMageCast01–04.png` | 돌 마법사 · 지팡이 발사와 복귀 4장 | 같은 전투 패킷의 `RockShot` 2발이 첫 자세의 청록 결정에서 나오며 본체 자세는 0.50초 동안 한 번 재생 |
| `MagmaExplosion.png` | 마그마 폭발 · 기본/초기 갑각 자세 | `MagmaExplosion.prefab` 기본 SpriteRenderer |
| `MagmaExplosionStrike1.png`–`MagmaExplosionStrike4.png` | 마그마 폭발 · 균열 시작 → 개방 → 피크 → 냉각 | `SpriteFrameAnimator`, 0.1초 간격, 비반복 |
| `RockGolem.png` | 이끼바위 골렘 · 기본 자세 | `GroundedUnitPoseController` 대기 자세 |
| `RockGolemWalk01.png`–`RockGolemWalk08.png`, `RockGolemWalk04To05.png` | 이끼바위 골렘 · 양발 보행과 내딛는 중간 자세 | 실제 XZ 이동 0.66칸마다 한 바퀴. 기준 속도 0.5칸/초에서 1.32초 |
| `RockGolem2.png`, `RockGolemRecover01.png`–`RockGolemRecover03.png` | 이끼바위 골렘 · 펀치 타격과 복귀 | 기존 타격을 공격 이벤트에서 즉시 표시한다. 0.05초 간격 네 자세 |
| `WallGolem.png`, `WallGolemWalk01.png`–`WallGolemWalk08.png` | 방벽 골렘 · 양발 보행과 체중 이동 | 실제 지면 이동 0.66칸마다 여덟 자세를 재생한다. 기본 속도 0.3칸/초에서 2.2초 |
| `WallGolemPunch01.png`–`WallGolemPunch04.png` | 방벽 골렘 · 주먹 타격과 회수 | `Attack` 상태를 받는 순간 주먹이 닿는 자세를 표시한다. 네 자세가 0.08초씩 이어진다 |
| `TowerbackWalk01.png`–`TowerbackWalk08.png` | 포탑등이 · 포탑의 무게를 받는 양발 보행 | 지면 이동 0.48칸마다 여덟 자세를 재생한다. 기본 속도 0.325칸/초에서 약 1.48초 |
| `TowerbackPunch01.png`–`TowerbackPunch04.png`, `TowerbackFire01.png`–`TowerbackFire04.png` | 포탑등이 · 지상 주먹 타격과 대공 포신 발사·반동 | 지상은 `Attack`에서 주먹 타격을 표시하고, 대공은 `RockShot` 발사 시 포구에서 탄을 내보낸다. 각 동작은 네 자세를 0.08초씩 재생한다 |
| `SeaSerpentHead.png`, `SeaSerpentGlide01.png`–`SeaSerpentGlide08.png` | 바다뱀 · 대기와 목을 굽혔다 펴는 이동 | 실제 지면 이동 0.88칸마다 여덟 자세를 재생한다. 기본 속도 0.55칸/초에서 1.6초 |
| `SeaSerpentFire01.png`–`SeaSerpentFire04.png` | 바다뱀 · 물줄기 발사와 턱 회수 | 네 자세가 물줄기 지속 시간 0.35초 동안 이어진다. 같은 서버 틱의 발사원이 확인되면 머리의 입 위치에서 물줄기를 그린다 |
| `DimensionToad.png`, `DimensionToadRetreat01.png`–`DimensionToadRetreat08.png` | 차원 두꺼비 · 대기와 여섯 발 도주 | 실제 지면 이동 1.28칸마다 여덟 자세를 재생한다. 가까운 앞발·먼 중간발·가까운 뒷발이 체중을 받다가 반대편 세 발에 넘긴다 |
| `MiniRockSwarm.png`, `MiniRockWalk01.png`–`MiniRockWalk08.png` | 꼬마돌 · 대기와 양발 보행 | 실제 지면 이동 0.48칸마다 여덟 자세를 재생한다. 기본 속도 0.5칸/초에서 0.96초 |
| `MiniRockPunch01.png`–`MiniRockPunch04.png` | 꼬마돌 · 짧은 주먹 타격과 회수 | `HitEvent`에서 표적 쪽으로 돌고 첫 자세를 표시한다. 네 자세가 0.08초씩 이어진다 |
| `StormRider.png`, `StormRiderGlide01.png`–`StormRiderGlide08.png` | 폭풍 기수 · 대기와 구름 질주 | 실제 지면 이동 1.92칸마다 구름의 앞뒤가 부풀고 눌리는 여덟 자세를 재생한다. 기본 속도 3칸/초에서 한 주기는 0.64초다 |
| `StormRiderThrust01.png`–`StormRiderThrust05.png` | 폭풍 기수 · 창 타격과 회수 | `HitEvent`에서 표적 방향으로 창을 내민다. 창을 거두고 자세를 정돈하는 다섯 장을 0.08초씩 재생한다 |
| `EmberSpiritSwarm.png`, `EmberSpiritTrot01.png`–`EmberSpiritTrot08.png` | 불씨 정령 · 대기와 네 발 보행 | 대각선으로 마주한 앞발과 뒷발이 체중을 번갈아 받는다. 이동 0.72칸마다 여덟 자세를 재생한다 |
| `EmberSpiritSwipe01.png`–`EmberSpiritSwipe04.png` | 불씨 정령 · 앞발 타격과 회수 | 개체별 `HitEvent`에서 표적 쪽으로 앞발을 내민다. 네 자세가 0.08초씩 이어진다 |
| `SeedSpiritSwarm.png`, `SeedSpiritStep01.png`–`SeedSpiritStep08.png` | 씨앗 정령 · 대기와 밑동 보행 | 밑동 잎이 지면을 번갈아 밀고 위쪽 새싹과 흰 꽃이 뒤따른다. 이동 0.72칸마다 여덟 자세를 재생한다 |
| `SeedSpiritLeafHit01.png`–`SeedSpiritLeafHit04.png` | 씨앗 정령 · 앞 잎 타격과 회수 | 개체별 `HitEvent`에서 몸을 낮추고 앞 잎을 뻗는다. 네 자세를 각각 0.08초간 표시한다 |
| `ZapMouse.png`, `ZapMouseRun01.png`–`ZapMouseRun08.png` | 찌릿쥐 · 대기와 네 발 질주 | 지면 이동 1.2칸마다 대각선 앞발과 뒷발이 체중을 넘기는 여덟 자세를 재생한다 |
| `ZapMouseSpikeHit01.png`–`ZapMouseSpikeHit04.png` | 찌릿쥐 · 가시 타격과 회수 | 피해를 준 틱에 표적 방향으로 머리와 앞쪽 가시를 낮추며 네 자세를 0.08초씩 재생한다 |
| `TreeGolem.png` | 고목 수호자 · 기본 자세 | `GroundedUnitPoseController` 대기 자세 |
| `TreeGolemWalk01.png`–`TreeGolemWalk08.png` | 고목 수호자 · 양발의 접촉, 체중 이동, 지나가기, 다음 착지 | 실제 XZ 이동 0.9칸마다 한 바퀴. 기준 속도 0.5칸/초에서 1.8초 |
| `TreeGolem2.png`, `TreeGolemRecover01.png`–`TreeGolemRecover03.png` | 고목 수호자 · 펀치 타격과 복귀 | 공격 이벤트에서 타격 프레임을 즉시 표시하고 0.05초 간격으로 복귀. 총 0.2초 |
| `BeanShooter.png`, `BeanShooterWalk01.png`–`BeanShooterWalk08.png` | BeanShooter · 대기와 네 뿌리발 보행 | 먼 뒷발, 먼 앞발, 가까운 뒷발, 가까운 앞발의 들기와 디디기 순으로 여덟 자세를 재생한다. 실제 XZ 이동 0.6칸마다 한 바퀴이며 기준 속도 0.6칸/초에서 1초 |
| `BeanShooterAttack.png`, `BeanShooterRecover01.png`–`BeanShooterRecover02.png` | BeanShooter · 발사와 줄기 복귀 | 공격 이벤트에서 발사 자세를 즉시 표시한다. 세 자세가 각각 0.08초씩 이어진다 |
| `StormStag.png`, `StormStagGallop01.png`–`StormStagGallop08.png` | 충격 사슴 · 대기와 네 발 질주 | 보간된 지면 이동 2.4칸마다 여덟 자세를 재생한다. 큰 좌표 보정 구간을 제외한다 |
| `StormStagImpact01.png`–`StormStagImpact03.png` | 충격 사슴 · 뿔 충돌과 체중 회수 | `HitEvent`가 시작점이다. 대상 방향으로 세 자세를 0.18초간 표시하고 유효한 이동 방향으로 질주를 이어 간다 |
| `EvilEnt.png` | 사악한 고목 · 기본 자세 | `EvilEnt.prefab` 기본 SpriteRenderer |
| `EvilEnt.png`, `EvilEntWalk01.png`–`EvilEntWalk08.png` | 사악한 고목 · 대기와 양쪽 뿌리발 보행 | 일반 고목은 이동 1.8칸마다 여덟 장을 재생한다. 차원문 보스는 기본 자세를 공유한다 |
| `EvilEnt2.png`, `EvilEntBrace02.png`, `EvilEntBrace03.png` | 사악한 고목 · 팔을 내보낼 때 상체를 지탱하는 자세 | 일반 고목과 차원문 보스가 공격 이벤트에서 세 장을 0.24초간 재생한다. 평상시와 공격 그림의 팔 시작점 높이가 같다 |
| `evil_ent_arm_segment.png`, `evil_ent_arm_fist.png`, `evil_ent_arm_fist_02.png`–`evil_ent_arm_fist_04.png` | 사악한 고목 · 뻗는 가지 팔과 주먹 네 자세 | `EvilEntPunchArm`의 팔 길이에 맞춰 가지를 반복한다. 주먹은 조이기, 서버 타격과 맞춘 최대 신장, 회복 순서로 바뀐다 |
| `evil_ent_grab_segment.png`, `evil_ent_grab_claw.png`, `evil_ent_grab_claw_02.png`–`evil_ent_grab_claw_04.png` | 사악한 고목 · 대상을 붙잡는 발톱 | 펼친 발톱이 22% 시점에 닿아 오므라든다. 끌어오는 0.8초 동안 잡은 자세가 이어진다. 회수 시간이 있으면 발톱을 다시 펼친다 |
| `evil_ent_burn_segment.png`, `evil_ent_burn_fist.png`, `evil_ent_burn_fist_02.png`–`evil_ent_burn_fist_04.png` | 사악한 고목 · 탄 가지 팔과 불주먹 | 주먹 안쪽 균열이 예열과 함께 밝아진다. 서버 타격 시점인 45%에 가장 넓게 벌어지고 회수를 시작하면 좁아진다 |
| `PlayerCharacterBase.png` | 수습 마법생 · 지팡이를 세운 기본 자세 | `Player.prefab` 의 `PlayerImage` SpriteRenderer |
| `PlayerCharacterAttack.png` | 수습 마법생 · 지팡이를 앞으로 뻗은 공격 자세 | `AttackSpriteSwapController.swapSprite`, 공격 이벤트에서 0.3초 표시 |
| `AquaArcher.png`, `AquaArcherWalk01.png`–`AquaArcherWalk08.png` | 물결 궁수 · 활을 든 대기와 양발 보행 | 실제 지면 이동 0.88칸마다 여덟 자세를 재생한다. 기준 속도 1칸/초에서 0.88초 |
| `AquaArcherAttack.png`, `AquaArcherRecover01.png`–`AquaArcherRecover02.png` | 물결 궁수 · 시위를 놓은 뒤 손을 내려 다음 화살을 거는 자세 | `WaterShot` 생성과 같은 서버 프레임의 `OnAttack`에 시작한다. 0.08초씩 세 자세를 표시한다 |
| `RockTurret.png` | 인간제 투석 포탑 · 장전 자세 | `RockTurret.prefab` 기본 SpriteRenderer |
| `RockTurretFire01.png`–`RockTurretFire04.png` | 인간제 투석 포탑 · 빈 슬링 발사, 반동, 복귀와 재장전 | 참조형 `RockShot`이 빈 슬링 중심에서 출발할 때 첫 자세를 표시한다. 네 장을 0.075초씩 재생한다 |
| `WaterSlime.png` | 물 슬라임 · 한 개체 기본 자세 | `WaterSlime.prefab` 기본 SpriteRenderer |
| `WaterSlimeCrawl01.png`–`WaterSlimeCrawl08.png` | 물 슬라임 · 지면 이동 | 실제 이동 0.96칸마다 여덟 자세를 재생한다 |
| `WaterSlimeSpit01.png`–`WaterSlimeSpit04.png` | 물 슬라임 · 물 뱉기와 복귀 | 네 자세를 0.08초씩 재생한다. 참조형 `WaterShot`이 시작되면 첫 자세와 방향을 맞춘다 |
| `PveBrickGolem.png` | 벽돌 골렘 · 두 주먹을 땅에 댄 기본 자세 | `PveBrickGolem.prefab` 기본 SpriteRenderer |
| `PveBrickGolem2.png` | 벽돌 골렘 · 한 팔을 높이 든 내려찍기 자세 | `BrickGolemPoseController`, 일반 공격 상태에서 0.1초 표시 |
| `PveBrickGolemSweep01.png`–`PveBrickGolemSweep04.png` | 벽돌 골렘 · 휩쓸기와 복귀 | `BrickGolemSweepPose`가 가리키는 보스의 몸 자세. 서버의 0.4초 신호에서 0.1초씩 표시 |
| `brick_golem_slam_warning_01.png`–`brick_golem_slam_warning_04.png` | 벽돌 골렘 · 내려찍기 예고 균열 | `BrickGolemSlamWarning`의 `GroundWarningProjectile`. 서버가 보낸 예고 시간의 0%, 25%, 50%, 75%에서 교체 |

실제 파일명과 `.meta` GUID는 런타임 참조 때문에 유지한다. 문서와 홈페이지에서는
숫자 접미사 대신 의미 이름을 표시한다.

## 공격 프레임 규칙

`OnAttackSpriteSwapper`는 공격 이벤트에서 공격 프레임으로 바꾸고 기본값 기준
0.2초 뒤 원래 프레임으로 복귀한다. Transform 보정 없이 Sprite만 바꾸므로 두
이미지가 아래 조건을 만족해야 한다.

`AquaArcher.prefab`의 `GroundedUnitPoseController`는 발사 자세를 먼저 표시하고
시위와 손이 돌아오는 두 프레임 뒤 이동 또는 대기 자세로 복귀한다. 물결 궁수의
`WaterShot`은 발사 그림의 활목에 맞춰 74px 오른쪽, 113px 위에서 출발하며
왼쪽을 쏠 때 가로 위치도 뒤집는다. `Tutorial/AquaArcher.prefab`은 공용 기본 그림을 사용한다.

`MagmaSpiritSpawnPresenter`는 일반 소환 연출을 재생하는 생성에서만 지면 돌파
프레임을 0.28초 표시한 뒤 선 기본 자세로 복귀한다. 동기화 복구처럼
`playSpawnPresentation`이 꺼진 생성에서는 소환 프레임을 건너뛴다.

`RockTurret`은 공용 `AbstractBuild`의 `AttackSpriteSwapController`를 사용한다.
다른 건물 프리팹은 `swapSprite`가 비어 있어 공격 이벤트를 구독하지 않으며,
`RockTurret`만 발사 직후 프레임을 0.1초 표시한다.

`EvilEnt`는 공용 `AbstractRangeMob`의 `AttackSpriteSwapController`를 사용한다.
`EvilEnt.prefab`은
`swapSprite`에 `EvilEnt2.png`만 지정하고 `duration`은 상위 프리팹 값 0.1초를
그대로 쓴다. 이 컨트롤러도 Transform은 건드리지 않고 `SpriteRenderer.sprite`만
바꾸므로 아래 조건이 그대로 적용된다.

`WaterSlimeSwarm.png`은 소환 마법의 무리 표시용 이미지이고 개별 소환수의 기본
프레임이 아니다. `WaterSlimeSwarmMagic`이 여러 `WaterSlime` 프리팹을 만들더라도
각 프리팹은 `WaterSlime.png` 한 개체를 표시한다. 파일명의 `Swarm`만 보고 그
이미지를 프리팹에도 연결하면 소환 수만큼 무리 그림이 중복되어 실제 개체 수가
잘못 보인다.

- 동일 PPU
- 동일 `Bottom Center` 피벗
- 동일 캐릭터 몸 배율
- 발 또는 지면 접점의 동일한 픽셀 기준점
- 동일 카메라 각도와 우측 방향
- 공격 동작에 필요한 팔·머리·효과만 변화
- 캔버스가 달라도 기준점에서 몸통 핵심점까지의 거리는 동일

검증할 때 두 PNG를 같은 PPU로 겹쳐 발점, 골반, 머리 중심을 대조한다.
`RockGolem`과 `RockGolem2`는 256x244, PPU 100, Bottom Center로 교정했다.
이끼바위 골렘의 보행과 펀치 복귀도 같은 캔버스와 피벗을 사용한다. 보행의 고유 자세는
9개다. 4번 자세의 기존 4칸 중 뒤 2칸을 먼 발이 앞으로 나가는 중간 자세에 사용한다.
체중 이동은 양쪽 모두 기존 배열의 4칸 동안 이어진다.
`TreeGolem`과 `TreeGolem2`는 240×256 / 312×256, PPU 85, Bottom Center다.
이슈 #482에서 펀치 자세의 발 간격을 75px로 조정했다. 기본 자세는 77px이며 두 자세의 지면 접점 차이는 1px 이하다.
보행 프레임에는 기본 자세의 캔버스를, 펀치 복귀에는 타격 자세의 캔버스를 사용한다.
`GroundedUnitPoseController`는 `OnMoved` 대신 루트의 실제 XZ 변위를 잰다. 고목
수호자의 기준 속도 0.5칸/초에서는 60Hz 위치 갱신당 이동량이 약 0.0083칸으로,
`PositionUpdater`의 `OnMoved` 조건인 0.01칸을 넘지 않을 수 있다. 보행을 연결할 때는
20Hz와 60Hz의 실제 위치 갱신으로 모든 프레임을 지나는지 확인한다.

`EvilEnt`와 `EvilEnt2`는 224x256, PPU 100, Bottom Center로 일치한다. 두 프레임을
프레임마다 따로 자르지 않고 하나의 공유 크롭 박스와 하나의 공유 배율로 함께
내보냈기 때문에, 지면 접점 픽셀이 사후 대조가 아니라 제작 방식으로 이미 동일하다.
`TreeGolem2`는 기존 펀치 캔버스와 주먹의 최대 가로 위치를 유지했다. 기본 자세의 양발을 기준으로 펀치 그림을 정렬했다.

`WaterSlime` 대기와 새 이동·발사 프레임은 320×224, 200 PPU, 하단 중앙 피벗을
공유한다. 기존 대기의 폭을 기준으로 몸통을 캔버스 왼쪽에 등록한다. 발사 그림의 입은
좌상단에서 (169.5, 164.5)px이며 피벗 기준 월드 오프셋은 (0.0475, 0.2975)다.
`WaterShot`은 렌더러에서 이 입 좌표를 좌우로 반전해 시작한다.

## 오라 의미

| 실제 파일 | 의미 이름 | 사용 방식 |
|---|---|---|
| `fire_aura.png` | 지옥불 · 대기 오라 / 공격 파동 | `FireIdleAura`, `FireAttackAura` |
| `wind_aura.png` | 바람 · 대기 오라 / 공격 파동 | `WindIdleAura`, `WindAttackAura` |
| `ligtning_aura.png` | 전기 · 대기 오라 / 공격 파동 | `LightningIdleAura`, `LightningAttackAura` |
| `nature_aura.png` | 자연 · 대기 오라 / 공격 파동 | `NatureIdleAura`, `NatureAttackAura` |
| `rock_aura.png` | 바위 · 대기 오라 / 공격 파동 | `RockIdleAura`, `RockAttackAura` |
| `water_aura.png` | 물 · 대기 오라 / 공격 파동 | `WaterIdleAura`, `WaterAttackAura` |
| `cloud.png` | 운룡 · 구형 물 아우라 | `CloudDragon.prefab` 전용 자식 SpriteRenderer |

오라는 본체에 합성하지 않는다. 공용 투명 Sprite로 별도 생성한다.

- 대기 오라: 반복 맥동, 알파 70%까지 변화, 약한 ±3도 회전
- 공격 파동: 0.3초 동안 0.86배 수축 후 1.35배 확장하며 페이드아웃
- 중심 정렬, 방사형 또는 타원형 실루엣
- 캐릭터 얼굴·팔다리·무기와 경쟁하는 고밀도 디테일 금지
- 불·운룡처럼 오라가 정체성에 필요한 개체도 본체와 오라를 독립 검증
- 운룡의 `cloud.png`는 바람 오라가 아니라 몸 전체를 감싸는 구형 물 아우라다.
  중앙은 본체가 읽히도록 저밀도로 유지하고, 공용 `wind_aura.png`와 혼용하지 않는다.

## 플레이어 프레임과 오라

플레이어는 다른 유닛과 세 가지가 다르다.

- 프레임이 세 장이다. 기본은 지팡이를 내린 자세고, 카드를 한 장 이상 고르면 세운
  자세, 시전이 성공하면 0.3초 동안 앞으로 뻗은 자세다.

  | 파일 | 자세 |
  |---|---|
  | `Customize/PlayerCharacterBase.png` | 지팡이를 땅에 짚고 내린 기본 자세 |
  | `Customize/PlayerCharacterStaffRaised.png` | 지팡이를 세운 자세 |
  | `Customize/PlayerCharacterAttack.png` | 지팡이를 앞으로 뻗은 자세 |

- pivot 이 Bottom Center 도 Center 도 아닌 custom 이다. 캔버스 981x1245, PPU 566,
  pivot `(0.3751, 0.0072)`. 발끝이 pivot 위에 서고, 서 있을 때의 가로 중심이
  pivot x 다. 세 프레임은 같은 캔버스에 같은 배율로, 발끝 행과 그 가로 중심을
  맞춰 올린다. 몸통 높이는 940px = 1.66 units 로, 그 자리에 있던
  `Assets/Resources/Game/player.png` 의 166px @ 100 PPU 와 같은 크기다. 맞춰 올린
  뒤 세 프레임의 눈 위치는 가로 27px, 세로 10px 안에 들어온다.
- 오라가 몸 전체를 감싸지 않고 지팡이 끝 한 점에 모인다. 그래서 오라는 본체와 같은
  캔버스에 그리지 않고, 작게 그린 뒤 `StaffAura` 앵커의 localPosition 으로 지팡이
  끝에 놓는다.
- 지팡이 머리는 앱 아이콘의 말린 지팡이다(이슈 #116). 세 프레임을 다시 그리지 않고
  `.art/tools/transplant-staff-head.py` 가 옛 머리를 지우고
  `.art/concept/player-staff/icon-crystal-alpha.png` 의 머리를 같은 배율로 붙였다.
  그래서 몸 픽셀은 원본 그대로고 세 머리는 같은 픽셀이다. 지팡이 머리를 다시 바꿀
  때도 이 방식을 먼저 쓴다. `image_gen` 으로 프레임 전체를 다시 그리면 몸의 크기와
  위치가 어긋난다.
- 결정의 중심이 오라 앵커다. `Player.prefab` 의 `idleLocalPosition` 은 세운 프레임의
  옛 결정 중심 `(154, 98)`, `attackLocalPosition` 은 뻗은 프레임의 옛 결정 중심
  `(909, 731)` 을 가리킨다(캔버스 픽셀). 말린 머리는 결정 위로 말려 올라가서, 그
  자리에 결정을 두면 세운 프레임은 캔버스 위로, 뻗은 프레임은 오른쪽으로 잘린다.
  그래서 지금 결정 중심은 세운 프레임 `(143, 110)`, 뻗은 프레임 `(872, 717)` 로
  옮겨 있고 prefab 값은 그대로다. 오라가 결정에서 떨어져 보이면 prefab 값을 새
  결정 중심으로 옮긴다.

지팡이 끝에 뜨는 오라는 새 에셋이 아니라 원래 있던 원소 오라 그대로다. 서버는
플레이어가 카드를 고르면 그 원소의 `FireIdleAura` 같은 effect 를 플레이어
오브젝트에 붙이고, 여러 원소를 고르면 겹쳐서 붙는다. 시전이 성공하면 대기 오라가
끝나고 `FireAttackAura` 계열이 0.3초 동안 뜬다.

`ServedObject._effectAnchor` 가 이 effect 들의 부모를 정한다. 비워 두면 지금까지처럼
오브젝트 자신에게 붙고, 플레이어만 `StaffAura` 앵커를 가리켜서 지팡이 끝에 모인다.
`PlayerStaffAuraController` 가 공격 이벤트에 맞춰 그 앵커를 세운 지팡이 끝에서 뻗은
지팡이 끝으로 옮기고 0.3초 뒤 되돌린다. 0.3초는 서버의 공격 오라 지속 시간과 맞춘
값이다.

**앵커에 올라갈 effect 는 이름으로 고른다.** `_effectAnchor` 를 모든 effect 에
쓰면 `Burn`·`Panic`·`Snared` 같은 상태 effect 까지 지팡이 끝으로 날아간다. 불이
얼굴 위가 아니라 지팡이 끝에서 타는 것이 화면에서 바로 보인다. `ServedObject`
의 `_effectAnchorEffects` 에 적힌 이름만 앵커로 가고 나머지는 몸에 붙는다.
프리팹에는 원소 오라 열두 개(`FireIdleAura`·`FireAttackAura` … `RockAttackAura`)
가 들어 있다. 목록이 비면 앵커는 아무 데도 안 쓰이므로, 앵커를 지정하지 않은 다른
오브젝트의 동작은 그대로다.

카드를 고르지 않았으면 지팡이는 내려가 있고 그 끝에 아무것도 없다.

`PlayerStaffPoseController` 가 세 프레임 사이를 고른다. 이 컴포넌트 하나가
`SpriteRenderer.sprite` 를 독점해야 한다. `AttackSpriteSwapController` 를 같이
붙이면 공격 중에 자세가 바뀔 때 뻗은 프레임이 덮이거나, 그쪽의 복원이 이미 지난
자세를 되돌려 놓는다. 그래서 플레이어 프리팹에서는 `AttackSpriteSwapController`
를 떼고 이것으로 바꿨다. 다른 오브젝트는 그대로 쓴다.

"카드를 골랐다"는 신호는 서버가 보내는 effect 다. 클라이언트의 카드 UI 상태를
쓰면 상대편 플레이어는 영영 지팡이를 들지 않는다. 어떤 effect 가 지팡이를 들게
하는지는 프리팹의 `raisingEffects` 에 있다.

거기 `CardSelected` 가 있다. 게임 서버가 카드 종류를 가리지 않고 한 장이라도
골려 있으면 붙이는 effect 다. 원소 오라로는 부족하다 — `CardSelectVisualizer` 가
`Fire`·`Water`·`Nature`·`Lightning`·`Rock`·`Wind` 여섯 장만 오라로 바꾸고
`Shoot`·`Drop`·`Build`·`Spawn`·`Explode` 는 흘려보내므로, Shoot 카드를 골라도
effect 가 하나도 안 실린다.

원소 대기 오라 여섯 개도 `raisingEffects` 에 그대로 두었다. `CardSelected` 와
겹치지만, 클라이언트가 서버보다 먼저 배포되면 그 여섯 개가 지금까지의 동작을
유지해 준다.

`CardSelected` 는 보이는 것이 없다. effect 하나에 프리팹 하나라는 규칙은 그대로
지켜서 `Assets/Resources/Prefabs/Effects/CardSelected.prefab` 을 Transform 만 있는
빈 오브젝트로 두었다. 빼면 `ServedObjectEffectRenderer` 가 프리팹을 못 찾았다고
경고를 남기는데, 그 경고는 오타를 잡아 주는 것이라 죽이지 않는다.

상대편 플레이어는 같은 스프라이트를 `flipX` 로 뒤집어 쓴다. 앵커도 x 를 뒤집어야
지팡이 끝에 남는다.

공격할 때 몸 전체를 뒤로 기울이던 `DOTweenAction.SwingMobAttack` 은 이 프레임
교체로 대체했다. 다른 오브젝트는 그대로 쓴다. 카드를 고를 때 몸을 25도 젖히던
회전도 같이 뺐다. 남은 것은 squash-and-stretch bounce 하나다.

### 인게임 플레이어가 쓰는 sprite 는 `Customize` 폴더에 없었다

`Assets/Resources/Prefabs/Player.prefab` 의 `PlayerImage` SpriteRenderer 가
가리키던 것은 `Assets/Art/Images/Customize/PlayerCharacterBase.png` 가 아니라
`Assets/Resources/Game/player.png` 다. 이름만 보고 Customize 쪽 파일을 갈아
끼우면 화면은 하나도 안 바뀐다. 공격 frame 만 새 그림으로 0.3초 떴다가 사라지고,
그 frame 이 2048x2048 @ 100 PPU 면 옛 sprite 의 열 배 크기로 뜬다.

sprite 를 갈아 끼울 때는 파일 이름이 아니라 prefab 과 scene 의 `m_Sprite` guid
로 고른다. 플레이어 캐릭터가 나오는 곳은 네 군데다.

| 쓰는 곳 | 무엇 |
|---|---|
| `Assets/Resources/Prefabs/Player.prefab` | 인게임 플레이어 |
| `Assets/Scenes/InteractiveTutorialScene.unity` | `LeftPlayer`/`RightPlayer` 의 `PlayerSprite` |
| `Assets/ScriptableObject/Adventures/Stage1.asset` | 시나리오 네 개의 `leftImage` |
| `Assets/Scenes/TEST_DOTween/DOTweenTestScene.unity` | tween 실험용 `player` 오브젝트 |

네 곳 모두 새 sprite 를 쓴다. `Assets/Resources/Game/player.png` 은 참조가 하나도
남지 않아 지웠다. 크기 기준으로 쓰던 192x170 @ 100 PPU, bottom-center pivot,
몸통 166px 은 `.art/tools/finalize-player-frames.py` 안에 상수로 남아 있다.

모험 초상화는 `AdventureStoryOverlayUI` 가 420x640 Image 에 `preserveAspect` 로
넣는다. 새 sprite 는 981x1245 라 세로가 아니라 가로에 맞춰 420x533 으로 들어가고
몸통이 402px 로 뜬다. 옛 `player.png` 는 192x170 이라 420x372 에 몸통 363px
이었으니 초상화가 조금 커진다.

`Assets/Art/Images/Customize/` 의 모자·망토 여덟 장(`ancient_*`, `leaf_*`)은 아직
참조가 하나도 없고 `Assets/Scripts/CustomizeScene` 도 없다. 커스터마이즈 기능을
살릴지 정해지지 않아 그대로 두었다.

튜토리얼은 SpriteRenderer 라 배율을 건드릴 것이 없다. 몸통이 두 sprite 모두
1.66 units 이고 발이 pivot 위에 서기 때문이다. `RightPlayer` 는 `m_LocalScale.x`
가 -1 인데, 이 반전도 pivot 기준이라 몸통이 제자리에서 뒤집힌다.

### 생성된 두 프레임은 그대로는 안 맞는다

`player-D-base-raised-v3.png` 와 `player-D-attack-thrust-v7.png` 는 같은
1145x1374 캔버스로 나왔지만 발끝 행이 1317 과 1279 로 38px, 서 있는 가로 중심이
618.5 와 520.5 로 98px 어긋나 있다. 그대로 올리면 공격할 때 캐릭터가 떠오르면서
옆으로 미끄러진다. `.art/tools/finalize-player-frames.py` 가 둘을 맞춰 올리고
PPU·pivot·앵커 좌표를 같이 출력한다.

그 script 는 base frame 의 머리 꼭대기 행을 인자로 받는다. 세운 지팡이가 머리
위에 있어서 alpha 채널만으로는 찾을 수 없고, 폭 비율 규칙도 폭 급변 규칙도 둘
다 틀린 행을 고른다. 측정해서 넘긴다.

### `image_gen` 이 투명 배경을 안 준다. 단색 키로 받아서 깎는다

이 기계의 `image_gen` 은 투명 배경을 요청하면 **격자무늬를 픽셀로 그려 넣는다.**
그린 격자무늬는 투명이 아니라 불투명 배경이고, 두 색이 섞여 있어 깎아내기도
어렵다. 지팡이 내린 프레임을 받으려고 10번을 돌려 alpha 가 들어온 것이 0번이었다.

대신 **평평한 단색을 그려 달라고 하면 된다.** 순수 초록 `#00FF00` 으로 받고
`.art/tools/key-out-background.py` 로 깎는다. 초록인 이유는 이 캐릭터 팔레트에
초록이 한 점도 없어서다 — 갈색 머리, 파란 클록, 회색 로브, 피부, 금색이 전부
빨강 아니면 파랑 우세다. 자홍색은 이 프로젝트가 결과를 눈으로 확인할 때 까는
색이라 키 색으로 쓰지 않는다.

깎는 방식은 임계값이 아니다. `C = a·F + (1-a)·K` 로 보고 키 채널이 나머지 두
채널 중 큰 쪽을 넘어선 만큼을 배경 기여분으로 읽어 픽셀마다 alpha 를 구하고,
경계 픽셀에서는 키 색을 도로 나눠 뺀다. 그래서 초록 테두리가 남지 않는다.

두 가지를 여기 적어 둔다.

- **테두리에서 연결된 것만 지우면 안 된다.** 지팡이와 팔과 몸 사이의 빈 틈은
  캐릭터에 둘러싸여 있어서 초록 덩어리로 남는다. 처음 짰을 때 5,189픽셀이 그렇게
  남았다. 캐릭터에 키 색이 없다는 전제로 전부 지우고, 대신 테두리에서 떨어진 채
  지워진 픽셀 수를 출력한다. 키 색이 캐릭터에 묻으면 구멍 대신 숫자로 드러난다.
- **키 fill 이 bit 단위로 정확하지 않다.** 맨 바깥 행이 coverage 0.07 쯤으로
  들어와서, 배경 판정선이 0.06 이면 1픽셀짜리 불투명 테두리가 남고 그 뒤의 모든
  측정이 그것을 내용으로 읽는다. 판정선을 0.10 에 둔다.

검증은 왕복으로 한다. alpha 가 이미 제대로 들어 있는 프레임을 초록 위에 합성한
뒤 다시 깎아 원본과 비교하면, alpha 차이는 평균 0.69/255, 크게 틀린 픽셀은 157만
중 29개, 캐릭터 안쪽 색 변화는 평균 0.95 였다.

`.plan/issues/2026-09-09-issue-586-restyle-player-sprite.md` 의 "생성된 배경을
스크립트로 지워서 투명하게 만들지 않는다"는 원래 격자무늬와 사진 같은 배경을
두고 쓴 규칙이다. 요청해서 받은 단색 키는 그 규칙의 대상이 아니다.

## 범위 표시 오라

위 오라 의미 표의 element 오라 6종은 **상태 표시**다. 서버가 보낸 effect 이름을
`ServedObjectEffectRenderer`가 `Prefabs/Effects/<이름>`으로 올리는 것이고, 그 개체가
무슨 속성 상태인지를 말한다. 범위 표시는 다른 것이다. 서버 component 의 반지름이
어디까지 닿는지를 말한다. 둘은 별도 에셋으로 유지하고 섞어 쓰지 않는다. 같은 그림을
쓰면 같은 고리가 한쪽에서는 "자연 속성 상태", 다른 쪽에서는 "repair 반경"을 뜻하게
된다.

| 실제 파일 | 의미 이름 | 사용 방식 |
|---|---|---|
| `Effect/AreaOfEffect/RepairAura.png`, `RepairAuraLight02.png`–`RepairAuraLight06.png` | repair_totem · 수리 범위 경계와 잎의 불빛 | `RepairTotem.prefab`의 `RepairAura`에 연결. `effect_radius`를 기준으로 XZ 바닥에 두고, 0.28초 간격으로 여섯 장을 재생한다 |
| `Effect/AreaOfEffect/RallyZone.png`, `RallyZoneEmber01.png`–`RallyZoneEmber08.png` | rallying_totem · 집결 구역 경계와 불씨 | `RallyingTotem.prefab`의 `RallyZone`에 연결. `AuraRadiusScaler`가 `effect_radius`로 크기를 정하고 `GroundPinnedChild`가 낙하 중에도 XZ 바닥에 놓는다. 불빛은 0.22초 간격으로 시계 방향으로 이어진다 |

- **상태 오라를 키워서 범위 표시로 쓰지 않는다.** element 오라 6종은 전부 가로
  1.28 world unit 로 그려져 있고 실제 최대 사용처가 `FireSpirit`의 2.8배, 즉 3.58
  unit 이다. 범위 표시는 8 unit 을 넘는다. 2026-09-14 에 `nature_aura.png`를 6.25배로
  늘려 붙였다가 되돌렸다. legacy 오라 그림은 잎마다 외곽선을 두르는데 `STYLE.md`는
  "No outer contour line"이라 그 외곽선이 화면의 다른 어떤 선보다 6배 굵어졌다.
- 범위 표시는 그려질 크기를 정해놓고 그 크기에 맞춰 새로 뽑는다. 띠 두께는 지름의
  10% 안팎, 가운데는 완전히 비운다. 안에 선 유닛을 가리면 안 된다.
- 정사각 canvas 로 내보낸다. `AuraRadiusScaler`가 sprite rect 의 가로·세로 절반으로
  각 축 배율을 따로 내므로, 정사각이어야 world 에서 원이 되고 prefab 의 localScale 도
  균일해진다.
- 반지름은 prefab 에 적지 않고 `ParametersDataSource`가 캐시한 parameter 표에서
  읽는다. 서버가 읽는 그 표다.
- **sprite 는 필드 경계로 잘리지 않는다.** `SkillIndicatorShapeRenderer`는 X 0..18,
  Z 0..10 으로 polygon 을 잘라내지만 SpriteRenderer 에는 그런 장치가 없다. 가장자리에
  세우면 그림이 판 밖으로 나간다. 잘림이 꼭 필요해지면 mesh 쪽으로 옮겨야 한다.

## 홈페이지 표시

- 기본·공격 프레임이 실제로 다른 포즈면 둘 다 표시한다.
- 컨셉과 축소 인게임 이미지가 같은 포즈면 컨셉만 표시한다.
- 오라는 소환수 상세의 `부속 에셋`으로 표시한다.
- 마법 상세와 소환수 상세은 양방향 링크를 제공한다.

## 거신의 잔해 부속 공격

| 실제 파일 | 의미 이름 | 런타임 연결 |
|---|---|---|
| `TitanRemnant.png` | 거신의 잔해 · 고정 본체 | `TitanRemnant.prefab` 기본 SpriteRenderer |
| `TitanFist.png` | 거신의 잔해 · 솟구치는 돌주먹 | `TitanFist.prefab` 기본 SpriteRenderer |

`TitanFist`는 본체의 공격 프레임이 아니라 적 위치에 별도 생성되는 ServedObject다.
따라서 본체와 공유 캔버스를 쓰지 않지만, 같은 석재 크기·팔레트·카메라·광원을
유지해 한 개체의 부속 공격으로 읽혀야 한다. 주먹은 `TitanFistPresenter`가 아래에서
위로 솟구치는 위치 연출을 담당하므로 스프라이트 자체에는 이동 잔상이나 지면
그림자를 합성하지 않는다.
## 해일 탄두 변형

| 실제 파일 | 의미 이름 | 런타임 연결 |
|---|---|---|
| `TidalWarhead.png` | 해일 탄두 · 공중 표적 · 어두운 눈 | `TidalWarhead.prefab` 기본 SpriteRenderer |
| `GroundTidalWarhead.png` | 해일 탄두 · 지상 표적 · 밝은 눈 | `GroundTidalWarhead.prefab` 기본 SpriteRenderer |

두 파일은 애니메이션 프레임이 아니라 표적 종류에 따라 선택되는 별도 프리팹이다.
두 형식은 `256x256`, PPU 100, 하단 중앙 피벗, 실루엣,
배율과 캔버스 배치를 공유한다. 지상형은 눈 내부 색만 밝게 바꾸며 몸, 지느러미,
꼬리, 입과 알파는 공중형과 픽셀 단위로 동일하게 유지한다.

`TidalWarheadFlight01–06`과 `GroundTidalWarheadFlight01–06`은 동일한 여섯 자세다. 앞끝을 맞추고 꼬리와 가까운·먼 지느러미가 차례로 휘게 그렸다. 둘 다 개체가 표시될 때 0.11초 간격으로 반복한다. 대응 자세는 눈 안쪽의 RGB 값만 다르다.

## 지옥불 군단장 프레임

| 파일 | 표시 동작 | 연결 |
| --- | --- | --- |
| `FireLordSpiritHover01–06.png` | 외투 끝과 뒷갑각의 공중 대기 | `AerialUnitPoseController.hoverFrames` |
| `FireLordSpiritApproach01–06.png` | 외투가 뒤로 흐르는 접근 | `AerialUnitPoseController.approachFrames` |

256×162 RGBA, PPU 100, 하단 중앙 피벗이다. 각각 장당 0.17초씩 1.02초 주기로 반복한다. 접근은 같은 방향의 수평 위치 갱신 두 건에서 시작한다. 자식 소환으로 한 차례 밀리거나 수직으로만 이동할 때는 대기 자세를 유지한다.

## 군단장의 불꽃 정령 프레임

| 파일 | 표시 동작 | 연결 |
| --- | --- | --- |
| `FireChildSpiritHover01–06.png` | 날개 판과 꼬리를 접었다 펴는 공중 대기 | `AerialUnitPoseController.hoverFrames` |
| `FireChildSpiritRelease01–04.png` | 앞쪽 건틀릿의 불꽃 발사와 복귀 | `AerialUnitPoseController.releaseFrames` |

228×256 RGBA, PPU 200, 하단 중앙 피벗이다. 대기는 장당 0.17초씩 1.02초 주기로 반복한다. 발사는 소환된 정령의 개별 공격 이벤트에 맞춰 장당 0.08초씩 재생한다. 불꽃 투사체는 기존 `FireShot`을 사용한다.

## 번개 정령 프레임

| 파일 | 표시 동작 | 연결 |
| --- | --- | --- |
| `ThunderSpiritHover01–06.png` | 윗부분과 꼬리를 접었다 펴는 공중 대기 | `AerialUnitPoseController.hoverFrames` |
| `ThunderSpiritRelease01–04.png` | 지상 발사와 복귀 | `AerialUnitPoseController.releaseFrames` |

256×226 RGBA, PPU 200, 하단 중앙 피벗이다. 대기는 장당 0.17초씩 1.02초 주기로 반복한다. 발사는 공격 이벤트에 맞춰 장당 0.08초씩 재생한다. 서버가 소멸을 알리면 기존 사라짐 연출이 마지막 자세를 사용한다.

## 거품 정령 프레임

| 파일 | 표시 동작 | 연결 |
| --- | --- | --- |
| `BubbleSpiritHover01–06.png` | 윗물결과 뒤 물방울이 접혔다 펴지는 공중 대기 | `AerialUnitPoseController.hoverFrames` |
| `BubbleSpiritRelease01–04.png` | 앞부분으로 거품을 밀어내는 발사와 복귀 | `AerialUnitPoseController.releaseFrames` |

250×256 RGBA, PPU 200, 하단 중앙 피벗이다. 대기는 0.17초 간격으로 1.02초 반복한다. 발사는 공격 이벤트에서 첫 자세를 표시하고 0.08초 간격으로 복귀한다. 공중 위치는 서버 위치를 따르고 거품은 기존 `BubbleShot`으로 표시한다.

## Cloud Dragon 프레임

| 실제 파일 | 의미 | 프리팹 연결 |
|---|---|---|
| `CloudDragon.png` | 기본 그림 | `CloudDragon.prefab` 기본 SpriteRenderer |
| `CloudDragonHover01–06.png` | 날개를 펴고 접는 공중 대기 | `AerialUnitPoseController.hoverFrames` |
| `CloudDragonRelease01–04.png` | 턱 방출과 반동·회수 | `AerialUnitPoseController.releaseFrames` |

열 자세는 256×182 RGBA, PPU 116.363636, Bottom Center 피벗을 공유한다. 대기는 0.17초씩 반복하고 발사·회수는 0.08초씩 재생한다. 1.50초 물 발사 뒤 1.60초 연쇄 번개처럼 재생 도중 다음 Attack이 오면 해당 틱에 01을 표시한다. 발사 그림에서는 입과 날개가 움직이고, 몸 바깥의 물과 번개는 각각 `WaterShot`과 `ChainLightning`이 그린다. 기존 540×182 `CloudDragonAttacking.png`는 이전 물 분사 그림이다.

운룡 변형에서 상속 Hover와 공통 공격 회전을 끈다. 몸통 렌더러의 부모는 유지하고 구형 물 아우라 `cloud.png`는 같은 자식 렌더러에 둔다. 정렬은 두 시퀀스에서 눈의 중심과 캔버스의 고정 피벗을 대조한다. 날개 끝이나 턱처럼 움직이는 외곽선으로 캔버스를 잘라 정렬하면 고정된 구름 몸통까지 떨려 보인다.

## 번개 강타 프레임

`LightningDropMagic`의 먹구름은 공격할 때마다 스프라이트가 바뀐다. 번개는 별도
오브젝트가 아니라 먹구름 스프라이트 안에서 구름 밑으로 자라는 프레임이다.

| 실제 파일 | 의미 이름 | 런타임 연결 |
|---|---|---|
| `sprites/LightningCloud.png` | 먹구름 · 대기 | `LightningCloud.prefab` 기본 SpriteRenderer |
| `sprites/LightningCloudStrike0.png` | 강타 · 구름 밑에 번개가 돋음 | `AttackFrameSequenceController.frames[0]` |
| `sprites/LightningCloudStrike1.png` | 강타 · 절반까지 자란 줄기 | `frames[1]` |
| `sprites/LightningCloudStrike2.png` | 강타 · 지면 직전까지 자란 줄기 | `frames[2]` |
| `sprites/LightningCloudStrike3.png` | 강타 · 지면 도달, 최대 밝기 | `frames[3]` |
| `sprites/LightningCloudStrike4.png` | 강타 · 잔광 | `frames[4]` |
| `sprites/LightningCloudStrike5.png` | 강타 · 소멸 직전 | `frames[5]` |

`AttackFrameSequenceController`는 공격 이벤트에서 여섯 프레임을 0.05초 간격으로
한 번 재생하고 대기 프레임으로 돌아온다. 한 자세를 잠깐 들고 있는
`AttackSpriteSwapController`와 달리, 자라나는 동작이 필요한 연출에 쓴다.

번개 오브젝트는 없다. 서버는 먹구름이 자기 아래 기둥을 범위 판정으로 때리고
`LightningDrop` 프리팹은 서버·클라이언트 양쪽에서 사라졌다. 지면 연출은
`ElectricField`가 담당한다.

### 캔버스 불변식

- 일곱 파일 모두 `320x640`, PPU 160, 중앙 피벗이다. 월드로는 `2.0 x 4.0` 유닛.
- 서버가 먹구름을 높이 `y=2`(`AERIAL_STANDARD_HEIGHT`)에 소환한다. 중앙
  피벗이므로 스프라이트는 지면 `y=0`부터 `y=4`까지를 정확히 덮는다. 구름은
  캔버스 최상단(월드 `y 2.8~4.0`), 번개는 구름 밑면(`y≈2.95`)에서 지면까지.
- 먹구름 높이는 서버 소환 좌표가 아니라 캔버스 안에서의 구름 위치로 조절한다.
  강타 판정 박스는 지면부터 `y=10`까지라 그림만 움직여도 판정은 그대로다.
- 여섯 프레임 x 0.05초 = 0.3초는 서버
  `LightningCloud.STRIKE_VISUAL_DURATION`과 같은 값이어야 한다. 서버는 마지막
  강타 뒤 그만큼 구름을 더 살려 두고 파괴한다. 한쪽만 바꾸면 마지막 번개가
  중간에 잘린다.
- `LightningCloud` 변형은 `Selectable`을 제거하고 `ServedObject._swingOnAttack`을
  끈다. 스프라이트가 지면까지 닿아서 선택 콜라이더가 그 아래 필드 클릭을 먹고,
  공격 연출의 30도 스윙은 캔버스 중심을 축으로 돌아 번개를 옆으로 던진다.

### 합성

이슈 #479에서는 기존 일곱 장의 y=168 위에 같은 다면체 먹구름을 합성했다. y=168부터 지면까지의 픽셀은 기존 프레임과 같다.

초기 프레임은 아래 명령으로 합성했다. 원본 두 장
(`.art/concept/lightning-strike/`)에서 다시 만들 수 있다. 검수 시트는
`.art/sheets/lightning-strike-review.png`.

```bash
.art/tools/compose-lightning-strike.py \
  --cloud .art/concept/lightning-strike/storm_cloud_source.png \
  --bolt .art/concept/lightning-strike/lightning_bolt_source.png \
  --idle-out Assets/Resources/Game/sprites/LightningCloud.png \
  --frame-out 'Assets/Resources/Game/sprites/LightningCloudStrike{index}.png'
```

## 폭발 이펙트 프레임 (`razor_gale`, `electric_explode`)

`Assets/Resources/Game/explode/`의 `razor_gale_frame_0`~`razor_gale_frame_3`
(192×155)는 4프레임이며, `electric_explode_frame_0/1` (192×116)은 2프레임이다.
두 효과는 `SpriteFrameAnimator`에서 0.12초 간격으로 반복하고 PPU는 100이다.
칼바람 회오리의 네 프레임은 공통 투명 캔버스와 하단 기준선을 사용한다.

### `.meta` 의 `spritePivot` 을 믿지 마라. `alignment` 가 이긴다

두 세트 모두 `spritePivot: {x: 0.5, y: 0.5}` 로 적혀 있지만 `alignment: 7` 이다.
Unity 는 `alignment` 가 9 (Custom) 일 때만 `spritePivot` 을 쓰므로, 런타임 피벗은
중앙이 아니라 **Bottom Center** 다. `Cloud Dragon 프레임` 절이 같은 함정을 이미
적어 뒀다.

차이가 그대로 드러난다. 생성물의 가로세로 비가 원본과 달라 비율을 지켜 넣으면
캔버스에 세로 여백이 남는데, 그것을 위아래로 나눠 가운데 정렬하면 이펙트가
아래 여백만큼 공중에 뜬다. 2026-09-18 작업에서 `razor_gale` 이 7px,
`electric_explode` 가 4px 떴고, `check-replacement.py` 가 네 장 모두
`바닥 여백이 0px 에서 Npx 로 늘었다` 로 잡았다. 남는 여백은 전부 위로 보내고
그림은 캔버스 바닥에 붙인다.

`check-frame-pair.py` 는 `spritePivot` 을 읽으므로 이 문제를 못 잡는다. 두
프레임을 똑같이 가운데 정렬하면 서로는 어긋나지 않아서 `통과` 가 나온다.
두 스크립트를 같이 돌려야 한다.

### 합집합 crop box 를 재기 전에 alpha 바닥을 깔아라

`key-out-background.py` 를 거친 파일에도, `image_gen` 이 alpha 를 직접 돌려준
파일에도 alpha 가 1~7 인 픽셀이 캔버스 구석에 남는다. `Image.getchannel("A")
.getbbox()` 는 0 이 아닌 값을 전부 내용으로 읽으므로, 두 프레임의 bounding box
합집합이 캔버스 전체가 된다. 2026-09-18 에 `electric_explode` 가 이렇게 배율
0.094 로 계산돼 캔버스의 59% 만 채운 채 한 번 나갔다. 재기 전에 8 미만을 0 으로
내리고, 내보낼 때 같은 바닥을 한 번 더 깐다.

## 돌 가고일 날갯짓

`StoneGargoyleFly1`은 날개 올림, `StoneGargoyleFly2`는 수평, `StoneGargoyleFly3`은 날개 내림이다. `StoneGargoyle.prefab`의 `SpriteFrameAnimator`가 `1, 2, 3, 2`를 0.16초 간격으로 반복한다. 경계의 1을 공유하므로 화면에서는 `1 → 2 → 3 → 2 → 1 → 2…`로 왕복한다.

세 파일은 192x165, PPU 100, Bottom Center이며 몸통 배율·눈 가로 기준·발톱 하단을 공유한다. `spriteMeshType: 0`인 Full Rect를 유지해 날개 자세마다 표시 경계가 바뀌지 않게 한다. 기존 정적 `StoneGargoyle.png`는 마법 아이콘으로 유지한다.

## 천둥 새 비행

`ThunderBirdFlap01–08`은 0.115초 간격으로 반복한다. 가까운 날개가 먼저 각도를 바꾸면 반대쪽 날개가 조금 늦게 따라온다. `ThunderBirdDive01–04`는 하강 시작 고도에서 0.4 월드 단위씩 내려갈 때 바뀐다. `ThunderBirdStrike01–04`는 피해 이벤트에서 시작해 0.08초 간격으로 부리 타격과 회수를 보여준다. `ThunderBirdClimb01–04`는 상승 중 0.125초 간격으로 반복한다. `ThunderBirdFall01–04`는 사망 시작 고도부터 지면까지 내려온 거리에 맞춰 날개를 접는다.

24장은 368×280 RGBA, Full Rect, 200 PPU다. 공통 피벗은 이미지 하단에서 45px 위의 중앙 `(184, 45)`다. 머리와 몸통이 날갯짓 중에 크게 흔들리지 않도록 위치를 맞췄다. 전투 개체는 `ThunderBirdFlightPresenter`가 표시하고 마법 아이콘은 `ThunderBirdSwarm.png`를 사용한다.

## 바람 정령 활공

`WindSpiritHover01–04`는 0.28초, `WindSpiritGlide01–04`는 0.19초 간격으로 순환한다. `WindSpiritDescent01–04`는 하강을 시작한 고도에서 0.08 월드 단위씩 내려갈 때 바뀐다. 곡선 띠의 두꺼운 부분이 몸통 뒤에서 흐르고 하강 중에는 꼬리 쪽 띠가 몸통에 붙는다.

12장은 206×256 RGBA, 200 PPU, 하단 중앙 피벗이다. 두 바람 정령 변형은 `WindSpiritFlightPresenter`에서 각자의 프레임과 간격을 사용한다. 제거가 시작되면 마지막 자세를 보여주고 기존 `Explode`가 이어진다.

## 화산편 걷기와 앞발 타격

`FireTadpoleIdle`은 지상 대기와 소환 통로의 자세다. `FireTadpoleWalk01–08`은 대각선의 두 발이 짚고 반대쪽 두 발이 앞으로 오가는 보행이다. 지면 이동 1.12칸마다 한 바퀴를 돈다. `FireTadpoleStomp01–04`는 피해 틱의 접촉 자세부터 0.08초씩 표시한다.

13장은 288×152 RGBA, 200 PPU, 하단 중앙 피벗이다. `FireTadpole.prefab`의 `GroundedUnitPoseController`는 소환 통로에서 지면에 닿은 뒤의 이동 거리로 보행을 재생한다. 모험의 `PveFireTadpole`은 정적 `FireTadpole.png`를 사용한다.

## 폭풍편 걷기와 전격 타격

`LightningTadpoleIdle`은 대기와 소환 통로의 자세다. `LightningTadpoleWalk01–08`은 앞발과 반대쪽 뒷발이 번갈아 지지하는 보행이며 지면 이동 1.12칸마다 반복한다. `LightningTadpoleZap01–04`는 개체별 피해 틱부터 0.08초씩 앞발 내밀기와 회수를 표시한다.

13장은 224×280 RGBA, 200 PPU, 하단 중앙 피벗이다. 전격 무늬는 청색 코어 안에서 커졌다가 잦아든다. 플레이어 프리팹의 `GroundedUnitPoseController`는 소환 통로를 벗어나 착지한 뒤 보행을 재생한다. 모험의 `PveLightningTadpole`은 정적 `LightningTadpole.png`를 사용한다.

## 전기 슬라임의 지면 이동과 타격

`ElectricSlimeIdle`은 금빛 젤 몸체와 번개 돌기가 안정된 대기 자세다. `ElectricSlimeCrawl01–08`은 바닥에 붙은 채 앞부분이 늘었다가 뒷부분이 따라오고 다시 모이는 과정이다. 지면 이동 0.88칸마다 반복한다. `ElectricSlimeBump01–04`는 피해 이벤트에서 접촉 자세로 시작해 0.08초씩 반동과 회수를 보여준다.

13장은 원본 `Game/spawn/lightning_slime`과 같은 128×111 RGBA, 100 PPU, 하단 중앙 피벗이다. `ElectricSlime.prefab`은 높이 0에 가까운 이동만 자세에 반영한다. 과충전 표시는 기존 효과에서 재생된다.

## 화염 슬라임의 지면 이동과 타격

`FireSlimeIdle`은 재 몸체와 주황색 균열을 담은 대기 자세다. `FireSlimeCrawl01–08`은 앞부분이 눌려 늘고, 뒷면이 따라와 모이는 과정으로 지면 이동 0.88칸마다 반복한다. `FireSlimeBump01–04`는 피해 틱의 접촉 자세부터 0.08초씩 반동과 회수를 보여준다.

13장은 원본 `Game/spawn/fire_slime`과 같은 128×108 RGBA, 100 PPU, 하단 중앙 피벗이다. 몸의 높이와 함께 균열이 좁아지고 주황색 꼭지는 한 자세 늦게 기운다. 불 장판은 서버가 생성한 별도 개체의 위치에서 재생된다.

## 나뭇잎 슬라임의 지면 이동과 타격

`LeafSlimeIdle`은 오른쪽을 바라보는 녹색 젤과 크기가 다른 잎 두 장을 담는다. `LeafSlimeCrawl01–08`은 젤 몸통이 먼저 눌려 늘어나고 잎이 시간차를 두고 따라오며 0.96칸마다 반복한다. `LeafSlimeBump01–04`는 접촉 피해 틱부터 0.08초씩 눌림과 복귀를 보여준다.

13장은 원본 `Game/spawn/leaf_slime`과 같은 128×108 RGBA, 100 PPU, 하단 중앙 피벗이다. 두 잎은 몸통을 따라 각기 다른 자세에서 기울어진다. 잎 장판은 생성한 지점에서 계속 재생된다.

## 바위 슬라임의 지면 이동과 타격

`RockSlimeIdle`은 갈색 바위 몸체, 붉은 균열 한 줄과 꼭대기 돌을 담는다. `RockSlimeCrawl01–08`은 앞쪽 면에 무게를 싣고 뒷면과 꼭대기 돌이 따라오며 0.96칸마다 반복한다. `RockSlimeBump01–04`는 피해 틱부터 0.08초씩 앞면의 접촉과 복귀를 보여준다. 원본과 같은 128×105 RGBA, 100 PPU, 하단 중앙 피벗을 사용한다.

## 바람 슬라임의 지면 이동과 타격

`WindSlimeIdle`은 바닥에 닿은 낮고 둥근 청록색 몸통과 짧은 바람 끝을 담는다. `WindSlimeCrawl01–08`은 둥근 두께가 뒤에서 앞으로 이어지고 꼭대기 끝이 뒤따른다. 이동 1칸마다 반복한다. `WindSlimeBump01–04`는 피해 틱부터 0.08초씩 눌림과 복귀를 표시한다. 원본과 같은 128×65 RGBA, 100 PPU, 하단 중앙 피벗을 쓴다.

## 공용 물 투사체 비행

`WaterShotFlight01–04`는 고정된 코끝과 이어지는 물결 꼬리를 가진 비행 자세다. 각 파일은 원본과 같은 192×61 RGBA, 100 PPU, 하단 중앙 피벗으로 등록한다. `Projectiles/WaterShot`은 투사체 수명에 맞춰 한 바퀴, 직접 시전의 `Prefabs/WaterShot`은 0.10초씩 반복한다.

## 바람 칼날 비행

`WindBladeFlight01–08`은 낮은 꼬리와 앞쪽 끝을 같은 자리로 등록하고, 두꺼운 부분이 굽이를 따라 앞쪽으로 지나가는 자세다. 원본과 같은 256×163 RGBA, 133.333333 PPU, 하단 중앙 피벗이다. `WindBlade` 프리팹의 지면 이동과 함께 0.12초씩 반복하며 관통 피해나 대상 수로 재생을 바꾸지 않는다.

## 화염탄 비행

`FireShotFlight01–04`는 고정된 코끝과 낮은 꼬리 안에서 주황색 열이 갑각의 뒤에서 앞으로 옮겨가는 자세다. 256×88 RGBA, 133.333333 PPU, 하단 중앙 피벗을 쓴다. 공용 `Projectiles/FireShot`은 비행 수명에 맞춰 한 번 재생한다. 직접 시전과 튜토리얼은 실제로 보이는 렌더러에서 0.10초씩 반복한다.

## 공용 전기 투사체 비행

`ElectricShotFlight01–04`는 금색 화살촉의 위치를 유지하고, 넓고 밝은 면이 꼬리에서 굴절부와 화살촉으로 옮겨가는 자세다. 192×51 RGBA, 100 PPU, 하단 중앙 피벗이다. 직접 시전의 `Prefabs/ElectricShot`은 0.10초씩 반복한다. 공용 `Projectiles/ElectricShot`은 비행 수명 동안 한 번 재생한다. `ElectricAbsorb`도 같은 공용 프리팹을 사용한다.

## 폭탄 정령의 투하 폭탄

`BombSpriteBombDescent01–06`은 같은 검은 껍질에서 도화선 불꽃이 눌렸다 펴지고 작아지는 하강 자세다. 231×256 RGBA, 400 PPU, 하단 중앙 피벗을 쓴다. `BombSpriteBomb`은 생성 후 0.11초씩 반복한다. 지상에 닿으면 서버가 별도 `BombSpriteExplosion`을 생성한다.

## 바위 강타의 비행

`BoulderStrikeFlight01–06`은 바위 앞끝과 중심을 맞추고 둥근 바람 자국 셋의 두께가 뒤에서 앞으로 이어지는 자세다. 256×146 RGBA, 100 PPU, 하단 중앙 피벗을 쓴다. `BoulderStrike` 전투 프리팹에서 0.11초씩 반복한다. 기존 바위 강타 그림은 기본 스프라이트로 쓴다.

## 드래곤 타워 불길의 비행

`DragonFlameFlight01–06`은 코끝과 얇은 꼬리를 맞추고, 둥근 불길 안의 굵고 밝은 면이 뒤에서 앞으로 흐르는 여섯 자세다. 256×88 RGBA, 133.333333 PPU, 하단 중앙 피벗이다. `DragonFlame`에서 불길마다 생성 시점부터 0.12초 간격으로 반복한다.

## 도깨비불의 이동

`WillOWispFlight01–04`는 둥근 연두색 심지 위의 불꽃 끝과 밝은 면을 바꾼 네 자세다. 256×180 RGBA, 133.333333 PPU, 하단 중앙 피벗이다. `WillOWisp` 전투 프리팹이 생성 시점부터 0.12초 간격으로 반복한다. 도깨비불이 닿으면 녹화된 진영 변경과 기존 페이드가 이어진다.

## 파도 소환의 이동

`TideCallFlight01–06`은 파도 밑단을 맞추고 두꺼운 물층이 뒤에서 올라와 둥근 물마루를 따라 앞으로 말리는 여섯 자세다. 256×193 RGBA, 133.333333 PPU, 하단 중앙 피벗이다. `TideCall` 전투 프리팹에서 0.12초 간격으로 반복한다. 녹화상 파도는 물 장판을 남기고 표적을 밀어낸 뒤에도 계속 이동한다.

## 토네이도의 회전

`TornadoStrikeSpin01–08`은 지면에 닿는 밑끝을 고정하고 상·중단 띠의 두꺼운 면이 둘레를 감아 도는 여덟 자세다. 224×256 RGBA, 133.333333 PPU, 하단 중앙 피벗이다. `TornadoStrike`에서 자세당 0.12초씩 반복한다. 기존 0.28초 생성 연출과 반투명도를 유지하면서 토네이도에 겹치던 기본 좌우 회전을 제거했다.

## 대지의 부름 발동

`EarthCallAwaken01–08`은 바닥의 황토색 고리를 맞추고 양옆에서 돌이 떠올랐다 가라앉는 여덟 자세다. 256×244 RGBA, 100 PPU, 하단 중앙 피벗이다. `EarthCall`에서 0.12초 간격으로 한 번 재생하며, 중앙을 비워 꼬마돌과 이끼바위 골렘이 보인다. 카드 그림에는 기존 `EarthCall.png`를 쓴다.

## 연쇄 번개의 비행

`ChainLightningFlight01–06`은 꺾이는 외곽과 두 청록 매듭을 맞추고, 밝은 전기가 왼쪽 꼬리에서 오른쪽 끝으로 움직이는 여섯 자세다. 256×76 RGBA, 133.333333 PPU, 하단 중앙 피벗이다. 직접 시전과 운룡의 공용 `ChainLightning`에서 0.11초 간격으로 반복한다. 잔상은 생성 순간의 자세를 받아 표시한다.
