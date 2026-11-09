# 2026-11-01 — 세력 선택과 단계별 전투 튜토리얼 추가

- Date: 2026-11-01
- GitHub Issue: #397
- Status: Validated

## Goal

신규·게스트 계정이 모션 코믹을 보고 HELL/NATURE를 선택한 뒤, 실제 게임 마법으로 전투를 익힌다. 기본 마법 5종은 기존 성장 API로 지급한다. 임시 메테오·토네이도 체험은 카드 소유권을 추가하지 않는다.

## Non-goals

PvP 밸런스, 마법의 수치, 성장 보상 정책, 기존 계정의 카드·덱, 운영 배포는 변경하지 않는다. 클라이언트에 별도 전투 규칙을 만들지 않는다.

## Context / Constraints

- Client #392의 컷신과 #396의 시작 카드 UI/API를 함께 사용한다. 작업 브랜치는 feature/391 위에 feature/393을 합친 격리 worktree다. 두 선행 PR을 의존성으로 명시한다.
- Lobby #106의 진행 API는 HELL/NATURE 선택과 15장 시작 덱 생성을 이미 처리한다. 그 위에 Lobby #107을 쌓는다.
- Game #186에서 인증된 소유자만 접근하는 튜토리얼 전투를 제공한다. 정상 SessionService를 호출하지 않으므로 전적·매칭 상태·MMR·보상·복구 기록이 생기지 않는다.
- 현재 튜토리얼은 모든 투사체를 같은 모형, 모든 소환을 물 궁수 모형으로 표현한다. 기존 모형 루프 대신 GameScene의 실물 프리팹·카드·조준·PresentationFramePlayer를 사용한다.
- 두 세력의 카드: HELL = fire_shot, ember_spirit_swarm, towerback, rock_golem, magma_explosion. NATURE = chain_lightning, water_slime_swarm, vine_spirit, tree_golem, water_explosion.

## Approach (Checklist)
- [x] **Step 0: Recon** (Inspect existing code, locate files)
  - 기존 컷신, 초기 계정 진입, 성장 API, GameScene 입력·프레임 처리와 Game PreviewCapture를 확인했다.
- [x] **Step 1: Implementation** (Code changes, file paths)
  1. ComicIntroPlayer가 컷신 완료/건너뛰기 모두 세력 선택 화면으로 이동한다. 기존 UI 프리팹으로 두 세력과 시작 카드 목록을 표시한다. /progression/sync에서 기존 선택이 있으면 재선택 없이 복원한다. 선택 오류는 성공으로 진행하지 않고 재시도를 제공한다.
  2. Lobby POST /api/tutorial/battles는 인증 사용자와 DB에 저장된 세력만 사용한다. 클라이언트가 사용자·보상·세력을 임의 지정하지 못한다. 가용 game 서버의 서버 인증 전용 생성 API를 호출하고 공개 주소와 battleId를 반환한다. 일반 매칭/SessionRecoveryStore를 사용하지 않는다.
  3. Game의 TutorialBattle은 PreviewCapture와 같은 GameContext/실제 Parameters/PrefabInitializer/physics/component/lifecycle 시스템을 사용한다. 서버가 단계별 허용 마법·마나·목표·진행을 소유한다. 클라이언트 요청 시 서버 단조시계로 경과한 고정 tick만 처리하고 요청당 처리량을 제한한다. 요청 빈도로 시간을 가속할 수 없다. 오랫동안 연결이 없으면 일시정지한 것처럼 제한된 tick만 진행한다.
  4. 한 사용자당 한 전투, 만료 시간, 총 동시 전투 상한, 오브젝트 상한을 둔다. 사용자 API는 ownerId를 인증에서 가져온다. cast/next/retry는 단계 revision 및 명령 ID를 검사해 재전송으로 중복 실행하지 않는다. 허용 마법 외 시전·비정상 좌표·범위 밖 배치·마나 부족을 거부한다. GET state는 전체 객체 snapshot과 최근 프레임을 제공하여 응답 손실 뒤 복구한다.
  5. 학습 구간은 shot → meteor_shower → tornado_strike → anti_air → formation → explosion → final. 단계의 적과 생성된 적 소환물까지 실제 사망을 확인해야 성공한다. 허공 시전, 단순 타이머, 이전 단계의 시전 이력으로 완료하지 않는다. formation은 골렘을 먼저 소환한 뒤 무리를 사용하도록 안내하고 두 사용을 확인한다. final은 기본 카드 5종을 허용하고 세 웨이브를 차례로 보내며 카드 정답을 강제하지 않는다.
  6. 단계 시작 시 필요한 마나를 주고 이후 정상 4/s 충전을 적용한다. 초기 학습에서는 필요한 카드만 제공한다. 완료 피드백 후 next로 전장·일회성 시전 이력을 초기화한다. 패배하면 현재 단계만 retry한다. 힌트는 학습 구간에서만 정답 카드를 가리키고 final에는 적 상태만 알린다.
  7. GameScene을 기반으로 전용 FactionTutorialBattleScene을 만든다. 정상 STOMP·ping·fever 컴포넌트를 씬에서 제거하고 전용 HTTP driver가 기존 프레임 표현을 호출한다. CardInputSender에는 튜토리얼 모드 입력 경계만 추가한다. 일반 게임에서는 동작을 바꾸지 않는다. 팝업·버튼은 기존 UI 프리팹을 인스턴스한다.
  8. 마지막 구간을 이기면 완료 안내와 지급받은 다섯 카드/시작 덱을 확인하고 로비로 간다. 기존 덱 만들기·마법 도감 강제 안내는 신규 흐름에서 생략한다. 정상 로그인·다른 사용자에게 튜토리얼 상태가 새지 않도록 세션 데이터와 종료 경로를 정리한다.
