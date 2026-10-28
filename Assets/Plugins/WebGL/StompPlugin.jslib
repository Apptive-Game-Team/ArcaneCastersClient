// Emscripten은 이 파일을 빌드 시점에 평가하고 LibraryManager.library에 등록된 심볼만
// framework.js로 내보낸다. mergeInto 바깥의 최상위 var는 런타임 번들에 존재하지 않으므로
// 플러그인 상태는 반드시 라이브러리 심볼($ 접두사)로 두고 __deps로 끌어와야 한다.
mergeInto(LibraryManager.library, {

  // client: 지금 살아 있는 STOMP 연결.
  // generation: 지금 유효한 연결 세대. ConnectStompSocket과 DisconnectStomp가 세대를 올린다.
  //             옛 소켓이 뒤늦게 올리는 콜백을 걸러내는 데 쓴다.
  $stompState: {
    client: null,
    target: 'StompConnector',
    generation: 0
  },

  // STOMP 서버에 WebSocket으로 연결
  ConnectStompSocket__deps: ['$stompState'],
  ConnectStompSocket: function (urlPtr, tokenPtr, targetPtr) {
    const url = UTF8ToString(urlPtr);
    const token = UTF8ToString(tokenPtr);
    const target = UTF8ToString(targetPtr);
    stompState.target = target;

    // 세대를 먼저 올린다. 바로 아래에서 옛 소켓을 닫을 때 터지는 콜백은 옛 세대를
    // 들고 있으므로 여기서부터 걸러진다.
    const generation = ++stompState.generation;

    // 앞선 연결이 남아 있으면 반드시 먼저 닫는다. 닫지 않으면 stompState.client가 새 소켓을
    // 가리키는 동안 옛 소켓이 참조 없이 열린 채 남아 서버에 STOMP 세션이 누수되고,
    // 옛 소켓의 핸드셰이크가 뒤늦게 끝나면 IsConnected만 true가 된 채 구독과 전송은
    // 아직 연결되지 않은 새 소켓으로 라우팅된다.
    if (stompState.client) {
      const stale = stompState.client;
      stompState.client = null;
      try { stale.disconnect(); } catch (e) {}
      try { if (stale.ws) stale.ws.close(); } catch (e) {}
    }

    const socket = new WebSocket(url);
    const stompClient = Stomp.over(socket);
    stompState.client = stompClient;
    stompClient.debug = null;

    stompClient.connect(
      { Authorization: 'Bearer ' + token },
      function (frame) {
        if (generation !== stompState.generation) return;
        SendMessage(target, 'OnConnected', JSON.stringify(frame.headers));
      },
      function (error) {
        if (generation !== stompState.generation) return;
        console.error('STOMP error:', error);
        SendMessage(target, error && error.command === 'ERROR' ? 'OnRejected' : 'OnError', String(error));
      }
    );
  },

  // 토픽 구독 – 수신 메시지는 항상 OnSubscriptionMessage({ id, body })로 라우팅
  SubscribeStomp__deps: ['$stompState'],
  SubscribeStomp: function (topicPtr, subscriptionIdPtr) {
    const topic = UTF8ToString(topicPtr);
    const subscriptionId = UTF8ToString(subscriptionIdPtr);

    if (!stompState.client || !stompState.client.connected) {
      console.warn('SubscribeStomp: STOMP client is not connected.');
      SendMessage(stompState.target, 'OnError', 'STOMP client is not connected.');
      return;
    }

    const target = stompState.target;
    const generation = stompState.generation;
    stompState.client.subscribe(topic, function (message) {
      if (generation !== stompState.generation) return;
      var bodyStr;
      if (typeof message.body === 'string') {
        bodyStr = message.body;
      } else if (message.body && typeof message.body === 'object') {
        try { bodyStr = JSON.stringify(message.body); }
        catch (e) { bodyStr = String(message.body); }
      } else {
        bodyStr = String(message.body);
      }

      var payload = JSON.stringify({ id: subscriptionId, body: bodyStr });
      SendMessage(target, 'OnSubscriptionMessage', payload);
    }, { id: subscriptionId });
  },

  // 메시지 전송
  SendStomp__deps: ['$stompState'],
  SendStomp: function (topicPtr, messagePtr) {
    const topic = UTF8ToString(topicPtr);
    const message = UTF8ToString(messagePtr);

    if (!stompState.client || !stompState.client.connected) {
      console.warn('SendStomp: STOMP client is not connected.');
      SendMessage(stompState.target, 'OnError', 'STOMP client is not connected.');
      return;
    }

    stompState.client.send(topic, {}, message);
  },

  // 구독 해제
  UnsubscribeStomp__deps: ['$stompState'],
  UnsubscribeStomp: function (subscriptionIdPtr) {
    const subscriptionId = UTF8ToString(subscriptionIdPtr);
    if (stompState.client) {
      stompState.client.unsubscribe(subscriptionId);
    }
  },

  // 연결 종료
  DisconnectStomp__deps: ['$stompState'],
  DisconnectStomp: function () {
    if (!stompState.client) return;

    // 세대를 올려 종료 과정에서 터지는 옛 소켓 콜백을 막는다.
    const stale = stompState.client;
    stompState.client = null;
    const generation = ++stompState.generation;
    const target = stompState.target;

    try {
      stale.disconnect(function () {
        if (generation === stompState.generation) SendMessage(target, 'OnDisconnected', 'Disconnected');
      });
    } catch (e) {
      // 핸드셰이크 도중이면 DISCONNECT 프레임 전송이 실패한다. 소켓만 닫고 알린다.
      try { if (stale.ws) stale.ws.close(); } catch (closeError) {}
      if (generation === stompState.generation) SendMessage(target, 'OnDisconnected', 'Disconnected');
    }
  }
});
