# 비전 보병
- 서버 키: `arcane_infantry`
- 시전: 소환 (`ArcaneInfantryMagic` → `ArcaneInfantry`)
- 진영: 인간 마법 문명
- 원소: 무속성
- 역할: 지상 단일 대상 근접 공격

양손 전완 장갑의 짧은 푸른 마력 칼날로 싸우는 기본 보병.
넓은 어깨, 청회색 흉갑과 청동 테두리를 사용하며 얼굴과 갈색 머리를 드러낸다.
2026-10-26 사용자가 선택한 C안이며, master-v2의 면과 재질을 유지한다.

기존 지상 이동·대상 탐색·근접 타격을 사용한다. 이동 중에는 걷기 두 프레임을
0.16초 간격으로 교대하고, 공격 자세를 0.16초 우선 표시한다. 이동 갱신이 끊기면
기본 자세로 돌아간다. InfantryPoseController가 모든 자세를 소유하며 기존 hopping은 제거한다.
초기 수치와 소환 비용은 database V045에서 관리한다. 로컬 마법 ID나 조합식은 없다.

아트: `ArcaneInfantry.png`, `ArcaneInfantryAttacking.png`,
`ArcaneInfantryWalkLeft.png`, `ArcaneInfantryWalkRight.png`. 네 프레임은
128×81, PPU 70, Bottom Center로 동일하다. 지지하는 발의 바닥 간격은 0px이다.
시안과 원본은 `.art/concept/arcane-infantry-restyle-20261026/`, 생성 프롬프트는
`.art/concept/arcane-infantry-restyle-20261026-prompts/`에 보존한다.