- [x] **Step 2: Tests** (Unit tests, manual verification steps)
  - 실제 마법과 prefab으로 두 세력의 각 구간을 돌려 공중 타깃·상성·전멸·재시도를 확인한다. 형성/최종 구간의 성공 조건은 시전 횟수와 분리해 테스트한다.
  - 인증·소유권·서버 전용 생성·마법 allowlist·범위·revision·중복 요청·상한·만료·시간 가속 방지와 로컬 상태 초기화를 검증한다.
  - Unity import/compile, EditMode 테스트, 두 세력의 흐름·카드·한국어/영어·16:9/4:3·컷신 skip·실패/재시도를 확인한다. 실제 서버 E2E가 불가능하면 결과와 한계를 구체적으로 기록한다.
- [ ] **Step 3: Rollout / Rollback** (Feature flags, migration steps)
  - 선행 DB V121, Lobby #106, Client #392/#396 이후 Game #186 → Lobby #107 → Client #397 순서로 적용한다. 새 DB migration은 필요하지 않다.
  - 모듈별 draft PR을 열어 상호 의존성과 검증을 기록한다. merge/deploy는 수행하지 않는다.

## Validation
- **Commands to run:** game/lobby Gradle test, Unity batchmode import/EditMode/PlayMode, scene/prefab/localization references, git diff --check, root graphify update .
- **Expected output:** 두 세력의 같은 학습 순서, 5종 시작 카드 일치, 임시 마법 회수, 실제 처치 기반 진행과 구간별 재시도, 일반 경기 회귀 없음. Graphify 미설치 시 해당 한계를 기록한다.

### 구간 정의

좌표는 x/z이며 플레이어는 (1,5), 상대 본진은 (17,5)이다. 모든 구간 시작 마나는 120, 이후 4/s다. 적 수·위치는 실제 시뮬레이션 검증 결과에 따라 조정하되 공유 스탯은 수정하지 않는다.

| 구간 | 허용 카드 HELL / NATURE | 적과 배치 | 성공 조건 |
|---|---|---|---|
| shot | fire_shot / chain_lightning | VineSpirit 1 (8,5) / WaterSlime 5 (8~9,4.5~5.5) | 적 전멸과 현재 구간에서 실제 시전 1회 이상 |
| meteor | meteor_shower, 시전 후 기본 슛 | WaterSlime 8 (5.5~6.3,4.5~5.5) | 메테오 체험 후 기본 슛으로 남은 적까지 전멸 |
| tornado | tornado_strike | MiniRock 6 (8~10,4.7~5.3) | 적 전멸 |
| anti_air | towerback / vine_spirit | BubbleSpirit 1 (9,5), 본래 공중 위치 사용 | 실제 공격으로 공중 적 처치 |
| formation | rock_golem, ember_spirit_swarm / tree_golem, water_slime_swarm | 골렘 배치 후 MiniRock 2 (10,4.7/5.3) | 골렘 먼저 소환, 그 골렘이 피격된 뒤 무리 사용, 적 전멸. 서버가 순서를 검사하고 먼저 무리를 쓰면 거부 |
| explosion | magma_explosion / water_explosion | SeedSpirit 8 / EmberSpirit 6, (8~9,4.5~5.5) | 적 전멸. 불→자연, 물→불 약점 안내 |
| final | 세력별 기본 5종 | MiniRock 2 → BubbleSpirit 1 → SeedSpirit 6 / EmberSpirit 4, 앞 웨이브 전멸 후 다음 웨이브 | 정답 카드 강제 없이 전체 적 전멸 |

