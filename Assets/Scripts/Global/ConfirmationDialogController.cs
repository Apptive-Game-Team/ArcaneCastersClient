using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Global
{
    public class ConfirmationDialogController : MonoBehaviour, IConfirmationDialog
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private UnityEngine.UI.Button confirmButton;
        [SerializeField] private UnityEngine.UI.Button cancelButton;

        private Action onConfirm;
        private Action onCancel;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            EnsureRoot();
            root.SetActive(false);
        }

        public void Show(LocalizedString message, Action onConfirm, Action onCancel = null)
        {
            EnsureRoot();
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;

            SetMessage(message);
            BindButton(confirmButton, Confirm);
            BindButton(cancelButton, Cancel);
            root.SetActive(true);
        }

        private void SetMessage(LocalizedString message)
        {
            if (messageText == null || message == null)
            {
                return;
            }

            message.GetLocalizedStringAsync().Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    messageText.text = handle.Result;
                }
            };
        }

        private void EnsureRoot()
        {
            if (root == null)
            {
                root = gameObject;
            }
        }

        private static void BindButton(UnityEngine.UI.Button button, Action callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke());
        }

        private void Confirm()
        {
            Action callback = onConfirm;
            Close();
            callback?.Invoke();
        }

        public void Cancel()
        {
            Action callback = onCancel;
            Close();
            callback?.Invoke();
        }

        private void Close()
        {
            root.SetActive(false);
            onConfirm = null;
            onCancel = null;
        }
    }
}
