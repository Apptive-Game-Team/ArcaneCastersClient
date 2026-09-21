# 애니메이션 프레임과 오라 에셋

Unity 런타임에서 하나의 소환수 외형을 구성하는 기본 프레임, 공격 프레임, 오라를
구분하는 정본 문서다. 숫자 접미사는 별도 캐릭터가 아니라 상태 프레임일 수 있다.

## 의미 이름

| 실제 파일 | 의미 이름 | 런타임 연결 |
|---|---|---|
| `MagmaSpirit.png` | 용암 갑각 악마 · 선 기본 자세 | `MagmaSpirit.prefab` 기본 SpriteRenderer |
| `MagmaSpiritAttacking.png` | 용암 갑각 악마 · 내려찍기 자세 | `OnAttackSpriteSwapper.onAttackSprite` |
| `MagmaSpiritSpawn.png` | 용암 갑각 악마 · 지면 돌파 소환 자세 | 생성 시 0.28초 표시 |
| `MagmaExplosion.png` | 마그마 폭발 · 기본/초기 갑각 자세 | `MagmaExplosion.prefab` 기본 SpriteRenderer |
| `MagmaExplosionStrike1.png`–`MagmaExplosionStrike4.png` | 마그마 폭발 · 균열 시작 → 개방 → 피크 → 냉각 | `SpriteFrameAnimator`, 0.1초 간격, 비반복 |
| `RockGolem.png` | 이끼바위 골렘 · 기본 자세 | `RockGolem.prefab` 기본 SpriteRenderer |
| `RockGolem2.png` | 이끼바위 골렘 · 공격 자세 | `OnAttackSpriteSwapper.onAttackSprite` |
| `TreeGolem.png` | 고목 수호자 · 기본 자세 | `TreeGolem.prefab` 기본 SpriteRenderer |
| `TreeGolem2.png` | 고목 수호자 · 공격 자세 | `OnAttackSpriteSwapper.onAttackSprite` |
| `EvilEnt.png` | 사악한 고목 · 기본 자세 | `EvilEnt.prefab` 기본 SpriteRenderer |
| `EvilEnt2.png` | 사악한 고목 · 팔을 뻗은 공격 자세 | `AttackSpriteSwapController.swapSprite`, 공격 이벤트에서 0.1초 표시 |
| `PlayerCharacterBase.png` | 수습 마법생 · 지팡이를 세운 기본 자세 | `Player.prefab` 의 `PlayerImage` SpriteRenderer |
| `PlayerCharacterAttack.png` | 수습 마법생 · 지팡이를 앞으로 뻗은 공격 자세 | `AttackSpriteSwapController.swapSprite`, 공격 이벤트에서 0.3초 표시 |
| `AquaArcher.png` | 물결 궁수 · 활시위를 당긴 기본 자세 | `AquaArcherAttackPresenter` 기본 Sprite |
| `AquaArcherAttack.png` | 물결 궁수 · 시위를 놓은 공격 자세 | 공격 이벤트에서 0.08초 표시 |
| `RockTurret.png` | 인간제 투석 포탑 · 장전 자세 | `RockTurret.prefab` 기본 SpriteRenderer |
| `RockTurretAttacking.png` | 인간제 투석 포탑 · 발사 직후 자세 | `AttackSpriteSwapController.swapSprite`, 공격 이벤트에서 0.1초 표시 |
| `WaterSlime.png` | 물 슬라임 · 한 개체 기본 자세 | `WaterSlime.prefab` 기본 SpriteRenderer |
| `WaterSlimeAttackSpit.png` | 물 슬라임 · 물 뱉기 자세 | `AttackSpriteSwapController.swapSprite`, 공격 이벤트에서 0.15초 표시 |

실제 파일명과 `.meta` GUID는 런타임 참조 때문에 유지한다. 문서와 홈페이지에서는
숫자 접미사 대신 의미 이름을 표시한다.

## 공격 프레임 규칙

`OnAttackSpriteSwapper`는 공격 이벤트에서 공격 프레임으로 바꾸고 기본값 기준
0.2초 뒤 원래 프레임으로 복귀한다. Transform 보정 없이 Sprite만 바꾸므로 두
이미지가 아래 조건을 만족해야 한다.

`AquaArcherAttackPresenter`는 평상시에 당긴 프레임을 유지하다 공격 이벤트에서
놓은 프레임으로 즉시 바꾸고 0.08초 뒤 복귀한다. 위치 튐을 막기 위해 별도
Transform 스케일 변형은 적용하지 않는다.

`MagmaSpiritSpawnPresenter`는 일반 소환 연출을 재생하는 생성에서만 지면 돌파
프레임을 0.28초 표시한 뒤 선 기본 자세로 복귀한다. 동기화 복구처럼
`playSpawnPresentation`이 꺼진 생성에서는 소환 프레임을 건너뛴다.

`RockTurret`은 공용 `AbstractBuild`의 `AttackSpriteSwapController`를 사용한다.
다른 건물 프리팹은 `swapSprite`가 비어 있어 공격 이벤트를 구독하지 않으며,
`RockTurret`만 발사 직후 프레임을 0.1초 표시한다.

`EvilEnt`는 공용 `AbstractRangeMob`의 `AttackSpriteSwapController`를 사용한다.
`TreeGolem`이 쓰는 레거시 `OnAttackSpriteSwapper`가 아니다. `EvilEnt.prefab`은
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
`TreeGolem`과 `TreeGolem2`는 PPU가 서로 달라 재작업 전에 교정이 필요하다.
`TreeGolem`은 240x256에 PPU 100, `TreeGolem2`는 256x215에 PPU 80이다.