구간 전멸은 RightPlayer의 살아 있는 Mob와 대기 중인 소환까지 검사한다. 스스로 만료되는 적은 사용하지 않는다. 아군 본진 사망 또는 구간 진행 120초 초과는 FAILED이며 현재 구간만 retry한다. 성공한 구간은 CLEARED로 멈추고 플레이어의 next 이후 진행한다. 마지막 성공은 COMPLETED다.

### API와 프레임 계약

- Lobby POST `/api/tutorial/battles`, body `{requestId}`. 서버가 저장된 세력을 읽어 게임 서버에 전달한다. 같은 owner는 requestId가 달라도 같은 서버에 고정한다. 생성 실패 시 다른 서버로 자동 전환하지 않는다. 응답은 `{battleId, ownerId, faction, server, expiresAt}`. 로비의 사용자별 생성 affinity는 유효 시간 동안 유지하고 테스트한다.
- Game POST `/api/server/tutorial/battles`, WORDONLINE_SERVER 전용, body `{ownerId,faction,requestId}`. 같은 owner/requestId는 기존 전투를 반환한다. 사용자당 한 전투 제한은 game 인스턴스 단위다. 신규 요청도 기존 활성 전투를 반환한다.
- Game GET `/api/tutorial/battles/{id}/state?afterFrame=N` → `{battleId,ownerId,faction,revision,stage,status,mana,cards,frames,snapshot,lastFrame}`. `frames`는 N 이후의 프레임을 오름차순으로 제공하며, 각 프레임은 `{frame,objects,events}`다. 마지막 snapshot은 `lastFrame` 시점이다. 프레임 기록은 제한된 ring buffer다. 클라이언트는 frame watermark로 이미 처리한 프레임을 제외하고, 프레임 적용 후 snapshot으로 누락을 복구한다. 프레임/이벤트 DTO는 기존 게임 표현 형식을 사용한다.
- Game POST `/api/tutorial/battles/{id}/command`, body `{commandId,revision,action,magic,position}`. action은 CAST/NEXT/RETRY다. 같은 commandId 재요청은 재실행하지 않는다. 현재 revision과 다르면 409; 소유자 불일치 403; 없음/만료 404/410; 잘못된 마법·좌표·순서·마나는 400이다. 응답은 accepted 여부만 제공하고 state polling으로 최신 결과를 읽는다. NEXT/RETRY마다 revision을 증가시킨다.
- Game DELETE `/api/tutorial/battles/{id}`는 본인 전투만 정리한다. TTL과 오브젝트 상한은 환경 설정과 테스트에서 확인한다. 명령·상태·tick은 arena 단위 lock으로 직렬화한다. HTTP 실패 시 client는 입력 잠금을 풀고 명시적으로 재시도하며 해당 전투가 완료됐다고 처리하지 않는다.
- Preview의 production component/physics/lifecycle만 재사용한다. PreviewConstruction, passive target, suppressesDeathFields 정책과 범용 녹화 엔진 추출은 가져오지 않는다. 본진의 피격·패배만 튜토리얼 전용 component로 처리한다.
- CardInputSender의 선택/취소 전송은 튜토리얼에서는 로컬 처리하고 CAST만 전용 driver에 보낸다. 성공·실패 응답은 기존 선택 잠금/복원 경로를 사용한다. 단계 DSL이나 별도 캠페인 엔진은 만들지 않는다.

### 진입·중단·완료 정책

