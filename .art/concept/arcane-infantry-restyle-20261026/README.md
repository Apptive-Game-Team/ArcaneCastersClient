# 비전 보병 C안 적용

2026-10-26 사용자가 C안을 선택하고 걷기 2프레임을 추가 요청했다.
모든 원본은 built-in image_gen으로 생성했다. 생성 프롬프트는
[`../arcane-infantry-restyle-20261026-prompts/`](../arcane-infantry-restyle-20261026-prompts/)에 있다.

- `C-production-pair.png`: 기본·공격 원본.
- `walk-step-one.png`, `walk-step-two.png`: 서로 반대 발을 든 원본.
- `C-walk-pair.png`: 두 걷기 원본의 해상도를 맞춘 시트.
- `production-geometry.json`, `walk-geometry.json`: 공통 배율, 배치 및 알파 검사값.
- `Before-idle.png`, `Before-attack.png`: 교체 전 비교 자료.

`.art/tools/finalize-arcane-blades.cjs`는 Node.js와 sharp로 분할·크기 정규화만
수행한다. 기본·공격 쌍은 발 중심, 걷기 쌍은 머리 중심과 지지하는 발 바닥을
기준으로 맞춘다. 모두 128×81, PPU 70, Bottom Center다. 기존 PNG의 GUID는 유지했다.

```text
node .art/tools/finalize-arcane-blades.cjs
node .art/tools/finalize-arcane-blades.cjs C-walk-pair.png walk
node .art/tools/preview-arcane-blades.cjs
```

Unity 2022.3.34f1: EditMode InfantryCarrierPrefabTests 11개와 PlayMode
InfantryPosePlayModeTests 1개 통과. 실제 이동 이벤트 간격으로 걷기 교대,
공격 우선, 공격 종료 후 이동/정지 복귀, 비활성 이벤트 무시와 좌우 방향 보존을 확인했다.
비교 이미지·GIF는 런타임 스프라이트를 합성한 자료이며 실제 게임 녹화가 아니다.
