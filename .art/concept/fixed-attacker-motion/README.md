# 고정 공격·지원 설치물 모션

기존 스프라이트에서 받침대, 돌 기단, 뿌리를 고정했다. 새 그림은 발사나 발동이 일어나는 상단에 사용한다. 모두 기존 `AttackFrameSequenceController`를 쓰며 서버의 `Attack` 상태가 도착할 때 발동 2칸, 복귀 1칸, 기본 자세 1칸을 12fps로 재생한다. 원래 캔버스 크기, 피벗, PPU를 사용한다.

| 프리팹 | 동작 |
|---|---|
| `GroundCannon`, `GroundTower` | 포신 반동과 복귀. `RockShot` 발사 때 포구 섬광 3칸(30fps, 0.10초) |
| `ElectricTower` | 결정의 방전과 복귀 |
| `RockTurret` | 투석 팔이 벌어지고 빈 투석부에 돌이 돌아옴 |
| `DragonTower` | 용 머리의 입이 열렸다 닫힘 |
| `FireworkTower` | 불꽃탄을 쏘는 틱에 `Attack`을 보내 상단 불꽃 핵의 발동 자세를 재생. 탄착 폭발은 비행이 끝날 때 발생 |
| `BubbleGenerator` | 상단의 보호 거품이 늘어났다 복귀 |
| `LifeTree` | 잎 사이와 줄기의 핵이 열렸다 복귀 |
| `HealingTotem` | 꽃잎이 치유의 핵을 드러냈다가 복귀 |
| `TitanRemnant` | 거신 주먹이 소환될 때 본체의 핵이 열림 |

폭풍 사슴은 돌진과 공포 후퇴에 달리기 8칸을 공유한다. 서버의 `HitEvent`가 도착하면 피해를 입힌 대상 쪽으로 몸을 낮춘 뒤 회복한다. 충돌 자세는 12fps로 낮춘 자세 2칸, 회복 1칸을 재생한다. 이후 현재 이동 상태로 돌아간다.

[유닛별 1배속 미리보기](https://arcane-combat-motion-review.dev-yunseong.chatgpt.site/motion.html) · Imagegen 프롬프트 [인간 설치물](human.prompt.txt), [다른 공격 설치물](other.prompt.txt), [지원·소환 설치물](support.prompt.txt)

## 스프라이트 색 통일

미리보기의 59종, 스프라이트 274장을 확인했다. 같은 유닛의 기본 자세를 기준으로 불투명한 부분의 색을 재질별로 비교했다. 차이가 큰 이동·공격·복귀 프레임 34장의 명도와 색조를 보정했다. 초록 잎, 푸른 물, 검붉은 돌과 불꽃처럼 진영과 동작을 구분하는 색을 유지했다. 보정 전후 캔버스와 알파 채널은 픽셀 단위로 일치한다.

[대표 프레임 보정 전후](palette-review.png)