- 가입/새 게스트는 기존 TutorialScene의 진입 선택 대신 ComicIntroScene으로 바로 간다. Comic 완료와 skip 모두 FactionSelectionScene으로 이동한다. 선택 단계에서 UserInfoGetter를 먼저 호출하여 사용자 식별자를 확보한다.
- 진행 표식은 matching server와 userId별 PlayerPrefs에 저장한다. `pending`/`completed`는 이 기기에서만 유지되며 다른 기기로 이전하지 않는다. 보상/세력은 기존 서버 DB에 보존된다. 선택 전 실패는 선택 화면에 남고, 이미 선택한 세력은 자동 복원한다.
- pending에는 생성 전에 저장한 requestId와 응답 후 저장한 battleId/server/expiresAt를 함께 둔다. 앱 재시작·생성 응답 유실에도 같은 requestId를 사용한다. 이미 주소를 알면 같은 game API의 state로 직접 복귀하며, 404/410 또는 명시적 만료 후에만 battle 정보를 지우고 새 requestId로 생성한다. 로비 affinity는 사용자별 TTL 동안 유지한다.
- 중간 종료 후 같은 기기의 일반 로그인·자동 로그인은 로비 초기화에서 pending을 확인해 선택/전투 연결로 복귀한다. 활성 battle은 서버에서 이어받는다. 전투 TTL이 지나면 세력은 유지하고 shot부터 다시 진행한다. 전투 내 패배는 현재 구간만 재시도한다.
- 기존 계정에 pending 표식이 없으면 새 튜토리얼을 강제하지 않는다. COMPLETED 응답을 받은 뒤에만 로컬 completed로 바꾸며 로비로 이동한다. 기존 BattleState→DeckState 연결은 신규 흐름에서 사용하지 않는다. GlobalTutorialManager를 종료해 로비의 이전 강제 안내가 겹치지 않게 한다.
- 취소/로그아웃/다른 계정 전환에서는 driver를 종료하고 SceneContext.MatchInfo를 정리한다. 비동기 응답의 ownerId와 현재 계정을 대조하여 이전 계정의 결과가 새 계정에 적용되지 않게 한다.

### 계획 검토 반영

- FAST REVIEW의 구간·API·진입/중단·생성 재시도 결정을 위에 확정했다.
- MEDIUM REVIEW: PASS. 녹화 전용 정책을 재사용하지 않고, CAST와 selection 전송 경계를 함께 처리하며, 일곱 구간을 명시적 메서드로 구현한다.
- Rejected feedback: 없음.

## Risks & Rollback
- **Risks:** 서로 다른 선행 PR이 아직 dev에 merge되지 않았다. 초기 실습 웨이브는 실제 현재 스탯으로 검증해야 하며 실패할 경우 적 수·배치만 조정한다. 서버 API 실패 시 재시도 UI를 유지한다.
- **Rollback steps:** 새 튜토리얼 진입 변경과 전용 API를 모듈별 revert한다. 카드 지급은 기존 멱등 API가 소유하므로 회수/중복 지급하지 않는다.

## Open Questions
- 없음. 컷신·시작 카드 지급 계약과 기존 마법 이름을 재사용한다.

## 구현 검증 결과

- HEAVY REVIEW: PASS. FAST의 복구·API·구간 정의와 MEDIUM의 입력 경계·실제 엔진 재사용 지적을 반영했다.
- 게임 서버 전체 1,234개 테스트 통과. 현재 DB의 974개 게임 파라미터만 읽기 전용으로 캡처해 두 세력의 7개 구간을 실제 마법·prefab·physics로 통과했다.
- 로비 전체 402개 중 실패 0개, 통과 359개, 기존 조건부 통합 테스트 43개 건너뜀. 응답 유실·다른 UUID·서버 목록 변경에도 owner별 서버가 유지된다.
- Unity 전체 EditMode 719개 중 716개 통과, 실패 0개, 기존 3개 건너뜀. 최종 복구 변경 후 FactionTutorialTests 7개 재통과.
- 컷신 Play Mode 3개 통과. 실제 GameScene의 카드 선택/단계 전환 Play Mode fixture 1개 통과. 네트워크 driver를 막은 UI fixture이며 실제 계정 E2E가 아니다.
- 한국어 16:9·영어 4:3 네이티브 Unity 프리팹 렌더를 검토했다. 기본 카드·두 세력 선택과 완료 화면을 캡처했다.
- 신규 인증 직후 계정별 진입 의도를 저장하여 첫 사용자 조회 실패에도 복구한다. comicSeen으로 만화 미완료와 세력 선택 재개를 구분한다. 마법·range 카탈로그를 못 읽으면 빈 손패로 진입하지 않고 재시도한다.
- 메테오는 무작위 낙하이므로 기본 슛으로 잔여 적을 정리하게 했다. 물 폭발 약점 실습은 실제 한 번에 처치되는 EmberSpirit을 사용한다.
- 운영 DB/사용자 데이터 변경, 배포, 실제 신규 계정 E2E, WebGL player build는 수행하지 않았다. Graphify는 graph/실행 파일이 없어 갱신하지 못했다.
