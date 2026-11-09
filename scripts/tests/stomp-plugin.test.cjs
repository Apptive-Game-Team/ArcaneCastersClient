const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');

// WebGL template이 불러오는 실제 stomp.js 2.3.3을 가짜 WebSocket 위에서 실행한다.
function runtime() {
  const sockets = [];
  const messages = [];
  const timers = new Map();
  let timerId = 0;
  let now = 0;
  class Socket {
    constructor(url) {
      this.url = url;
      this.readyState = 0;
      this.sent = [];
      sockets.push(this);
    }
    send(data) {
      if (this.readyState !== 1) throw new Error('Socket is not open');
      this.sent.push(data);
    }
    open() { this.readyState = 1; this.onopen?.({}); }
    receive(data) { this.onmessage?.({ data }); }
    close() { this.readyState = 3; this.onclose?.({ code: 1000 }); }
  }
  const context = vm.createContext({
    console, WebSocket: Socket, TextEncoder, TextDecoder,
    Date: class extends Date {
      constructor(...args) { super(...(args.length ? args : [now])); }
      static now() { return now; }
    },
    setInterval: (callback, delay) => {
      timers.set(++timerId, { callback, delay, next: now + delay });
      return timerId;
    },
    clearInterval: id => timers.delete(id),
    setTimeout, clearTimeout,
    UTF8ToString: value => value,
    SendMessage: (...args) => messages.push(args),
    LibraryManager: { library: {} },
    mergeInto: (target, values) => Object.assign(target, values),
  });
  context.window = context;
  vm.runInContext(fs.readFileSync(path.join(__dirname, 'fixtures/stomp-2.3.3.js'), 'utf8'), context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../../Assets/Plugins/WebGL/StompPlugin.jslib'), 'utf8'), context);
  context.stompState = context.LibraryManager.library.$stompState;
  const api = context.LibraryManager.library;
  function connect(target = 'GameServerConnection') {
    api.ConnectStompSocket('wss://test.invalid/ws?token=private-test-token', 'private-test-token', target);
    return sockets.at(-1);
  }
  function connected(socket, heartbeat = '0,0') {
    socket.open();
    socket.receive(`CONNECTED\nversion:1.2\nheart-beat:${heartbeat}\n\n\0`);
  }
  function advanceTime(milliseconds) {
    now += milliseconds;
    for (const timer of timers.values()) {
      if (timer.next <= now) { timer.next = now + timer.delay; timer.callback(); }
    }
  }
  return { api, context, sockets, messages, timers, connect, connected, advanceTime };
}

test('지연된 handshake를 유지하고 전달받은 GameObject로 연결을 알린다', () => {
  const r = runtime();
  const socket = r.connect('PersistentConnection-7');
  // Plugin에는 2초마다 기존 handshake를 닫는 timer가 없어야 한다.
  r.advanceTime(3500);
  assert.equal(socket.readyState, 0);
  r.connected(socket);
  assert.deepEqual(r.messages.map(m => m.slice(0, 2)), [['PersistentConnection-7', 'OnConnected']]);
  assert.equal(r.context.stompState.client.connected, true);
});

test('이전 연결의 connected와 error 및 구독 callback이 새 연결에 도달하지 않는다', () => {
  const r = runtime();
  const old = r.connect('old-host');
  r.connected(old);
  r.api.SubscribeStomp('/frames', 'frame-sub', 'old-host');
  const oldClient = r.context.stompState.client;
  const connected = oldClient.connectCallback;
  const subscription = oldClient.subscriptions['frame-sub'];
  const close = old.onclose;
  const error = old.onerror;
  const current = r.connect('new-host');
  r.messages.length = 0;
  connected({ headers: {} });
  subscription({ body: 'stale body' });
  close({ code: 1006 });
  error({});
  assert.deepEqual(r.messages, []);
  r.connected(current);
  assert.equal(r.messages[0][0], 'new-host');
  assert.equal(r.context.stompState.client.connected, true);
});

test('현재 구독 메시지만 지정된 host로 전달한다', () => {
  const r = runtime();
  const socket = r.connect();
  r.connected(socket);
  r.api.SubscribeStomp('/frames', 'frame-sub', 'GameServerConnection');
  socket.receive('MESSAGE\nsubscription:frame-sub\nmessage-id:1\n\n{"type":"sync"}\0');
  const message = r.messages.at(-1);
  assert.equal(message[0], 'GameServerConnection');
  assert.equal(message[1], 'OnSubscriptionMessage');
  assert.deepEqual(JSON.parse(message[2]), { id: 'frame-sub', body: '{"type":"sync"}' });
});

test('서버 종료는 stomp.js cleanup을 실행하고 중복 오류 없이 종료를 알린다', () => {
  const r = runtime();
  const socket = r.connect();
  r.connected(socket, '10000,10000');
  assert.equal(r.timers.size, 2);
  r.messages.length = 0;
  socket.close();
  assert.equal(r.context.stompState.client.connected, false);
  assert.equal(r.timers.size, 0);
  assert.deepEqual(r.messages, [['GameServerConnection', 'OnDisconnected', 'WebSocket closed (1000)']]);
  assert.ok(!JSON.stringify(r.messages).includes('private-test-token'));
});

test('연결 중과 연결 완료 뒤의 의도한 종료는 callback을 보내지 않는다', () => {
  for (const opened of [false, true]) {
    const r = runtime();
    const socket = r.connect('destroyed-host');
    if (opened) r.connected(socket);
    const lateClose = socket.onclose;
    const lateError = socket.onerror;
    r.messages.length = 0;
    r.api.DisconnectStomp('destroyed-host');
    lateClose({ code: 1000 });
    lateError({});
    r.api.DisconnectStomp('destroyed-host');
    assert.deepEqual(r.messages, []);
    assert.equal(r.context.stompState.client, null);
    assert.equal(r.context.stompState.callbackObject, null);
    assert.equal(socket.readyState, 3);
  }
});

test('프레임 끝의 옛 host 종료와 전송이 새 host 연결을 건드리지 않는다', () => {
  const r = runtime();
  r.connected(r.connect('old-host'));
  const currentSocket = r.connect('new-host');
  r.connected(currentSocket);
  r.api.SubscribeStomp('/frames', 'frame-sub', 'new-host');
  const currentClient = r.context.stompState.client;
  const generation = r.context.stompState.generation;
  const sentCount = currentSocket.sent.length;
  r.messages.length = 0;
  r.api.DisconnectStomp('old-host');
  r.api.SubscribeStomp('/stale', 'stale-sub', 'old-host');
  r.api.UnsubscribeStomp('frame-sub', 'old-host');
  r.api.SendStomp('/input', 'stale input', 'old-host');
  assert.equal(currentSocket.readyState, 1);
  assert.equal(currentSocket.sent.length, sentCount);
  assert.equal(r.context.stompState.client, currentClient);
  assert.equal(r.context.stompState.generation, generation);
  assert.ok(currentClient.subscriptions['frame-sub']);
  assert.deepEqual(r.messages, []);
  r.api.DisconnectStomp('new-host');
  assert.equal(currentSocket.readyState, 3);
});