`EvilEnt`와 `EvilEnt2`는 224x256, PPU 100, Bottom Center로 일치한다. 두 프레임을
프레임마다 따로 자르지 않고 하나의 공유 크롭 박스와 하나의 공유 배율로 함께
내보냈기 때문에, 지면 접점 픽셀이 사후 대조가 아니라 제작 방식으로 이미 동일하다.
`TreeGolem` 쌍이 어긋난 원인이 프레임별 개별 크롭·개별 배율이므로, 새 공격
프레임은 이 방식으로 만든다. 캔버스 크기가 같아지는 것은 결과이지 목표가 아니다.

`WaterSlime`과 `WaterSlimeAttackSpit`은 320x224, PPU 200, Bottom Center로
일치한다. 두 프레임은 하나의 공유 소스 픽셀 배율과 하단 기준선을 사용하며,
공격 프레임 오른쪽의 추가 폭은 물 투사체를 위한 투명 캔버스다.

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
| `Effect/AreaOfEffect/RepairAura.png` | repair_totem · 수리 범위 경계 | `RepairTotem.prefab` 자식 SpriteRenderer, `AuraRadiusScaler`가 크기 결정 |

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
그래도 같은 투사체로 읽혀야 하므로 `256x256`, PPU 100, 중앙 피벗, 실루엣,
배율과 캔버스 배치를 공유한다. 지상형은 눈 내부 색만 밝게 바꾸며 몸, 지느러미,
꼬리, 입과 알파는 공중형과 픽셀 단위로 동일하게 유지한다.

## Cloud Dragon 프레임

| 실제 파일 | 의미 이름 | 프리팹 연결 |
|---|---|---|
| `CloudDragon.png` | 운룡 · 기본 자세 · 입에 물 없음 | `CloudDragon.prefab` 기본 SpriteRenderer |
| `CloudDragonAttacking.png` | 운룡 · 물 분사 공격 자세 | `AttackSpriteSwapController.swapSprite` |

두 프레임은 PPU 116.363636, Bottom Center 피벗, 몸통 배율·바닥선을 공유한다.
`CloudDragon.png`는 `256x182`다. `CloudDragonAttacking.png`는 `540x182`로, 몸통은
`CloudDragon.png`와 캔버스 폭이 다르지만 `AttackSpriteSwapController`가 Sprite만
바꾸고 Transform은 건드리지 않으므로, 몸통은 두 캔버스의 가로 중심(각각 폭의
50%, Bottom Center 피벗 기준)에서 같은 오프셋에 오도록 배치했다 — 캔버스 폭이
다르면 중심 자체가 다른 픽셀 위치를 가리키므로, 몸통을 두 프레임에서 같은
픽셀 오프셋에 두면 오히려 world 위치가 어긋난다. 폭이 늘어난 만큼은 전부
물줄기 몫이고, 몸통 크기·바닥선·세로 위치는 `CloudDragon.png`와 동일하다.
공격 이벤트가 발생하면 `CloudDragonAttacking.png`를 0.1초간 표시한 뒤 기본
자세로 복원한다. 구형 물 아우라 `cloud.png`는 두 본체 프레임과 계속 분리해서
렌더링한다.

`check-frame-pair.py`의 가로 차이 값은 이 쌍에서 크게 나온다(약 0.96 unit,
한도 0.05) — 이 스크립트는 캔버스 하단 10% 띠의 가로 중심을 "발" 위치로 보는데,
그 띠 안에 굵은 물줄기가 넓게 걸쳐 있어 중심이 물 쪽으로 크게 쏠린다. 실제
몸통 정렬은 정상이다: 두 프레임을 각자의 pivot(각 캔버스 폭의 50%) 기준으로
겹쳐 보면 날개·뿔·꼬리·등뼈 돌기가 픽셀 단위로 일치한다(세로 차이는 0.000으로
그대로 통과). 이 스크립트는 좁고 완전히 대칭인 프레임 쌍을 가정하고 만들어져
있어, 캔버스 폭이 다른 프레임에서 굵은 투사체가 바닥 근처를 지나가면 이렇게
false positive를 낸다 — 숫자만 보지 말고 pivot 기준으로 겹친 그림을 반드시
확인한다.

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

프레임은 생성물이 아니라 결정적 합성 결과다. 원본 두 장
(`.art/concept/lightning-strike/`)에서 다시 만들 수 있다. 검수 시트는
`.art/sheets/lightning-strike-review.png`.

```bash
.art/tools/compose-lightning-strike.py \
  --cloud .art/concept/lightning-strike/storm_cloud_source.png \
  --bolt .art/concept/lightning-strike/lightning_bolt_source.png \
  --idle-out Assets/Resources/Game/sprites/LightningCloud.png \
  --frame-out 'Assets/Resources/Game/sprites/LightningCloudStrike{index}.png'
```

## 폭발 이펙트 프레임 쌍 (`razor_gale`, `electric_explode`)

`Assets/Resources/Game/explode/` 의 `razor_gale_frame_0/1` (192x155) 과
`electric_explode_frame_0/1` (192x116) 은 각각 `SpriteFrameAnimator` 가 0.12초
간격에 `loop: 1` 로 돌리는 두 프레임이다. 두 장의 캔버스 크기가 같아야 하고,
`spritePixelsToUnits` 는 둘 다 100 이다.

### `.meta` 의 `spritePivot` 을 믿지 마라. `alignment` 가 이긴다

네 파일 모두 `spritePivot: {x: 0.5, y: 0.5}` 로 적혀 있지만 `alignment: 7` 이다.
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
