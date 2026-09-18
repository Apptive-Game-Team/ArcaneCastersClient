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
  }
});
