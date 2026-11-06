# 핀의 2단계처럼 한쪽 팔에 적용한 부분 잠식

이 문서는 v3 이력이다. 이후 사용자 요청으로 잠식을 약 70%까지 넓힌
[v4](../v4/README.md)가 현재 적용본이다. 이 폴더의 스크립트는 v3 재현용이다.

사용자는 붉은 눈만 있는 v1보다 타락이 보이길 원했고, 전신이 어두워진 v2에는
핀처럼 더 부분적인 잠식을 요청했다. 현재 v3는 붉은 눈을 유지하며 화면 오른쪽
팔과 어깨 가장자리에만 검붉은 돌 표면과 짧은 붉은 균열을 적용한다.
얼굴과 몸통 대부분·반대쪽 팔·두 다리의 돌색과 초록 이끼는 남긴다.

핀 참고는 website 저장소의
`content/arcane-casters/story/references/mage-corruption.png` 가운데 2단계다.
국소적으로 갑각과 균열이 나타나는 범위만 참고하고 만화 그림체는 옮기지 않는다.
`RockGolem` 기본·공격은 같은 팔이 잠식되어 있고 `RockMage`의 푸른 지팡이
수정은 그대로다. 전체 네 유닛, 다섯 런타임 PNG에 적용했다.

## 제작과 재현

- built-in `image_gen` 편집을 사용했다. `prompts/`에 정확한 지시를 남겼다.
  기존 v1 PNG와 핀 시트를 참고했고 기본 골렘을 나머지의 부분 잠식 범위 참고로 사용했다.
- `sources/`는 채택한 생성물을 최대 512px로 정리한 입력이다.
- 꼬마돌의 부분 편집은 두 번 출력 단계에서 거절됐다. 앞서 생성된 v2 꼬마돌의
  팔 부분만 v1 원본에 `mini-arm-mask.png`로 합성한 결과를 `sources/`에 보존했다.
  나머지 부위와 원본 alpha는 합성 단계에서 유지했다.
- `finalize.py`는 v1 커밋 `bfca3fa9`의 캔버스·alpha bbox·바닥 여백에 맞춰
  정규화한다. 수치와 `.meta` 동일 확인은 `validation.json`에 기록했다.
- 전체 생성물과 원본 입력은 로컬 `generated/`, `inputs/`에 보관하며 Git에는
  최종 선택 입력과 프롬프트, 재현 스크립트만 넣는다.
- 최종 파일은 `Assets/Resources/Game/sprites/`의 기존 다섯 PNG다.
  비교는 `docs/pr-media/423/golems-partial-corruption-v3.png`와
  `golems-partial-corruption-v3-64px.png`에 있다. 런타임 캡처가 아닌 PNG 비교다.

캔버스·alpha bbox·바닥 여백, `.meta`의 GUID·PPU·피벗과 prefab 연결을 유지했다.
몸체 표현을 갱신했으므로 눈 밖 픽셀과 alpha 윤곽 전체가 v1과 같다는 의미는 아니다.
원본 크기·64px 비교를 확인했다. Unity Editor 실제 재생은 수행하지 않았다.
