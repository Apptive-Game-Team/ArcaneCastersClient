# 전투 모션 연결과 검수 목록

88종의 연결된 클립과 현재 서버 기록을 대조했다. 각 칸은 실제 프리팹과 미리보기에 연결된 스프라이트 슬롯 수다. 홀드와 기존 그림의 반복을 포함한다. 개별 재생 시간은 1배속 미리보기에서 확인한다.

Unity에서 132개 전투 리소스의 스크립트, 프레임 배열과 직렬화 참조를 검사했다. 피격·퇴장 검수는 기존 그룹별 기록을 사용한다. 서버 행동 열은 시험 파라미터와 수동으로 배치한 지상·공중 대상에 대해 production simulation을 실행한 6.50초 기록이다. 이 열은 관측된 행동을 표시하며 기록 밖의 패턴은 별도 검수 대상이다.

| 유닛 | 연결된 동작과 칸 수 | 서버에서 관측한 신호·생성물 | Unity 검수 근거 |
|---|---|---|---|
| 천둥 새 `ThunderBird` | 날갯짓 6<br>급강하 공격 8<br>피격 · 회복 3<br>추락 · 착지 · 소멸 30 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 번개 정령 `ThunderSpirit` | 부유 6<br>방전 7<br>피격 · 회복 3<br>추락 · 착지 · 소멸 30 | `이동` · `Attack` · `ElectricShot` | 프리팹 참조 · 피격·퇴장 재생 |
| 전기 창병 `ElectricSpearman` | 부유 이동 8<br>찌르기 6<br>피격 · 회복 3<br>추락 · 착지 · 소멸 30 | 별도 서버 타입 없음 | 프리팹 참조 · 피격·퇴장 재생 |
| 전기 슬라임 `ElectricSlime` | 이동 6<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 찌릿쥐 `ZapMouse` | 달리기 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 폭풍 사슴 `StormStag` | 돌진 · 공포 후퇴 8<br>달리기 → 충돌 → 회복 11<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `이동` · `ElectricShot` | 프리팹 참조 · 피격·퇴장 재생 |
| 폭풍편 `LightningTadpole` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 물의 궁수 `AquaArcher` | 이동 6<br>활 발사 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` · `WaterShot` | 프리팹 참조 · 피격·퇴장 재생 |
| 비전 보병 `ArcaneInfantry` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 콩 사수 `BeanShooter` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` · `Bean` | 프리팹 참조 · 피격·퇴장 재생 |
| 폭탄 정령 `BombSprite` | 비행 6<br>폭탄 투하 6<br>피격 · 회복 3<br>추락 · 착지 · 소멸 30 | `이동` · `Attack` · `BombSpriteBomb` · `BombSpriteExplosion` | 프리팹 참조 · 피격·퇴장 재생 |
| 거품 정령 `BubbleSpirit` | 부유 이동 8<br>공중 거품 발사 6<br>피격 · 회복 3<br>추락 · 착지 · 소멸 30 | `이동` · `Attack` · `BubbleShot` | 프리팹 참조 · 피격·퇴장 재생 · 새 부유 이동 1배속 |
| 치킨 특공대 `ChickenCommando` | 이동 6<br>공격 5<br>낙하산 강하 · 착지 13<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 운룡 `CloudDragon` | 비행 6<br>공격 6<br>피격 · 회복 3<br>추락 · 착지 · 소멸 31 | `이동` · `Attack` · `WaterShot` · `ChainLightning` | 프리팹 참조 · 피격·퇴장 재생 |
| 경계 운반자 `DimensionToad` | 이동 8<br>차원문 소환 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `SummonRelease` · `FireTadpole` · `LightningTadpole` | 프리팹 참조 · 피격·퇴장 재생 |
| 불씨 정령 `EmberSpirit` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` · `FireField` | 프리팹 참조 · 피격·퇴장 재생 |
| 사악한 나무 거인 `EvilEnt` | 이동 8<br>주먹 뻗기 6<br>붙잡아 끌어오기 8<br>불주먹 7<br>피격 · 회복 3<br>쓰러짐 · 소멸 8 | `Attack` · `EvilEntGrabArm` · `EvilEntFireFist` | 프리팹 참조 · 피격·퇴장 재생 |
| 하급 불 정령 `FireChildSpirit` | 비행 6<br>공격 6<br>피격 · 회복 3<br>추락 · 착지 · 소멸 30 | `이동` · `Attack` · `FireShot` | 프리팹 참조 · 피격·퇴장 재생 |
| 불의 군주 `FireLordSpirit` | 비행 8<br>하급 악마 소환 6<br>피격 · 회복 3<br>추락 · 착지 · 소멸 31 | `SummonRelease` · `FireChildSpirit` | 프리팹 참조 · 피격·퇴장 재생 |
| 불 슬라임 `FireSlime` | 이동 6<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` · `FireField` | 프리팹 참조 · 피격·퇴장 재생 |
| 불 정령 `FireSpirit` | 이동 8<br>불꽃 분사 10<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `SprayRelease` | 프리팹 참조 · 피격·퇴장 재생 |
| 화산편 `FireTadpole` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 잎 슬라임 `LeafSlime` | 이동 6<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` · `LeafField` | 프리팹 참조 · 피격·퇴장 재생 |
| 용암 갑각 악마 `MagmaSpirit` | 이동 8<br>공격 6<br>소환 1<br>피격 · 회복 3<br>쓰러짐 · 잔해 생성 6 | `이동` · `Attack` · `MagmaFist` | 프리팹 참조 · 피격·퇴장 재생 · 주먹 생성·본체 타격 6회 |
| 꼬마 돌 `MiniRock` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 벽돌 골렘 `PveBrickGolem` | 전방 휩쓸기 4<br>불씨 정령 무리 소환 6<br>타격 · 회복 5<br>내려찍기 예고 → 타격 17<br>피격 · 회복 3<br>쓰러짐 · 소멸 8 | `Attack` · `BrickGolemSlamWarning` · `BrickGolemSweepPose` | 프리팹 참조 · 피격·퇴장 재생 |
| 타락한 덩굴 정령 `PveCorruptedVineSpirit` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 타락한 바람 정령 `PveCorruptedWindSpirit` | 비행 6<br>공중 폭발 6<br>피격 · 회복 3 | `이동` · `WindSpiritBurst` | 프리팹 참조 · 피격·퇴장 재생 |
| 모험 경계 운반자 `PveDimensionToad` | 이동 8<br>차원문 소환 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `SummonRelease` · `PveFireTadpole` · `PveLightningTadpole` | 프리팹 참조 · 피격·퇴장 재생 |
| 사악한 나무 거인 `PveEvilEnt` | 주먹 뻗기 6<br>붙잡아 끌어오기 8<br>불주먹 7<br>피격 · 회복 3<br>쓰러짐 · 소멸 8 | `Attack` · `EvilEntGrabArm` · `EvilEntFireFist` | 프리팹 참조 · 피격·퇴장 재생 |
| 화산편 `PveFireTadpole` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 폭풍편 `PveLightningTadpole` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 잎 슬라임 둥지 `PveNatureSlimeNest` | 잎 슬라임 소환 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | 이 기록에서 발동 없음 | 프리팹 참조 · 피격·퇴장 재생 |
| 모험 덩굴 군락 `PveVineColony` | 무리 소환 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | 이 기록에서 발동 없음 | 프리팹 참조 · 피격·퇴장 재생 |
| 덩굴 마녀 `PveVineWitch` | 이동 8<br>일반 시전 6<br>분노 발동 8<br>연속 덩굴 시전 5<br>연속 덩굴 네 번 24<br>무리 소환 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 8 | 이 기록에서 발동 없음 | 프리팹 참조 · 피격·퇴장 재생 |
| 물 슬라임 둥지 `PveWaterSlimeNest` | 물 슬라임 소환 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | 이 기록에서 발동 없음 | 프리팹 참조 · 피격·퇴장 재생 |
| 바위 골렘 `RockGolem` | 이동 8<br>주먹 타격 8<br>피격 · 회복 3<br>쓰러짐 · 잔해 생성 6 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 돌 마법사 `RockMage` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` · `RockShot` | 프리팹 참조 · 피격·퇴장 재생 |
| 바위 슬라임 `RockSlime` | 이동 6<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 바다뱀 `SeaSerpent` | 유영 8<br>물줄기 분사 7<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `이동` · `Attack` · `SprayRelease` | 프리팹 참조 · 피격·퇴장 재생 |
| 씨앗 둥지 `SeedNest` | 씨앗 정령 소환 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `SummonRelease` · `SeedSpirit` · `LeafField` | 프리팹 참조 · 피격·퇴장 재생 |
| 씨앗 정령 `SeedSpirit` | 이동 6<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` · `LeafField` | 프리팹 참조 · 피격·퇴장 재생 |
| 돌 가고일 `StoneGargoyle` | 비행 4<br>피격 · 회복 3<br>추락 · 착지 · 소멸 30 | 별도 서버 타입 없음 | 프리팹 참조 · 피격·퇴장 재생 |
| 폭풍 기수 `StormRider` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 포탑등이 `Towerback` | 이동 8<br>지상 타격 6<br>대공 포격 4<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `이동` · `Attack` · `RockShot` | 프리팹 참조 · 피격·퇴장 재생 |
| 나무 골렘 `TreeGolem` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `이동` · `Attack` · `LeafField` | 프리팹 참조 · 피격·퇴장 재생 |
| 덩굴 군락 `VineColony` | 적 발밑 덩굴 소환 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `SummonRelease` · `Vine` | 프리팹 참조 · 피격·퇴장 재생 |
| 덩굴 정령 `VineSpirit` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` · `NatureShot` | 프리팹 참조 · 피격·퇴장 재생 |
| 방벽 골렘 `WallGolem` | 이동 8<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 잔해 생성 6 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 물 슬라임 `WaterSlime` | 이동 6<br>공격 5<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` · `WaterShot` | 프리팹 참조 · 피격·퇴장 재생 |
| 바람 슬라임 `WindSlime` | 이동 6<br>공격 6<br>피격 · 회복 3<br>쓰러짐 · 소멸 5 | `이동` · `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 바람 정령 `WindSpirit` | 비행 6<br>공중 폭발 6<br>피격 · 회복 3 | `이동` · `WindSpiritBurst` | 프리팹 참조 · 피격·퇴장 재생 |
| 불 소환소 `FireSummon` | 불 슬라임 소환 6 | `SummonRelease` · `FireSlime` | 프리팹 참조 · SeedNest 공유 본체·생성 기록 |
| 돌 소환소 `RockSummon` | 바위 슬라임 소환 6 | `SummonRelease` · `RockSlime` | 프리팹 참조 · SeedNest 공유 본체·생성 기록 |
| 전기 소환소 `ElectricSummon` | 전기 슬라임 소환 6 | `SummonRelease` · `ElectricSlime` | 프리팹 참조 · SeedNest 공유 본체·생성 기록 |
| 바람 소환소 `WindSummon` | 바람 슬라임 소환 6 | `SummonRelease` · `WindSlime` | 프리팹 참조 · SeedNest 공유 본체·생성 기록 |
| 수습 마법생 `Player` | 지팡이 들기 3<br>지팡이 시전 6 | 조준·시전 입력 | 프리팹 참조 · 조준·시전 상태 전환 |
| 분화구 운반수 `CraterCarrier` | 박격포 운반 8<br>설치 전환 6<br>포격 · 탄착 2<br>회수 전환 6<br>운반 중 피격 3<br>운반 중 파괴 6<br>설치 후 피격 3<br>설치 후 파괴 6 | `이동` · `Attack` · `CraterShell` · `CraterExplosion` | 프리팹 참조 · 운반·설치·회수 재생 |
| 거품 생성기 `BubbleGenerator` | 보호 거품 방출 4<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | 이 기록에서 발동 없음 | 프리팹 참조 · 피격·퇴장 재생 |
| 생명의 나무 `LifeTree` | 생명력 방출 4<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 치유 토템 `HealingTotem` | 치유 발동 4<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `Attack` | 프리팹 참조 · 피격·퇴장 재생 |
| 거신의 잔해 `TitanRemnant` | 거신 주먹 소환 4<br>피격 · 회복 3<br>쓰러짐 · 잔해 생성 6 | `Attack` · `TitanFist` | 프리팹 참조 · 피격·퇴장 재생 |
| 지상 대포 `GroundCannon` | 발사 4<br>피격 · 회복 3<br>쓰러짐 · 잔해 생성 6 | `Attack` · `RockShot` | 프리팹 참조 · 피격·퇴장 재생 |
| 지상 탑 `GroundTower` | 발사 4<br>피격 · 회복 3<br>쓰러짐 · 잔해 생성 6 | `Attack` · `RockShot` | 프리팹 참조 · 피격·퇴장 재생 |
| 전기 탑 `ElectricTower` | 발사 4<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `Attack` · `ElectricShot` | 프리팹 참조 · 피격·퇴장 재생 |
| 바위 포탑 `RockTurret` | 돌 투척 4<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `Attack` · `RockShot` | 프리팹 참조 · 피격·퇴장 재생 |
| 용 탑 `DragonTower` | 발사 4<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `Attack` · `DragonFlame` | 프리팹 참조 · 피격·퇴장 재생 |
| 불꽃 탑 `FireworkTower` | 발사 4<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `Attack` · `FireworkShell` | 프리팹 참조 · 피격·퇴장 재생 |
| 번개 구름 `LightningCloud` | 낙뢰 6<br>구름 흩어짐 5 | `Attack` · `ElectricField` | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 마나 우물 `ManaWell` | 마나 공급 유지 10<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | 이 기록에서 발동 없음 | 프리팹 참조 · 피격·퇴장 재생 |
| 집결 토템 `RallyingTotem` | 집결 효과 유지 10<br>불씨 꺼짐 · 소멸 5 | 이 기록에서 발동 없음 | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 수리 토템 `RepairTotem` | 설치물 지속 시간 유지 10<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | 이 기록에서 발동 없음 | 프리팹 참조 · 피격·퇴장 재생 |
| 풀 생성기 `GrassGenerator` | 잎밭 생성 5<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `GrassGeneratorBloom` · `LeafField` | 프리팹 참조 · 피격·퇴장 재생 |
| 바람 토템 `WindTotem` | 밀어내기 5<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `WindTotemPulse` | 프리팹 참조 · 피격·퇴장 재생 |
| 분화구 `Crater` | 용암 분출 5<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | `CraterEruption` · `CraterEmber` · `FireField` | 프리팹 참조 · 피격·퇴장 재생 |
| 불 룬 `FireRune` | 충돌 · 룬 폭발 5 | `FireRuneBurst` | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 물 룬 `WaterRune` | 충돌 · 룬 폭발 5 | `WaterRuneBurst` | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 자연 룬 `NatureRune` | 충돌 · 룬 폭발 5 | `NatureRuneBurst` | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 바위 룬 `RockRune` | 충돌 · 룬 폭발 5 | `RockRuneBurst` | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 번개 룬 `LightningRune` | 충돌 · 룬 폭발 5 | `LightningRuneBurst` | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 바람 룬 `WindRune` | 충돌 · 룬 폭발 5 | `WindRuneBurst` | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 광란 토템 `FrenzyTotem` | 착지 · 광란 발동 5<br>낙하부터 광란 발동까지 35 | `FrenzyTotemBurst` | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 감전 함정 `ShockTrap` | 함정 발동 · 방전 1<br>피격 · 회복 3<br>쓰러짐 · 소멸 6 | 이 기록에서 발동 없음 | 프리팹 참조 · 피격·퇴장 재생 |
| 덩굴 `Vine` | 덩굴 솟기 4<br>속박 종료 · 퇴장 4 | 이 기록에서 발동 없음 | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 거대 덩굴 `GiantVine` | 덩굴 솟기 4<br>속박 종료 · 퇴장 4 | 이 기록에서 발동 없음 | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 작은 돌 잔해 `RockRemnant` | 잔해 생성 3<br>수명 만료 · 붕괴 5 | 이 기록에서 발동 없음 | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 중간 돌 잔해 `MediumRockRemnant` | 잔해 생성 3<br>수명 만료 · 붕괴 5 | 이 기록에서 발동 없음 | 프리팹 참조 · 수명·발동 퇴장 재생 |
| 과성장 `Overgrowth` | 성장 · 발동 · 잎 흩어짐 9 | `OvergrowthRelease` · `SeedSpirit` · `LeafField` | 프리팹 참조 · 수명·발동 퇴장 재생 |

