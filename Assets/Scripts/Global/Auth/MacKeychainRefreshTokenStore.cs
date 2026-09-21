#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Global.Auth
{
    /// <summary>
    /// macOS Keychain 의 generic password item 하나에 refresh token 을 둔다.
    ///
    /// SecKeychainAddGenericPassword 계열은 macOS 10.10 에서 deprecated 로 표시됐고 Apple 은
    /// SecItemAdd / SecItemCopyMatching 을 권한다. 그런데도 옛 API 를 고른 이유는 interop 면적
    /// 이다. SecItem 계열은 CFDictionary, CFString, CFData 를 직접 만들고 CFRelease 까지 맞춰야
    /// 하지만, generic password 계열은 C 문자열과 길이만 넘기면 된다. 기능은 지금도 살아 있다.
    /// </summary>
    public sealed class MacKeychainRefreshTokenStore : IRefreshTokenStore
    {
        private const string SecurityFramework = "/System/Library/Frameworks/Security.framework/Security";
        private const string CoreFoundationFramework = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        private const string ServiceName = "dev.yunseong.ac.client";
        private const string AccountName = "refresh-token";

        private const int ErrSecSuccess = 0;
        private const int ErrSecItemNotFound = -25300;

        public RefreshTokenDelivery Delivery => RefreshTokenDelivery.Body;

        public bool TryRead(out string refreshToken)
        {
            refreshToken = null;

            byte[] service = Encoding.UTF8.GetBytes(ServiceName);
            byte[] account = Encoding.UTF8.GetBytes(AccountName);

            IntPtr passwordData = IntPtr.Zero;
            IntPtr itemReference = IntPtr.Zero;
            try
            {
                int status = SecKeychainFindGenericPassword(IntPtr.Zero,
                    (uint)service.Length, service,
                    (uint)account.Length, account,
                    out uint passwordLength, out passwordData, out itemReference);

                if (status == ErrSecItemNotFound) return false;
                if (status != ErrSecSuccess)
                {
                    WDebug.LogError($"[RefreshToken] Keychain find 실패, OSStatus={status}");
                    return false;
                }
                if (passwordData == IntPtr.Zero || passwordLength == 0) return false;

                byte[] password = new byte[passwordLength];
                Marshal.Copy(passwordData, password, 0, password.Length);
                refreshToken = Encoding.UTF8.GetString(password);
                Array.Clear(password, 0, password.Length);
                return true;
            }
            finally
            {
                // SecKeychainFindGenericPassword 가 준 버퍼는 SecKeychainItemFreeContent 로만
                // 돌려준다. 첫 인자 attrList 는 attribute 를 받지 않았으므로 NULL 이다.
                if (passwordData != IntPtr.Zero) SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
                if (itemReference != IntPtr.Zero) CFRelease(itemReference);
            }
        }

        public void Save(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                Clear();
                return;
            }

            byte[] service = Encoding.UTF8.GetBytes(ServiceName);
            byte[] account = Encoding.UTF8.GetBytes(AccountName);
            byte[] password = Encoding.UTF8.GetBytes(refreshToken);

            IntPtr existingItem = IntPtr.Zero;
            try
            {
                int findStatus = FindItemOnly(service, account, out existingItem);
                if (findStatus == ErrSecSuccess && existingItem != IntPtr.Zero)
                {
                    // 같은 service/account 로 Add 를 다시 부르면 errSecDuplicateItem 이 난다.
                    // rotation 마다 덮어써야 하므로 기존 item 의 데이터만 바꾼다.
                    int modifyStatus = SecKeychainItemModifyAttributesAndData(existingItem, IntPtr.Zero,
                        (uint)password.Length, password);
                    if (modifyStatus != ErrSecSuccess)
                    {
                        WDebug.LogError($"[RefreshToken] Keychain modify 실패, OSStatus={modifyStatus}");
                    }
                    return;
                }

                if (findStatus != ErrSecItemNotFound)
                {
                    WDebug.LogError($"[RefreshToken] Keychain find 실패, OSStatus={findStatus}");
                    return;
                }

                int addStatus = SecKeychainAddGenericPassword(IntPtr.Zero,
                    (uint)service.Length, service,
                    (uint)account.Length, account,
                    (uint)password.Length, password,
                    IntPtr.Zero);
                if (addStatus != ErrSecSuccess)
                {
                    WDebug.LogError($"[RefreshToken] Keychain add 실패, OSStatus={addStatus}");
                }
            }
            finally
            {
                if (existingItem != IntPtr.Zero) CFRelease(existingItem);
                Array.Clear(password, 0, password.Length);
            }
        }

        public void Clear()
        {
            byte[] service = Encoding.UTF8.GetBytes(ServiceName);
            byte[] account = Encoding.UTF8.GetBytes(AccountName);

            IntPtr itemReference = IntPtr.Zero;
            try
            {
                int findStatus = FindItemOnly(service, account, out itemReference);
                if (findStatus == ErrSecItemNotFound) return;
                if (findStatus != ErrSecSuccess || itemReference == IntPtr.Zero)
                {
                    WDebug.LogError($"[RefreshToken] Keychain find 실패, OSStatus={findStatus}");
                    return;
                }

                int deleteStatus = SecKeychainItemDelete(itemReference);
                if (deleteStatus != ErrSecSuccess)
                {
                    WDebug.LogError($"[RefreshToken] Keychain delete 실패, OSStatus={deleteStatus}");
                }
            }
            finally
            {
                if (itemReference != IntPtr.Zero) CFRelease(itemReference);
            }
        }

        /// <summary>
        /// 값은 필요 없고 item reference 만 필요할 때 부른다. passwordLength 와 passwordData 에
        /// NULL 을 주면 Security framework 가 버퍼를 만들지 않으므로 free 할 것도 없다.
        /// </summary>
        private static int FindItemOnly(byte[] service, byte[] account, out IntPtr itemReference)
        {
            return SecKeychainFindGenericPasswordItemOnly(IntPtr.Zero,
                (uint)service.Length, service,
                (uint)account.Length, account,
                IntPtr.Zero, IntPtr.Zero, out itemReference);
        }

        // OSStatus SecKeychainAddGenericPassword(SecKeychainRef keychain,
        //     UInt32 serviceNameLength, const char *serviceName,
        //     UInt32 accountNameLength, const char *accountName,
        //     UInt32 passwordLength, const void *passwordData,
        //     SecKeychainItemRef *itemRef);
        // keychain 에 NULL 을 주면 기본 keychain 을 쓴다. itemRef 를 받으면 CFRelease 해야 하므로
        // NULL 을 준다.
        [DllImport(SecurityFramework)]
        private static extern int SecKeychainAddGenericPassword(IntPtr keychain,
            uint serviceNameLength, byte[] serviceName,
            uint accountNameLength, byte[] accountName,
            uint passwordLength, byte[] passwordData,
            IntPtr itemReference);

        // OSStatus SecKeychainFindGenericPassword(CFTypeRef keychainOrArray,
        //     UInt32 serviceNameLength, const char *serviceName,
        //     UInt32 accountNameLength, const char *accountName,
        //     UInt32 *passwordLength, void **passwordData,
        //     SecKeychainItemRef *itemRef);
        [DllImport(SecurityFramework)]
        private static extern int SecKeychainFindGenericPassword(IntPtr keychainOrArray,
            uint serviceNameLength, byte[] serviceName,
            uint accountNameLength, byte[] accountName,
            out uint passwordLength, out IntPtr passwordData,
            out IntPtr itemReference);

        // 위와 같은 함수인데 passwordLength/passwordData 에 NULL 을 넘기려고 따로 선언한다.
        [DllImport(SecurityFramework, EntryPoint = "SecKeychainFindGenericPassword")]
        private static extern int SecKeychainFindGenericPasswordItemOnly(IntPtr keychainOrArray,
            uint serviceNameLength, byte[] serviceName,
            uint accountNameLength, byte[] accountName,
            IntPtr passwordLength, IntPtr passwordData,
            out IntPtr itemReference);

        // OSStatus SecKeychainItemFreeContent(SecKeychainAttributeList *attrList, void *data);
        [DllImport(SecurityFramework)]
        private static extern int SecKeychainItemFreeContent(IntPtr attributeList, IntPtr data);

        // OSStatus SecKeychainItemDelete(SecKeychainItemRef itemRef);
        [DllImport(SecurityFramework)]
        private static extern int SecKeychainItemDelete(IntPtr itemReference);

        // OSStatus SecKeychainItemModifyAttributesAndData(SecKeychainItemRef itemRef,
        //     const SecKeychainAttributeList *attrList, UInt32 length, const void *data);
        // attrList 가 NULL 이면 attribute 는 그대로 두고 데이터만 바꾼다.
        [DllImport(SecurityFramework)]
        private static extern int SecKeychainItemModifyAttributesAndData(IntPtr itemReference,
            IntPtr attributeList, uint length, byte[] data);

        // void CFRelease(CFTypeRef cf);
        [DllImport(CoreFoundationFramework)]
        private static extern void CFRelease(IntPtr reference);
    }
}
#endif
