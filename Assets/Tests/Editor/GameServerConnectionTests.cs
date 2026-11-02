using System;
using System.Collections;
using System.Reflection;
using Data;
using Global;
using Global.Stomp;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class GameServerConnectionTests
    {
        private GameObject owner;
        private GameObject gameplayObject;
        private User previousUser;
        private MatchedInfoDto previousMatch;
        private string previousToken;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!Application.isPlaying)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                yield return new EnterPlayMode();
            }
            previousUser = SceneContext.User;
            previousMatch = SceneContext.MatchInfo;
            previousToken = SceneContext.JwtToken;
            SceneContext.User = new User(7, "connection-test", "connection@example.test", 0);
            SceneContext.JwtToken = "test-token";
            owner = new GameObject("GameServerConnectionTests_Owner");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (gameplayObject != null) Object.Destroy(gameplayObject);
            yield return null;
            var current = typeof(GameServerConnection).GetField("current", BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null) as GameServerConnection;
            if (current != null) current.Close();
            if (owner != null) Object.Destroy(owner);
            SceneContext.User = previousUser;
            SceneContext.MatchInfo = previousMatch;
            SceneContext.JwtToken = previousToken;
            yield return null;
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator DelayedHandshake_DoesNotReplaceConnectionBeforeConnected()
        {
            var match = Match("delayed");
            var connection = CreateConnection(match, out var transport);
            bool? completed = null;
            connection.StartCoroutine(connection.Prepare(match, owner, result => completed = result));

            yield return new WaitForSecondsRealtime(2.2f);

            Assert.That(completed, Is.Null, "CONNECTED를 받기 전에는 준비가 끝나면 안 됩니다.");
            Assert.That(transport.ConnectCalls, Is.EqualTo(1));
            Assert.That(transport.DisconnectCalls, Is.Zero);
            transport.SignalConnected();
            yield return null;

            Assert.That(completed, Is.True);
            Assert.That(transport.ConnectCalls, Is.EqualTo(1));
            Assert.That(transport.DisconnectCalls, Is.Zero);
        }

        [UnityTest]
        public IEnumerator GameplayClaim_PreservesPreparedTransportAfterPreparingOwnerIsDestroyed()
        {
            var match = Match("handoff");
            var connection = CreateConnection(match, out var transport);
            transport.ConnectImmediately = true;
            bool completed = false;
            yield return connection.Prepare(match, owner, result => completed = result);
            Assert.That(completed, Is.True);

            connection.ClaimForGameplay();
            Object.Destroy(owner);
            yield return null;
            yield return null;

            var reused = GameServerConnection.ForMatch(Match("handoff"));
            Assert.That(reused, Is.SameAs(connection));
            Assert.That(reused.Transport, Is.SameAs(transport));
            Assert.That(transport.IsConnected, Is.True);
            Assert.That(transport.ConnectCalls, Is.EqualTo(1));
            Assert.That(transport.DisconnectCalls, Is.Zero);
        }

        [UnityTest]
        public IEnumerator GameplayConnector_StartUsesPreparedTransportAndDestroyClosesIt()
        {
            var match = Match("production-handoff");
            SceneContext.MatchInfo = match;
            var connection = CreateConnection(match, out var transport);
            transport.ConnectImmediately = true;
            yield return connection.Prepare(match, owner, _ => { });

            gameplayObject = new GameObject("GameServerConnectionTests_Gameplay");
            gameplayObject.AddComponent<GameScene.StompConnector>();
            Object.Destroy(owner);
            yield return null;
            yield return null;

            Assert.That(GameServerConnection.ForMatch(match), Is.SameAs(connection));
            Assert.That(connection.Transport, Is.SameAs(transport));
            Assert.That(transport.IsConnected, Is.True);
            Assert.That(transport.ConnectCalls, Is.EqualTo(1));
            Assert.That(transport.DisconnectCalls, Is.Zero);
            Assert.That(transport.SubscribeCalls, Is.EqualTo(1));
            Assert.That(transport.LastSubscribedTopic, Is.EqualTo("/game/production-handoff/frameInfos/7"));

            Object.Destroy(gameplayObject);
            yield return null;
            yield return null;

            Assert.That(connection == null, Is.True);
            Assert.That(transport.IsConnected, Is.False);
            Assert.That(transport.DisconnectCalls, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyedPreparingOwner_ClosesPendingConnection()
        {
            var match = Match("cancel-pending");
            var connection = CreateConnection(match, out var transport);
            connection.StartCoroutine(connection.Prepare(match, owner, _ => { }));
            Assert.That(transport.ConnectCalls, Is.EqualTo(1));

            Object.Destroy(owner);
            yield return null;
            yield return null;

            Assert.That(connection == null, Is.True);
            Assert.That(transport.DisconnectCalls, Is.GreaterThanOrEqualTo(1));
            Assert.That(transport.ConnectCalls, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyedOwnerWithoutGameplayClaim_ClosesConnectedTransport()
        {
            var match = Match("cancel-handoff");
            var connection = CreateConnection(match, out var transport);
            transport.ConnectImmediately = true;
            yield return connection.Prepare(match, owner, _ => { });
            Assert.That(transport.IsConnected, Is.True);

            Object.Destroy(owner);
            yield return null;
            yield return null;

            Assert.That(connection == null, Is.True);
            Assert.That(transport.IsConnected, Is.False);
            Assert.That(transport.DisconnectCalls, Is.GreaterThanOrEqualTo(1));
        }

        [UnityTest]
        public IEnumerator DifferentSession_ReplacesAndDisconnectsOldTransport()
        {
            var connection = CreateConnection(Match("old-session"), out var transport);
            var replacement = GameServerConnection.ForMatch(Match("new-session"));

            Assert.That(replacement, Is.Not.SameAs(connection));
            Assert.That(replacement.Transport, Is.Not.SameAs(transport));
            Assert.That(transport.DisconnectCalls, Is.EqualTo(1));
            yield return null;
            Assert.That(connection == null, Is.True);
        }

        [UnityTest]
        public IEnumerator FailedScenePreparation_KeepsCurrentSceneAndInvokesFailureOnce()
        {
            var match = Match("failed-entry");
            SceneContext.MatchInfo = match;
            CreateConnection(match, out var transport);
            transport.FailImmediately = true;
            int failures = 0;
            var scene = SceneManager.GetActiveScene();

            // 성공 경로로 잘못 진입하면 존재하지 않는 씬을 로드하므로 테스트가 실패합니다.
            yield return GameSceneEntry.PrepareAndLoad(owner, () => failures++, "MustNotLoadBeforeStompConnected");

            Assert.That(failures, Is.EqualTo(1));
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(scene.handle));
            Assert.That(transport.ConnectCalls, Is.EqualTo(GameServerConnection.MaxAttempts));
            Assert.That(transport.IsConnected, Is.False);
            Assert.That(transport.DisconnectCalls, Is.GreaterThanOrEqualTo(GameServerConnection.MaxAttempts));
        }

        [UnityTest]
        public IEnumerator ReplacedMatchDuringPreparation_DoesNotEnterGameSceneWithOldConnection()
        {
            var match = Match("old-entry");
            SceneContext.MatchInfo = match;
            var connection = CreateConnection(match, out var transport);
            transport.ConnectImmediately = true;
            int failures = 0;
            var scene = SceneManager.GetActiveScene();
            var entry = GameSceneEntry.PrepareAndLoad(owner, () => failures++, "MustNotLoadStaleMatch");
            Assert.That(entry.MoveNext(), Is.True);

            // 준비 도중 매칭 정보가 바뀌면 이전 연결이 성공해도 씬 전환을 취소해야 합니다.
            SceneContext.MatchInfo = Match("replacement-entry");
            yield return entry.Current;
            Assert.That(entry.MoveNext(), Is.False);

            Assert.That(failures, Is.EqualTo(1));
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(scene.handle));
            Assert.That(transport.ConnectCalls, Is.EqualTo(1));
            Assert.That(transport.DisconnectCalls, Is.EqualTo(1));
            Assert.That(transport.IsConnected, Is.False);
            yield return null;
            Assert.That(connection == null, Is.True);
        }

        private static MatchedInfoDto Match(string sessionId)
        {
            return new MatchedInfoDto { sessionId = sessionId, server = "https://game.example.test" };
        }

        private static GameServerConnection CreateConnection(MatchedInfoDto match, out FakeTransport transport)
        {
            var connection = GameServerConnection.ForMatch(match);
            transport = new FakeTransport();
            typeof(GameServerConnection).GetField("transport", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(connection, transport);
            return connection;
        }

        private sealed class FakeTransport : IStompTransport
        {
            public bool IsConnected { get; private set; }
            public bool ConnectImmediately;
            public bool FailImmediately;
            public int ConnectCalls;
            public int DisconnectCalls;
            public int SubscribeCalls;
            public string LastSubscribedTopic;
            public event Action Connected;
            public event Action<string> Disconnected;
            public event Action<string> Errored;
            public event Action<string, string> MessageReceived { add { } remove { } }

            public void Connect(string url, string token)
            {
                ConnectCalls++;
                if (FailImmediately) Errored?.Invoke("test connection rejected");
                else if (ConnectImmediately) SignalConnected();
            }

            public void SignalConnected()
            {
                IsConnected = true;
                Connected?.Invoke();
            }

            public void Disconnect()
            {
                DisconnectCalls++;
                IsConnected = false;
                Disconnected?.Invoke("test disconnect");
            }

            public void Subscribe(string topic, string subscriptionId)
            {
                SubscribeCalls++;
                LastSubscribedTopic = topic;
            }
            public void Unsubscribe(string subscriptionId) { }
            public void Send(string topic, string message) { }
        }
    }
}
