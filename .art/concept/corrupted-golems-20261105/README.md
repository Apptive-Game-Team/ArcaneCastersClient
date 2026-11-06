# 돌 골렘 부족의 타락 외형 적용

현재 적용본은 [v5 검은 갑각 잠식](v5/README.md)이다. 아래의 눈만 변경한 내용과
`finalize.py`, `validation.json`은 v1 기록이며 현재 적용본 검증으로 사용하지 않는다.
v1 재현 스크립트를 실행하면 현재 게임 파일을 과거 외형으로 되돌리므로 주의한다.

사용자가 게임 안 돌 골렘 부족 유닛 전체의 타락 외형 적용을 요청했다.
코믹스의 황갈색 돌 몸체와 초록 이끼는 그대로 두고, 타원 눈만 붉게 빛나도록 한다.

- 참고: website 저장소의 `content/arcane-casters/story/episodes/002/art-v4/page-03-diverse.png`,
  `episodes/006/art-revised-v3/page-12.png`. 만화 그림체는 게임에 옮기지 않는다.
- 도구: built-in `image_gen`, 투명 배경 편집. `prompts/`에 편집 지시를 기록했다.
- `eyes/`는 생성 결과에서 선택한 두 눈의 원본 crop이다. 몸체의 재생성 픽셀은 사용하지 않았다.
- `finalize.py`는 선택한 눈을 원본의 눈 마스크에만 맞춰 합성한다.
  입력 기준은 작업 시작 커밋 `38840beb`로 고정한다.
- `validation.json`은 각 이미지의 캔버스·alpha bbox·변경 픽셀 수와 보존 검증 결과다.
- `generated/`의 전체 생성물과 `review/`의 전체 스프라이트 시트는 로컬 검토용이다.
  최종 PNG는 `Assets/Resources/Game/sprites/`, 비교는 `docs/pr-media/423/`에 저장한다.

`MiniRock.prefab`은 `MiniRockSwarm.png`를 사용한다. `RockGolem.prefab`은
`RockGolem.png`와 `RockGolem2.png`를 기본·공격 프레임으로 사용한다.
`RockMage`와 `WallGolem`은 각 이름의 PNG를 사용하며 별도 공격 Sprite override가 없다.
상위 `AbstractMeleeMob`와 `AbstractRangeMob`의 `swapSprite` 기본값은 비어 있다.
따라서 이번 범위는 네 유닛, 다섯 이미지다. 인간 진영의 `Towerback`, `PveBrickGolem`,
생명이 없는 바위 투사체·잔해·구조물은 이번 대상에 포함하지 않는다.

기존 `.meta` GUID, PPU, 피벗, 프레임 배선, 능력치와 이름은 변경하지 않는다.
64px 축소와 전후 비교는 확인했다. Unity Editor에서 실제 플레이 검증은 별도다.
