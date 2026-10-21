using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace Global
{
    public class SystemMessageUI : LocalSingletonObject<SystemMessageUI>
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private float duration = 5f; 
        [SerializeField] private float replaceDelay = 1f; 

        private static readonly Queue<string> messageQueue = new Queue<string>();
        private Coroutine messageRoutine;
        private Coroutine transientRoutine;

        protected override void Awake()
        {
            base.Awake();
            if (messagePanel == null)
                messagePanel = messageText.transform.parent.gameObject;
            
            messagePanel.SetActive(false);
        }

        private void Start()
        {
            if (messageQueue.Count > 0 && messageRoutine == null)
            {
                messageRoutine = StartCoroutine(ProcessMessageQueue());
            }
        }

        public void ShowMessage(LocalizedString localizedString, Action callback = null)
        {
            localizedString.GetLocalizedStringAsync().Completed += handle =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    ShowMessage(handle.Result);
                }
                callback?.Invoke();
            };
        }
    
        public void ShowMessage(string msg)
        {
            ClearTransientMessage();
            messageQueue.Enqueue(msg);

            if (messageRoutine == null)
            {
                messageRoutine = StartCoroutine(ProcessMessageQueue());
            }
        }

        // Live telemetry must never enter the static queue that survives scene changes.
        public void ShowTransientMessage(string msg)
        {
            ClearTransientMessage();
            if (messageRoutine != null || messageQueue.Count > 0 || !isActiveAndEnabled)
                return;

            transientRoutine = StartCoroutine(DisplayTransientMessage(msg));
        }

        public void ClearTransientMessage()
        {
            if (transientRoutine == null) return;

            StopCoroutine(transientRoutine);
            transientRoutine = null;
            if (messageRoutine == null)
            {
                messagePanel.SetActive(false);
                messageText.text = "";
            }
        }

        private IEnumerator DisplayTransientMessage(string msg)
        {
            messageText.text = msg;
            messagePanel.SetActive(true);
            yield return new WaitForSecondsRealtime(duration);
            messagePanel.SetActive(false);
            messageText.text = "";
            transientRoutine = null;
        }

        private void OnDisable()
        {
            ClearTransientMessage();
        }

        private IEnumerator ProcessMessageQueue()
        {
            while (messageQueue.Count > 0)
            {
                string msg = messageQueue.Peek();
            
                messageText.text = msg;
                messagePanel.SetActive(true);

                float timer = 0f;
                float currentDuration = duration;

                while (timer < currentDuration)
                {
                    timer += Time.deltaTime;
                
                    if (messageQueue.Count > 1 && timer >= replaceDelay)
                    {
                        break; 
                    }
                
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
