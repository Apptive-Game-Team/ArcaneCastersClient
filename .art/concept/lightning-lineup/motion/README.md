# 전기 계열 외형과 전투 모션

[선택안과 색 비교](../README.md#선택안과-색-검토)의 A · A · A · B · B · B를 Unity 스프라이트에 적용했다. 폭풍편은 기존 외형과 동작을 쓴다. 전기 창병의 창날, 천둥 새의 번개 깃, 찌릿쥐의 꼬리만 금색을 더 선명하게 맞췄다. 나머지 면은 선택안의 색을 기준으로 프레임별 편차를 보정했다.

| 유닛 | 이동 | 공격 |
|---|---|---|
| 번개 정령 | 6칸 · 구름과 꼬리의 부유 | 7칸 · 방전, 접힘, 복귀 |
| 전기 창병 | 8칸 · 창을 든 부유 이동 | 6칸 · 창을 뻗고 회수 |
| 전기 슬라임 | 6칸 · 바닥을 짚고 전진 | 6칸 · 앞쪽이 늘어났다 복귀 |
| 천둥 새 | 6칸 · 날개 올림과 내림 | 8칸 · 날개 접기, 급강하, 날개를 펴며 상승, 복귀 |
| 찌릿쥐 | 8칸 · 양쪽 발의 디딤과 통과 자세 | 6칸 · 낮게 몸을 뻗고 복귀 |
| 폭풍 사슴 | 8칸 · 돌진과 공포 후퇴의 달리기 | 충돌로 피해를 주며 별도 공격 프레임은 없음 |
| 폭풍편 | 기존 8칸 | 기존 6칸 |

`UnitSpriteMotionController`가 한 칸을 1/12초 동안 재생한다. 천둥 새의 공격 순서는 `ThunderBirdDive` → `ThunderBirdContact` → `ThunderBirdRecover` → `ThunderBirdSwarm`이며 각각 두 칸이다. 부리와 몸의 높낮이는 스프라이트에 담았다. 공격 이벤트와 서버의 이동·충돌 판정은 기존대로 동작한다. 새 스프라이트는 같은 유닛의 캔버스 크기, PPU, Bottom Center 피벗을 사용한다.

제작 프롬프트 [번개 정령](ThunderSpirit.prompt.txt), [전기 창병](ElectricSpearman.prompt.txt), [전기 슬라임](ElectricSlime.prompt.txt), [천둥 새](ThunderBird.prompt.txt), [천둥 새 급강하](ThunderBird-dive.prompt.txt), [찌릿쥐](ZapMouse.prompt.txt), [폭풍 사슴](StormStag.prompt.txt)
