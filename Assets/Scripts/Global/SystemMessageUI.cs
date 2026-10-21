using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

namespace Global
{
    public class SystemMessageUI : LocalSingletonObject<SystemMessageUI>
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private float duration = 5f;
        [SerializeField] private float replaceDelay = 1f;

        private sealed class Message
        {
            public string Text;
            public bool SceneScoped;
            public SystemMessageUI Owner;
            public Scene Scene;
            public bool IsValid => !SceneScoped ||
                (Owner != null && Owner.isActiveAndEnabled && Scene.IsValid() && Scene.isLoaded && Owner.gameObject.scene == Scene);
        }

        private static readonly Queue<Message> messageQueue = new Queue<Message>();
        private Coroutine messageRoutine;

        protected override void Awake()
        {
            base.Awake();
            if (messagePanel == null)
                messagePanel = messageText.transform.parent.gameObject;
            messagePanel.SetActive(false);
        }

        private void OnDisable()
        {
            if (messageRoutine != null)
                StopCoroutine(messageRoutine);
            messageRoutine = null;
            if (messagePanel != null)
                messagePanel.SetActive(false);
        }

        private void OnEnable()
        {
            StartMessages();
        }

        public void ShowMessage(LocalizedString localizedString, Action callback = null)
        {
            localizedString.GetLocalizedStringAsync().Completed += handle =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    ShowMessage(handle.Result);
                callback?.Invoke();
            };
        }

        public void ShowMessage(string msg)
        {
            messageQueue.Enqueue(new Message { Text = msg });
            StartMessages();
        }

        // Lobby status belongs to its originating scene, including delayed localization results.
        public void ShowSceneMessage(LocalizedString localizedString)
        {
            Scene ownerScene = gameObject.scene;
            localizedString.GetLocalizedStringAsync().Completed += handle =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    EnqueueSceneMessage(handle.Result, ownerScene);
            };
        }

        public void ShowSceneMessage(string msg)
        {
            EnqueueSceneMessage(msg, gameObject.scene);
        }

        private void EnqueueSceneMessage(string msg, Scene ownerScene)
        {
            var message = new Message { Text = msg, SceneScoped = true, Owner = this, Scene = ownerScene };
            if (!message.IsValid) return;
            messageQueue.Enqueue(message);
            StartMessages();
        }

        private void StartMessages()
        {
            // Generic errors may finish localizing after their original UI was destroyed.
            // They remain queued for the next scene's UI.
            while (messageQueue.Count > 0 && !messageQueue.Peek().IsValid)
                messageQueue.Dequeue();
            if (this != null && isActiveAndEnabled && messageQueue.Count > 0 && messageRoutine == null)
                messageRoutine = StartCoroutine(ProcessMessageQueue());
        }

        private IEnumerator ProcessMessageQueue()
        {
            while (messageQueue.Count > 0)
            {
                Message message = messageQueue.Peek();
                if (!message.IsValid)
                {
                    messageQueue.Dequeue();
                    continue;
                }

                messageText.text = message.Text;
                messagePanel.SetActive(true);
                float timer = 0f;
                while (timer < duration && message.IsValid)
                {
                    timer += Time.deltaTime;
                    if (messageQueue.Count > 1 && timer >= replaceDelay)
                        break;
                    yield return null;
                }
                messageQueue.Dequeue();
            }

            messagePanel.SetActive(false);
            messageText.text = "";
            messageRoutine = null;
        }
    }
}
