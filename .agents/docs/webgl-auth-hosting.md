# WebGL 빌드는 올라간 page 에 따라 인증이 다르게 동작한다

같은 WebGL 빌드라도 어느 page 에서 도느냐에 따라 account server 와의 인증이 달라진다.
코드만 읽어서는 드러나지 않는다.

## refresh token cookie 는 같은 domain 에서만 저장된다

account server 는 WebGL 에만 refresh token 을 `HttpOnly` cookie 로 내려준다
(`Path=/api/auth`, `SameSite=Lax`). 그래서 account server 와 같은 domain 에서 서비스되는
page 에서만 그 cookie 가 저장된다.

| page | cookie | 로그인 유지 |
| --- | --- | --- |
| `arcanecasters.theevilent.com` | 저장된다 | 된다 |
| `html-classic.itch.zone` (itch.io 가 서비스하는 주소) | 저장되지 않는다 | 안 된다 |
| `apptive-game-team.github.io` (release play 주소) | 저장되지 않는다 | 안 된다 |
| `localhost` | 저장되지 않는다 | 안 된다 |

cookie 를 못 쓰는 page 에서 cookie 방식을 그대로 쓰면, 50분 주기 갱신이 매번 401 을 받아
`AuthSession` 이 경기 중에도 로그인 화면으로 돌려보낸다. access token 이 3600초라 오래 켜 두면
반드시 걸린다. 그래서 `RefreshTokenStoreSelector` 가 page 를 보고
`BrowserCookieRefreshTokenStore` 와 `InMemoryRefreshTokenStore` 를 가른다. 판정은
`Assets/Plugins/WebGL/RefreshToken.jslib` 의 `CanUseCookieDelivery` 이고, 기준은
`window.location.hostname` 과 account server host 의 registrable domain 비교다.

브라우저가 실제로 보는 것은 이 문서의 domain 이 아니라 **최상위 문서의 site** 다. 지금은 빌드가
올라가는 자리 모두 최상위 문서와 빌드가 같은 site 라 결과가 같다. `itch.io` 페이지 안에
`arcanecasters.theevilent.com` 을 iframe 으로 끼우는 배치를 만든다면 이 판정이 cookie 를 쓸 수
있다고 잘못 답하므로, 그때는 최상위 host 를 읽도록 고쳐야 한다.

## CORS 허용 목록에 없는 origin 은 호출 자체가 막힌다

account server 의 `cors.allowed-origins` 는 명시 목록이다. 목록에 없는 origin 에서 온 요청은
preflight 가 403 이라 로그인부터 실패한다. `itch.io` 가 빌드를 서비스하는
`https://html-classic.itch.zone` 은 목록에 없다.

빌드를 올릴 자리가 바뀌면 이 한 줄로 먼저 확인한다.

```bash
curl -sS -i -X OPTIONS https://account.theevilent.com/api/auth/login \
  -H "Origin: https://html-classic.itch.zone" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: content-type"
```

`access-control-allow-origin` 이 응답에 없으면 그 page 에서는 account server 를 못 부른다.
