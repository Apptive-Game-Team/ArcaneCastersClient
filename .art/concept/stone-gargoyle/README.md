# 돌 가고일 외형 후보

2026-11-04에 기본 외형을 생성하고 2026-11-05 사용자 요청으로 지옥불 차원 영향을 약 30% 추가했다.

`hellfire-30-source.png`가 승인된 수정 원본이고 `StoneGargoyle-approved.png`가 최대 192px 게임용 이미지이다. 기본 외형 `base-source.png`는 돌 부족 형태와 자세의 참조로 남긴다. 최종 프롬프트는 `../stone-gargoyle-prompts/hellfire-30.txt`에 있다. 내장 image_gen으로 생성했다.

수정안은 따뜻한 석재 몸통·이끼·붉은 눈을 유지하고 날개 가장자리·어깨·발톱에 그을린 돌과 붉은 균열을 넣었다. 화풍은 master-v2의 큰 다면체와 무광 cut-paper 표현을 따른다. 세계관의 새 종족 관계는 확정하지 않는다.

게임용 후보는 192x176 RGBA, Bottom Center, PPU 100이다. 파란 키 배경을 `--deadzone 4`로 제거했으며 네 모서리 alpha는 0이고 불투명 픽셀에 파란 잔색은 없다. 투명 픽셀 비율은 약 47.6%다. `review-sheet.png`에 어두운 바탕·밝은 바탕과 192/96/64px 크기, 기존 돌 골렘·번개 정령·지옥불 악마를 함께 표시했다.

2026-11-05 사용자가 수정안 적용을 승인했다. `Assets/Resources/Game/sprites/StoneGargoyle.png`와 `Assets/Resources/Prefabs/StoneGargoyle.prefab`에 연결했다.

기본 blue key 계산은 눈의 거의 흰 하이라이트 두 픽셀 RGB (251, 251, 253)/(251, 251, 252)까지 배경으로 판정했다. 배경색을 바꾼 재생성도 같은 문제가 한 픽셀 남았다. 실제 키색과 팔레트가 겹친 경우가 아니므로 기존 도구의 `--deadzone 4`로 키 판정 범위를 좁혔다. 승인된 원본의 눈과 형태를 유지했고 `enclosed_pixels=0`이다. 배경색 수정용 재생성물은 사용하지 않는다.
