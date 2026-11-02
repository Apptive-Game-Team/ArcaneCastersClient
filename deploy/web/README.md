# WebGL Nginx 이미지 게시

`dev` 또는 `deploy`에 push하면 Unity WebGL 빌드 후 Nginx 이미지를 검사하고 GHCR에 게시합니다.
`workflow_dispatch`도 두 브랜치에서만 게시합니다. Unity 버전은 `ProjectVersion.txt`를 읽습니다.
`dev`는 `BuildDevWebGL`, `deploy`는 `BuildWebGL`로 빌드합니다.
`ArcaneCastersPlay`와 `WORD_ONLINE_PLAY_TOKEN`은 이 workflow에서 사용하지 않습니다.
다른 track과 itch.io workflow는 변경하지 않습니다.

- 이미지: `ghcr.io/apptive-game-team/arcanecasters-play`
- `dev`: dev 브랜치의 최근 게시 이미지.
- `deploy`, `latest`: deploy 브랜치의 최근 게시 이미지. dev 빌드는 이 태그를 변경하지 않습니다.
- `sha-<전체 커밋 SHA>`: 배포 및 롤백에 사용할 태그. Actions summary에서 확인합니다.
- 대상 플랫폼: `linux/amd64` (GitHub의 `ubuntu-latest` runner).

서버 자동 갱신 기능은 포함하지 않습니다. dev에서 설정을 병합한 후 기존 승격 절차로
deploy에도 이 workflow를 반영해야 deploy 이미지 게시가 실행됩니다.

## GitHub 설정

기존 `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` secrets를 유지합니다.
GHCR 게시에는 `GITHUB_TOKEN`의 `packages: write` 권한을 사용하므로 별도 게시 토큰이 필요하지 않습니다.
조직 정책에서 Actions의 package 생성을 허용해야 합니다. 같은 이름의 package가 이미 있다면
이 저장소의 Actions 접근 권한도 확인합니다.
Package 공개 여부는 자동으로 바꾸지 않습니다. private package를 받는 서버는
`read:packages` 권한이 있는 계정으로 `docker login ghcr.io`를 먼저 실행합니다.

## 서버 실행

Docker Compose가 설치된 서버에 이 디렉터리의 `compose.yaml`을 복사합니다.
Actions summary의 실제 SHA 태그를 지정합니다.

```bash
export PLAY_IMAGE_TAG=sha-<전체-커밋-SHA>
docker compose -f compose.yaml pull
docker compose -f compose.yaml up -d
curl -I http://127.0.0.1:8081/
```

기존 서버 Nginx의 HTTPS virtual host에서 다음 upstream으로 전달합니다.
인증서와 DNS는 서버에서 별도로 관리합니다. 이미지는 HTTP 80 포트만 제공합니다.

```nginx
location / {
    proxy_pass http://127.0.0.1:8081;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
}
```

기존 플레이 도메인을 유지하면 origin 변경을 줄일 수 있습니다. 새 도메인을 쓰면
account와 lobby의 CORS 허용 origin 및 로그인/토큰 갱신을 확인합니다.
배포 전 실제 브라우저에서 게임 로딩, 로그인, 경기 진입을 확인합니다.
Compose 교체 시 짧은 요청 중단이 생길 수 있고, 이전 페이지가 요청하는 옛 빌드 파일은
새 이미지에 없을 수 있으므로 업데이트 후 페이지 새로고침이 필요할 수 있습니다.

## 검증과 롤백

이미지에는 Unity 프로젝트 대신 빌드 결과만 복사합니다. Nginx는 엔트리/PWA 파일을
재검증하고, WASM MIME 및 `.gz`/`.br`의 Content-Encoding을 설정합니다.
현재 프로젝트의 압축 설정은 disabled이며, 변경 시 실제 빌드 검사도 해당 헤더를 확인합니다.
PR 검사에서는 작은 WebGL fixture로 압축 파일과 누락 파일 404를 검사합니다.

```bash
bash deploy/web/build-image.sh play-local build/WebGL/WebGL
bash deploy/web/check-image.sh play-local build/WebGL/WebGL
```

롤백은 `PLAY_IMAGE_TAG`를 이전 게시 SHA로 바꾼 후 같은 pull/up 명령을 실행합니다.
운영에 쓰는 이전 이미지 태그는 GHCR에서 삭제하지 않습니다.
