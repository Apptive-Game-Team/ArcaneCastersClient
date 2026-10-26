# 마법 유형 문양

인게임 카드의 작은 원형 배지용 벡터 문양이다. UI의 잉크색 `#1C1A2B`와
기존 `FlatCircle`을 사용한다. 원소를 뜻하는 `type_*.png`와 구분한다.

| 파일 | 내부 유형 | 문양 |
| --- | --- | --- |
| unit | Unit / Spawn | 투구 |
| building | Building / Summon | 성탑 |
| drop | Drop | 아래로 떨어지는 화살과 바닥 |
| explosion | Explosion | 방사형 충격파 |
| shot | Shot | 진행 방향과 궤적 |

`sharp`가 설치된 Node.js 환경에서 다음 명령으로 PNG를 재생성한다.

```sh
node .art/tools/render-cast-kind-icons.cjs
```

출력은 `Assets/Art/Images/UI/Card/CastKind/`의 투명 128×128 PNG 5장이다.
배지는 36×36, 내부 문양은 24×24 canvas unit이며 카드 우상단에서 9만큼 떨어져 있다.
1280 화면에서는 문양이 약 16px로 줄어든다. 크기를 바꿀 때 다섯 문양을 함께 검수한다.
두 Image의 raycast를 꺼서 클릭과 hover가 기존 카드로 전달되게 한다.

`docs/pr-media/371/cast-kind-card-layout.png`는 기존 아트와 프리팹 치수로 만든
오프라인 배치 예시다. Unity 실행 화면이나 실제 마나 데이터 검증 자료는 아니다.
