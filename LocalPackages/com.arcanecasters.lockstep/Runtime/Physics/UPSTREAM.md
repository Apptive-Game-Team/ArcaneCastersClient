# 고정 소수점 물리엔진 출처

https://github.com/sam-vdp/bepuphysics1int 의 커밋 `9237daa68c3014fd7c2e93c6a99326ba5248d60b`에서 런타임 C# 소스만 가져왔다. `LICENSE.md`와 `FixedMath.LICENSE.txt`를 함께 배포한다.

수정 사항은 `Tools/LockstepProbe/Patch-Engine.ps1`에 기록했다. Fix64의 CHECKMATH 경로에서 남은 `m_rawValue`를 `RawValue`로 수정하고 BoxBoxCollider의 float 리터럴을 명시적 decimal→Fix64 상수로 바꿨다. `WINDOWS`는 BCL HashSet 선택에만 사용하며 모든 플랫폼에서 `CHECKMATH`와 함께 적용한다. Space는 단일 스레드로 실행한다.

전투 어댑터의 재질은 `VelocityOnlyContactResponse`를 명시적으로 켠다. 양쪽 재질이 켠 접촉에서만 warm-start와 침투 보정을 끄고, 반발 속도와 최대 충격량을 적용한다. 일반 유닛끼리는 충격량 상한 1, 경계가 포함되면 상한 없이 처리한다. `Material`과 `ContactPenetrationConstraint`에 추가한 이 선택적 설정도 패치 스크립트로 재현한다. 기본 재질을 사용하는 기존 엔진 probe의 동작과 재생 해시는 유지한다. 전체 원본 물리 테스트 통과는 아직 검증 중이다.

전투 접촉에는 회전 충격량을 적용하지 않는다. 몸체 중심 높이가 같으면 접촉 법선의 높이 성분을 제거하고 수평 성분을 정규화한다. 밀집된 원형 몸체의 수치 오차가 높이 이동으로 누적되는 것을 막는다. 다른 높이의 접촉은 원래 법선을 유지한다.
