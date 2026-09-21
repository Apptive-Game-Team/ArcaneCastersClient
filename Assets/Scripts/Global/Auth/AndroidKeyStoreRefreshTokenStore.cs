#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Text;
using UnityEngine;

namespace Global.Auth
{
    /// <summary>
    /// AndroidKeyStore 안의 AES key 로 refresh token 을 암호화하고, 암호문과 initialization
    /// vector 를 Base64 로 <see cref="PlayerPrefs"/> 에 둔다. key 는 하드웨어가 지키는 저장소를
    /// 벗어나지 않으므로 PlayerPrefs 값만 빼내도 복호화할 수 없다.
    ///
    /// androidx.security:security-crypto 를 쓰지 않고 java.security / javax.crypto 를 직접 부르는
    /// 이유는 gradle 의존성 때문이다. 그 라이브러리를 넣으려면 mainTemplate.gradle 이 있어야 하는데
    /// 이 환경에서는 Unity Editor 를 띄워 생성할 수 없다. 여기서 부르는 class 는 전부 Android
    /// 플랫폼에 이미 들어 있다.
    /// </summary>
    public sealed class AndroidKeyStoreRefreshTokenStore : IRefreshTokenStore
    {
        private const string KeyStoreProvider = "AndroidKeyStore";
        private const string KeyAlias = "dev.yunseong.ac.client.refreshToken";
        private const string Transformation = "AES/GCM/NoPadding";

        private const string CipherTextPreference = "auth.refreshToken.cipherText";
        private const string InitializationVectorPreference = "auth.refreshToken.iv";

        /// <summary>GCM authentication tag 길이(bit). 복호화 때 암호화와 같은 값을 줘야 한다.</summary>
        private const int AuthenticationTagBits = 128;

        public RefreshTokenDelivery Delivery => RefreshTokenDelivery.Body;

        public bool TryRead(out string refreshToken)
        {
            refreshToken = null;

            string cipherTextBase64 = PlayerPrefs.GetString(CipherTextPreference, string.Empty);
            string initializationVectorBase64 = PlayerPrefs.GetString(InitializationVectorPreference, string.Empty);
            if (cipherTextBase64.Length == 0 || initializationVectorBase64.Length == 0) return false;

            try
            {
                byte[] cipherText = Convert.FromBase64String(cipherTextBase64);
                byte[] initializationVector = Convert.FromBase64String(initializationVectorBase64);

                using (AndroidJavaObject keyStore = OpenKeyStore())
                {
                    if (!keyStore.Call<bool>("containsAlias", KeyAlias)) return false;

                    // getKey(String alias, char[] password) 의 password 는 AndroidKeyStore 구현이
                    // 쓰지 않는다. null 대신 빈 배열을 주는 이유는 JNI signature 다. Unity 는 인자의
                    // C# 타입으로 signature 를 만들기 때문에 null 을 주면 [C 가 아니라
                    // Ljava/lang/Object; 로 찾는다.
                    using (AndroidJavaObject key = keyStore.Call<AndroidJavaObject>("getKey", KeyAlias, new char[0]))
                    using (AndroidJavaObject cipher = CreateCipher())
                    using (AndroidJavaObject parameterSpec = new AndroidJavaObject(
                        "javax.crypto.spec.GCMParameterSpec", AuthenticationTagBits, initializationVector))
                    {
                        cipher.Call("init", GetCipherMode("DECRYPT_MODE"), key, parameterSpec);
                        byte[] plainText = cipher.Call<byte[]>("doFinal", cipherText);
                        refreshToken = Encoding.UTF8.GetString(plainText);
                        Array.Clear(plainText, 0, plainText.Length);
                        return refreshToken.Length > 0;
                    }
                }
            }
            catch (Exception exception)
            {
                // key 가 지워졌거나 tag 검증에 실패하면 복호화는 다시 시도해도 실패한다.
                // 남은 값을 지워서 다음 실행이 같은 오류를 반복하지 않게 한다.
                WDebug.LogError($"[RefreshToken] AndroidKeyStore 복호화 실패: {exception.Message}");
                Clear();
                refreshToken = null;
                return false;
            }
        }

        public void Save(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                Clear();
                return;
            }

            try
            {
                byte[] plainText = Encoding.UTF8.GetBytes(refreshToken);
                using (AndroidJavaObject key = LoadOrCreateKey())
                using (AndroidJavaObject cipher = CreateCipher())
                {
                    cipher.Call("init", GetCipherMode("ENCRYPT_MODE"), key);
                    byte[] cipherText = cipher.Call<byte[]>("doFinal", plainText);
                    byte[] initializationVector = cipher.Call<byte[]>("getIV");
                    Array.Clear(plainText, 0, plainText.Length);

                    PlayerPrefs.SetString(CipherTextPreference, Convert.ToBase64String(cipherText));
                    PlayerPrefs.SetString(InitializationVectorPreference, Convert.ToBase64String(initializationVector));
                    PlayerPrefs.Save();
                }
            }
            catch (Exception exception)
            {
                WDebug.LogError($"[RefreshToken] AndroidKeyStore 암호화 실패: {exception.Message}");
            }
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(CipherTextPreference);
            PlayerPrefs.DeleteKey(InitializationVectorPreference);
            PlayerPrefs.Save();

