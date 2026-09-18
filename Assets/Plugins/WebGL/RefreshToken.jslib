// refresh token 은 WebGL 에서 account server 가 심은 HttpOnly cookie 에 들어 있다.
// UnityWebRequest 에는 withCredentials 를 켜는 C# API 가 없어서 cross-origin 요청이 그 cookie 를
// 싣지 못한다. 그래서 브라우저 fetch 로 직접 보낸다.
//
// C# 쪽 계약 (Global.Auth.BrowserCredentialedPost 가 부른다):
//
//   [DllImport("__Internal")]
//   static extern void SendCredentialedPost(string url, string objectName, string callbackMethod);
//
// 응답은 objectName 이름의 GameObject 에 SendMessage 로 돌아온다. callbackMethod 는 부르는 쪽이
// 정하고, 인자는 JSON 문자열 하나다.
//
//   { "status": <HTTP status code, 숫자>, "body": "<응답 body, 문자열>" }
//
// HTTP status 가 아예 없는 network 실패는 status 를 0 으로 두고 body 에 오류 메시지를 담는다.
// 부르는 쪽은 status 0 과 4xx/5xx 를 모두 실패로 다루면 된다.
//
// 그리고 (Global.Auth.BrowserCookieRefreshTokenStore 가 부른다):
//
//   [DllImport("__Internal")]
//   static extern int CanUseCookieDelivery(string accountHost);
//
// 이 page 에서 account server 의 cookie 가 저장되면 1, 아니면 0 이다.
mergeInto(LibraryManager.library, {
  SendCredentialedPost: function (urlPtr, objectNamePtr, callbackMethodPtr) {
    var url = UTF8ToString(urlPtr);
    var objectName = UTF8ToString(objectNamePtr);
    var callbackMethod = UTF8ToString(callbackMethodPtr);

    function report(status, body) {
      SendMessage(objectName, callbackMethod, JSON.stringify({ status: status, body: body }));
    }

    fetch(url, {
      method: "POST",
      credentials: "include",
      headers: { "X-Token-Delivery": "cookie" }
    }).then(function (response) {
      return response.text().then(function (body) {
        report(response.status, body);
      });
    }).catch(function (error) {
      // 여기로 오는 것은 요청 자체가 실패한 경우다. CORS 거부, 오프라인, body 를 읽다 끊긴 경우가
      // 모두 섞여 있고 그중 어느 것도 HTTP status 를 주지 않는다.
      report(0, error && error.message ? error.message : String(error));
    });
  },

  // account server 와 같은 domain 에서 서비스되는 page 에서만 cookie 가 저장된다.
  // arcanecasters.theevilent.com 은 1 을, itch.io 가 서비스하는 html-classic.itch.zone 과 release
  // play 주소인 github.io 는 0 을 받는다.
  //
  // 엄밀히 말하면 브라우저는 SameSite 를 최상위 문서 기준으로 판정하는데, 여기서는 이 문서의
  // host 만 본다. 빌드가 올라가는 세 자리 모두 최상위 문서와 이 문서가 같은 site 라 결과가 같다.
  // itch.io 페이지 안에 arcanecasters.theevilent.com 을 다시 iframe 으로 끼우는 배치를 만든다면
  // 이 판정이 1 을 잘못 돌려주므로, 그때는 최상위 host 를 읽어야 한다.
  CanUseCookieDelivery: function (accountHostPtr) {
    var accountHost = UTF8ToString(accountHostPtr);

    // registrable domain 을 마지막 label 두 개로 근사한다. 비교 대상 한쪽이 언제나 account server
    // host 이므로, 이 근사가 두 host 를 같다고 보는 경우는 실제로 같은 site 인 경우뿐이다.
    function registrableDomain(hostname) {
      var labels = hostname.split(".");
      return labels.length <= 2 ? hostname : labels.slice(-2).join(".");
    }

    var pageHost = window.location.hostname;
    return registrableDomain(pageHost) === registrableDomain(accountHost) ? 1 : 0;
  }
});
