using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class SceneScopedMessageTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);

        [UnityTest]
        public IEnumerator LobbyMessagesExpireButCrossSceneErrorsSurvive()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();

            Type uiType = RuntimeType("Global.SystemMessageUI");
            object queue = uiType.GetField("messageQueue", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            MethodInfo clear = queue.GetType().GetMethod("Clear");
            clear.Invoke(queue, null);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SystemMessage.prefab");
            GameObject first = null;
            GameObject second = null;
            try
            {
                first = Object.Instantiate(prefab);
                Component oldUi = first.GetComponent(uiType);
                Scene oldScene = first.scene;
                MethodInfo scoped = uiType.GetMethod("ShowSceneMessage", new[] { typeof(string) });
                MethodInfo generic = uiType.GetMethod("ShowMessage", new[] { typeof(string) });
                MethodInfo delayed = uiType.GetMethod("EnqueueSceneMessage", PrivateInstance);
                scoped.Invoke(oldUi, new object[] { "Matching in progress" });
                scoped.Invoke(oldUi, new object[] { "Queued matching status" });
                generic.Invoke(oldUi, new object[] { "Connection error" });
                ((Behaviour)oldUi).enabled = false;
                delayed.Invoke(oldUi, new object[] { "Disabled owner translation", oldScene });
                Assert.AreEqual(3, ((ICollection)queue).Count,
                    "Disabling the owner must reject late localization without clearing generic errors.");
                Assert.IsNull(uiType.GetField("messageRoutine", PrivateInstance).GetValue(oldUi));
                var oldPanel = (GameObject)uiType.GetField("messagePanel", PrivateInstance).GetValue(oldUi);
                Assert.IsFalse(oldPanel.activeSelf);
                Object.DestroyImmediate(first);
                first = null;

                // This is the same completion path used by delayed localization.
                delayed.Invoke(oldUi, new object[] { "Late matching translation", oldScene });
                Assert.AreEqual(3, ((ICollection)queue).Count, "Destroyed owners must reject delayed translations.");

                second = Object.Instantiate(prefab);
                Component currentUi = second.GetComponent(uiType);
                yield return null;
                var text = (TMP_Text)uiType.GetField("messageText", PrivateInstance).GetValue(currentUi);
                Assert.AreEqual("Connection error", text.text,
                    "The next scene must skip both displayed and queued lobby status, preserving generic errors.");
                Assert.AreEqual(1, ((ICollection)queue).Count);
            }
            finally
            {
                if (first != null) Object.DestroyImmediate(first);
                if (second != null) Object.DestroyImmediate(second);
                clear.Invoke(queue, null);
            }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator OnlyExpiredMessagesDoNotBlockNewSceneNotifications()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            Type uiType = RuntimeType("Global.SystemMessageUI");
            object queue = uiType.GetField("messageQueue", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            MethodInfo clear = queue.GetType().GetMethod("Clear");
            clear.Invoke(queue, null);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SystemMessage.prefab");
            GameObject first = null;
            GameObject second = null;
            try
            {
                MethodInfo scoped = uiType.GetMethod("ShowSceneMessage", new[] { typeof(string) });
                first = Object.Instantiate(prefab);
                scoped.Invoke(first.GetComponent(uiType), new object[] { "Matching in progress" });
                Object.DestroyImmediate(first);
                first = null;
                second = Object.Instantiate(prefab);
                yield return null;
                Assert.AreEqual(0, ((ICollection)queue).Count);
                Component currentUi = second.GetComponent(uiType);
                scoped.Invoke(currentUi, new object[] { "Current scene status" });
                var text = (TMP_Text)uiType.GetField("messageText", PrivateInstance).GetValue(currentUi);
                var panel = (GameObject)uiType.GetField("messagePanel", PrivateInstance).GetValue(currentUi);
                Assert.IsTrue(panel.activeSelf);
                Assert.AreEqual("Current scene status", text.text);
            }
            finally
            {
                if (first != null) Object.DestroyImmediate(first);
                if (second != null) Object.DestroyImmediate(second);
                clear.Invoke(queue, null);
            }
            yield return new ExitPlayMode();
        }
    }
}
