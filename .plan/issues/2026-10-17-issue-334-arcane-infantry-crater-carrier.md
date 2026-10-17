# 2026-10-17 — 비전 보병과 분화구 운반수 구현
- Date: 2026-10-17
- GitHub Issue: client #334, game #147, database #115
- Status: Implemented and verified
## Goal
Add playable arcane_infantry (비전 보병) and crater_carrier (분화구 운반수) matching approved concept art to current dev single-magic-card model.
## Non-goals
No deployment, no version bump, no changes to cards/recipes, no changes to unrelated in-progress work.
## Context / Constraints
Independent worktrees at .worktrees/infantry-mortar/{game,client,database}; game/client based on origin/dev, DB stacked on origin/feature/111 (V044). DB migration precedes publishing dependent game code. No graph JSON/tool available in root.
## Approach (Checklist)
- [x] Step 0: Recon. Reuse AbstractSpawnMagic, MeleeAttackMob, BehaviorMob, existing AoE/counter contracts and AbstractRangeMob Unity prefab.
- [x] Step 1: Implementation. Add magic beans, prefab enum entries and GameObjectKey entries. Infantry: Element None, one basic melee ground unit (mana 20, HP 360, damage 70, interval 1.0, speed 1.4, radius .35, small, mass 2). Carrier: Fire+Rock, medium ground siege unit (mana 45, HP 440, damage 150, interval 2.6, speed .65, radius .5, mass 4, range 7, blast radius 1.4, deployment 1.2s, recovery .9s, shell horizontal speed 6). Cast range 6; quantity 1. All balance values DB-backed, use new setup_duration and recovery_duration keys only where needed.
- Carrier local BehaviorMob subclass: intercept entry into inherited AttackState to run Deployment state; after setup validate current ground enemy/range then fire once and enter a SiegeAttackState extending AttackState, preserving shared target checks and interval scaling. When target lost/out of range/directive movement requested, force Recovery before walking; repeated directive updates cannot restart recovery. Stun interrupts setup/shooting, then requires recovery before movement; master conversion invalidates targets and also respects recovery. No general AI rewrite. New raw status names Deploying, Deployed, Recovering; presenter handles Attack as deployed and next server tick restores phase. Use OnOtherStatus for sync as well as live frames, no client-side gameplay timer.
- Shell is its own CraterShell server GameObject with ballistic movement, fixed copied ground impact point, no collision during flight, one ground-only enemy splash at arrival, Fire+Rock attack attribution preserved when shooter dies. Exclude allies/air/destroyed; no early damage, no homing. Spawn pure-visual timed CraterExplosion (no second damage). Medium carrier leaves small RockRemnant. Update allowlist.
- DB V045 after checking remote refs for unique numbers: magics, objects, tuning incl magic_id, cast indicator, prefab_elements Fire,Rock, tags unit+size+role+AoE, sync derived magic tags. Access policy consistent with existing non-premium summon. No prod data SQL in game.
- Client: canonical anchor based production sprites (infantry idle/attack; carrier carried/planted/recovery; molten shell; reuse appropriate established explosion artwork). Crop with shared scale and baseline for frame sets, verify alpha/64px. Prefabs named to match server, custom carrier presenter subscribes statuses; disable default sprite swap/swing conflicts on carrier. Add all six localization rows per magic with final Korean names and accurate gameplay descriptions, no local IDs/recipes.
- [x] Step 2: Tests. Server regression tests for melee, deployment/recovery locks including target loss/stun/directives, delayed fixed-point splash including shooter death, ground targeting/allies, single impact, rubble. DB migration static validation plus isolated Postgres apply twice if local runtime available. Client compilation via Unity CLI when installed, else contract/asset validators and clearly document Editor limitation. Runtime asset resource/guid/pivot/localization checks.
- [ ] Step 3: Rollout / Rollback. Commit/push DB first, then server and client. Open linked module PRs (DB base feature/111, others dev), attach all. Do not merge or deploy. Parent pointer only if safely published and requested by repo workflow.
## Validation
- Commands to run: gradlew.bat test; bash scripts/ci/validate-migrations.sh origin/feature/111; client asset/localization consistency and available C# compilation.
- Expected output: all new tests and required regression suite pass, no missing resources, idempotent DB registration, complete final name/description tables.
## Risks & Rollback
- Risks: siege transitions around target loss/stun/directives, damage on wrong ground point, misaligned sprite frames.
- Rollback steps: revert feature commits before deployment; forward migration if already applied.
## Open Questions
None required; initial balance is a reviewable starting point.
## Plan Review
- Fast PASS: add directive-vs-stun/recovery, setup target loss, full-snapshot phase, zero-distance single-impact tests.
- Medium PASS: keep carrier in attacker package; SiegeAttackState delegates inherited cadence/validity and restores phase only when still current. Presenter derives ServedObjectBehaviour and uses OnBound/OnUnbound. Dedicated fixed-point shell justified. No shared AI rewrite.
- Clarification: ignore ordinary Move/Idle/Directive requests while stunned; allow stun refresh and after stun finishes require recovery if equipment was deployed. Recovery requests never reset timer.

## Verification results
- Three plan reviewers passed.
- Game: full Gradle suite passes, including deterministic and production previews (88 magics / 132 scenarios).
- Client: Unity 2022.3.34f1 compilation and 40 EditMode prefab/phase/audio tests pass. All 12 localization rows and 4 prefab GUID/fileID graphs validated.
- DB: V045 static validation passed against origin/feature/111. Docker daemon unavailable, so no PostgreSQL application was performed.
- Graphify update attempted at monorepo root; executable is not installed.
- Production PNGs and prompts are persisted; original checkout changes remain untouched.