            try
            {
                using (AndroidJavaObject keyStore = OpenKeyStore())
                {
                    if (keyStore.Call<bool>("containsAlias", KeyAlias)) keyStore.Call("deleteEntry", KeyAlias);
                }
            }
            catch (Exception exception)
            {
                // 암호문은 이미 지웠으므로 key 가 남아도 값이 새지는 않는다. 다음 Save 가 같은
                // alias 에 새 key 를 만든다.
                WDebug.LogWarning($"[RefreshToken] AndroidKeyStore key 삭제 실패: {exception.Message}");
            }
        }

        private static AndroidJavaObject OpenKeyStore()
        {
            using (AndroidJavaClass keyStoreClass = new AndroidJavaClass("java.security.KeyStore"))
            {
                AndroidJavaObject keyStore = keyStoreClass.CallStatic<AndroidJavaObject>("getInstance", KeyStoreProvider);
                // AndroidKeyStore 는 파일에서 읽지 않으므로 load 에 stream 없이 null 을 준다.
                keyStore.Call("load", (AndroidJavaObject)null);
                return keyStore;
            }
        }

        private static AndroidJavaObject CreateCipher()
        {
            using (AndroidJavaClass cipherClass = new AndroidJavaClass("javax.crypto.Cipher"))
            {
                return cipherClass.CallStatic<AndroidJavaObject>("getInstance", Transformation);
            }
        }

        private static int GetCipherMode(string fieldName)
        {
            using (AndroidJavaClass cipherClass = new AndroidJavaClass("javax.crypto.Cipher"))
            {
                return cipherClass.GetStatic<int>(fieldName);
            }
        }

        private static AndroidJavaObject LoadOrCreateKey()
        {
            using (AndroidJavaObject keyStore = OpenKeyStore())
            {
                if (keyStore.Call<bool>("containsAlias", KeyAlias))
                {
                    return keyStore.Call<AndroidJavaObject>("getKey", KeyAlias, new char[0]);
                }
            }

            return GenerateKey();
        }

        /// <summary>
        /// alias 에 AES-GCM key 를 새로 만든다. KeyGenerator 가 만든 key 는 AndroidKeyStore 에
        /// 바로 등록되므로 따로 저장할 필요가 없다.
        /// </summary>
        private static AndroidJavaObject GenerateKey()
        {
            using (AndroidJavaClass keyProperties = new AndroidJavaClass("android.security.keystore.KeyProperties"))
            using (AndroidJavaClass keyGeneratorClass = new AndroidJavaClass("javax.crypto.KeyGenerator"))
            {
                int purposes = keyProperties.GetStatic<int>("PURPOSE_ENCRYPT") | keyProperties.GetStatic<int>("PURPOSE_DECRYPT");
                string blockModeGcm = keyProperties.GetStatic<string>("BLOCK_MODE_GCM");
                string paddingNone = keyProperties.GetStatic<string>("ENCRYPTION_PADDING_NONE");
                string algorithmAes = keyProperties.GetStatic<string>("KEY_ALGORITHM_AES");

                using (AndroidJavaObject builder = new AndroidJavaObject(
                    "android.security.keystore.KeyGenParameterSpec$Builder", KeyAlias, purposes))
                {
                    // setBlockModes 와 setEncryptionPaddings 는 String... varargs 라 JNI 에서는
                    // String[] 하나를 받는다. object[] 로 한 번 더 감싸지 않으면 params 가 문자열을
                    // 인자 여러 개로 펼친다.
                    using (AndroidJavaObject withBlockMode = builder.Call<AndroidJavaObject>(
                        "setBlockModes", new object[] { new[] { blockModeGcm } }))
                    using (AndroidJavaObject withPadding = withBlockMode.Call<AndroidJavaObject>(
                        "setEncryptionPaddings", new object[] { new[] { paddingNone } }))
                    using (AndroidJavaObject specification = withPadding.Call<AndroidJavaObject>("build"))
                    {
                        AndroidJavaObject keyGenerator = keyGeneratorClass.CallStatic<AndroidJavaObject>(
                            "getInstance", algorithmAes, KeyStoreProvider);
                        keyGenerator.Call("init", specification);
                        AndroidJavaObject key = keyGenerator.Call<AndroidJavaObject>("generateKey");
                        keyGenerator.Dispose();
                        return key;
                    }
                }
            }
        }
    }
}
#endif
