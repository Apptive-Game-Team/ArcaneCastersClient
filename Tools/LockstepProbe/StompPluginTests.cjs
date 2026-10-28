const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const messages = [], clients = [], library = {};
const context = vm.createContext({
    LibraryManager: { library }, mergeInto: Object.assign, UTF8ToString: value => value,
    SendMessage: (...args) => messages.push(args), console,
    WebSocket: function () { this.close = () => {}; },
    Stomp: { over: ws => {
        const client = { ws, connected: true,
            connect(headers, success, error) { this.success = success; this.error = error; },
            disconnect(callback) { this.closed = callback; },
            subscribe(topic, callback) { this.message = callback; },
            send(topic, headers, body) { this.sent = { topic, body }; }
        };
        clients.push(client); return client;
    } }
});
vm.runInContext(fs.readFileSync(path.join(__dirname, '../../Assets/Plugins/WebGL/StompPlugin.jslib'), 'utf8'), context);
context.stompState = library.$stompState;
library.ConnectStompSocket('ws://local', 'test', 'First');
clients[0].success({ headers: {} });
library.SubscribeStomp('/game/lockstep/one', 'one');
clients[0].message({ body: 'frame0' });
assert.equal(messages.at(-1)[0], 'First');
library.DisconnectStomp();
library.ConnectStompSocket('ws://local', 'test', 'Second');
const before = messages.length;
clients[0].closed(); clients[0].success({ headers: {} }); clients[0].error('late'); clients[0].message({ body: 'late' });
assert.equal(messages.length, before, 'Old socket callbacks must not reach a new connection');
clients[1].success({ headers: {} });
assert.deepEqual(messages.at(-1), ['Second', 'OnConnected', '{}']);
clients[1].error({ command: 'ERROR', toString: () => 'rejected' });
assert.deepEqual(messages.at(-1), ['Second', 'OnRejected', 'rejected']);
clients[1].error('network unavailable');
assert.deepEqual(messages.at(-1), ['Second', 'OnError', 'network unavailable']);
library.SendStomp('/app/lockstep/one/input', 'batch');
assert.deepEqual(clients[1].sent, { topic: '/app/lockstep/one/input', body: 'batch' });
console.log('PASS WebGL STOMP: callback routing, stale socket rejection, protocol vs network errors');