전기 창병과 돌 가고일은 현재 Rust 프리팹 타입에 없다. Unity의 독립 입력 검수와 미리보기로 프레임을 확인했으며, 추락 높이는 번개 정령 서버 기록을 공유한다. 네 속성 소환소는 SeedNest 본체를 공유한다.

12.50초 추가 기록에서 모험 슬라임 둥지 두 종은 9.95초에 중앙에 한 마리씩, 모험 덩굴 군락은 같은 틱에 양쪽으로 두 마리를 소환했다. 덩굴 마녀는 7.95초에 양쪽으로 두 마리를 소환했다. 네 종류 모두 소환 신호와 실제 생성 위치가 일치했다. 거품 생성기는 아군을 배치했을 때 0.75초에 발사·공격 신호와 아군의 보호막 적용을 보냈다. 20.50초 추가 기록에서는 덩굴 마녀의 일반 시전 한 번, 분노 발동과 연속 시전 네 번을 Unity에서 재생했다.

## 본체 목록 외의 서버 타입

50개 타입은 마법의 투사체, 탄착 효과, 장판과 지형이다. 아래 프리팹은 같은 132개 리소스 검사에 포함했다. 해당 유닛이 생성하는 효과는 위 서버 행동 열에도 표시했다.

| 서버 타입 | 표시 리소스와 확인 범위 |
|---|---|
| `FireShot` | [프리팹](../../Assets/Resources/Prefabs/FireShot.prefab) 스크립트·프레임 참조 |
| `FireExplode` | [프리팹](../../Assets/Resources/Prefabs/FireExplode.prefab) 스크립트·프레임 참조 |
| `FireField` | [프리팹](../../Assets/Resources/Prefabs/FireField.prefab) 스크립트·프레임 참조 |
| `WaterShot` | [프리팹](../../Assets/Resources/Prefabs/WaterShot.prefab) 스크립트·프레임 참조 |
| `WaterExplode` | [프리팹](../../Assets/Resources/Prefabs/WaterExplode.prefab) 스크립트·프레임 참조 |
| `WaterField` | [프리팹](../../Assets/Resources/Prefabs/WaterField.prefab) 스크립트·프레임 참조 |
| `TidalWarhead` | [프리팹](../../Assets/Resources/Prefabs/TidalWarhead.prefab) 스크립트·프레임 참조 |
| `GroundTidalWarhead` | [프리팹](../../Assets/Resources/Prefabs/GroundTidalWarhead.prefab) 스크립트·프레임 참조 |
| `TidalWarheadExplosion` | [프리팹](../../Assets/Resources/Prefabs/TidalWarheadExplosion.prefab) 스크립트·프레임 참조 |
| `RockRolling` | [프리팹](../../Assets/Resources/Prefabs/RockRolling.prefab) 스크립트·프레임 참조 |
| `RockExplode` | [프리팹](../../Assets/Resources/Prefabs/RockExplode.prefab) 스크립트·프레임 참조 |
| `BoulderStrike` | [프리팹](../../Assets/Resources/Prefabs/BoulderStrike.prefab) 스크립트·프레임 참조 |
| `ElectricShot` | [프리팹](../../Assets/Resources/Prefabs/ElectricShot.prefab) 스크립트·프레임 참조 |
| `ElectricExplode` | [프리팹](../../Assets/Resources/Prefabs/ElectricExplode.prefab) 스크립트·프레임 참조 |
| `ElectricField` | [프리팹](../../Assets/Resources/Prefabs/ElectricField.prefab) 스크립트·프레임 참조 |
| `VineToss` | 서버에서 덩굴 줄을 생성하고 즉시 제거하는 중간 객체. 현재 마법은 VineLine으로 덩굴을 생성한다. |
| `LeafExplode` | [프리팹](../../Assets/Resources/Prefabs/LeafExplode.prefab) 스크립트·프레임 참조 |
| `LeafField` | [프리팹](../../Assets/Resources/Prefabs/LeafField.prefab) 스크립트·프레임 참조 |
| `WindBlade` | [프리팹](../../Assets/Resources/Prefabs/WindBlade.prefab) 스크립트·프레임 참조 |
| `WindExplode` | [프리팹](../../Assets/Resources/Prefabs/WindExplode.prefab) 스크립트·프레임 참조 |
| `DragonFlame` | [프리팹](../../Assets/Resources/Prefabs/DragonFlame.prefab) 스크립트·프레임 참조 |
| `SandStorm` | [프리팹](../../Assets/Resources/Prefabs/SandStorm.prefab) 스크립트·프레임 참조 |
| `ChainLightning` | [프리팹](../../Assets/Resources/Prefabs/ChainLightning.prefab) 스크립트·프레임 참조 |
| `TornadoStrike` | [프리팹](../../Assets/Resources/Prefabs/TornadoStrike.prefab) 스크립트·프레임 참조 |
| `MeteorShower` | [프리팹](../../Assets/Resources/Prefabs/MeteorShower.prefab) 스크립트·프레임 참조 |
| `TideCall` | [프리팹](../../Assets/Resources/Prefabs/TideCall.prefab) 스크립트·프레임 참조 |
| `FireDrop` | [프리팹](../../Assets/Resources/Prefabs/FireDrop.prefab) 스크립트·프레임 참조 |
| `NatureDrop` | [프리팹](../../Assets/Resources/Prefabs/NatureDrop.prefab) 스크립트·프레임 참조 |
| `RockDrop` | [프리팹](../../Assets/Resources/Prefabs/RockDrop.prefab) 스크립트·프레임 참조 |
| `WindDrop` | [프리팹](../../Assets/Resources/Prefabs/WindDrop.prefab) 스크립트·프레임 참조 |
| `MagmaFist` | [프리팹](../../Assets/Resources/Prefabs/MagmaFist.prefab) 스크립트·프레임 참조 |
| `MeteorDrop` | [프리팹](../../Assets/Resources/Prefabs/MeteorDrop.prefab) 스크립트·프레임 참조 |
| `CraterEmber` | [프리팹](../../Assets/Resources/Prefabs/CraterEmber.prefab) 스크립트·프레임 참조 |
| `TitanFist` | [프리팹](../../Assets/Resources/Prefabs/TitanFist.prefab) 스크립트·프레임 참조 |
| `FireworkShell` | [프리팹](../../Assets/Resources/Prefabs/FireworkShell.prefab) 스크립트·프레임 참조 |
| `BombSpriteBomb` | [프리팹](../../Assets/Resources/Prefabs/BombSpriteBomb.prefab) 스크립트·프레임 참조 |
| `BombSpriteExplosion` | [프리팹](../../Assets/Resources/Prefabs/BombSpriteExplosion.prefab) 스크립트·프레임 참조 |
| `Bean` | [프리팹](../../Assets/Resources/Prefabs/Bean.prefab) 스크립트·프레임 참조 |
| `EarthCall` | [프리팹](../../Assets/Resources/Prefabs/EarthCall.prefab) 스크립트·프레임 참조 |
| `MagmaExplosion` | [프리팹](../../Assets/Resources/Prefabs/MagmaExplosion.prefab) 스크립트·프레임 참조 |
| `WaterExplosion` | [프리팹](../../Assets/Resources/Prefabs/WaterExplosion.prefab) 스크립트·프레임 참조 |
| `RazorGale` | [프리팹](../../Assets/Resources/Prefabs/RazorGale.prefab) 스크립트·프레임 참조 |
| `ShockOverload` | [프리팹](../../Assets/Resources/Prefabs/ShockOverload.prefab) 스크립트·프레임 참조 |
| `WillOWisp` | [프리팹](../../Assets/Resources/Prefabs/WillOWisp.prefab) 스크립트·프레임 참조 |
| `Leafair` | [프리팹](../../Assets/Resources/Prefabs/Leafair.prefab) 스크립트·프레임 참조 |
| `Wall` | 경기장 경계 충돌체 |
| `RiverWater` | [프리팹](../../Assets/Resources/Prefabs/RiverWater.prefab) 스크립트·프레임 참조 |
| `RiverBridge` | [프리팹](../../Assets/Resources/Prefabs/RiverBridge.prefab) 스크립트·프레임 참조 |
| `CraterShell` | [프리팹](../../Assets/Resources/Prefabs/CraterShell.prefab) 스크립트·프레임 참조 |
| `CraterExplosion` | [프리팹](../../Assets/Resources/Prefabs/CraterExplosion.prefab) 스크립트·프레임 참조 |

원본 프레임과 리소스 검사 결과는 .generated/combat-full-audit/에, 그룹별 Unity 재생 기록은 각 combat-*-reactions 폴더와 기존 *-unity.log에 보관한다. 주요 신호별 설명은 [전투 동작 검수](combat-cue-review.md)에 기록한다.
