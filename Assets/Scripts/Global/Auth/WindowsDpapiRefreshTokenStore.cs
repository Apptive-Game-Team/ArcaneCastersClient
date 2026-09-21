#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Global.Auth
{
    /// <summary>
    /// Windows DPAPI 로 refresh token 을 암호화해서 <see cref="Application.persistentDataPath"/>
    /// 아래 파일에 둔다. key 는 로그인한 Windows 사용자 계정에 묶이므로 파일만 복사해 간 다른
    /// 사용자는 복호화하지 못한다.
    /// </summary>
    public sealed class WindowsDpapiRefreshTokenStore : IRefreshTokenStore
    {
        private const string FileName = "refresh_token.dpapi";

        /// <summary>사용자에게 prompt 를 띄우지 않는다. CRYPTPROTECT_UI_FORBIDDEN.</summary>
        private const uint CryptProtectUiForbidden = 0x1;

        public RefreshTokenDelivery Delivery => RefreshTokenDelivery.Body;

        public bool TryRead(out string refreshToken)
        {
            refreshToken = null;
            string path = ResolvePath();
            if (!File.Exists(path)) return false;

            byte[] cipherText;
            try
            {
                cipherText = File.ReadAllBytes(path);
            }
            catch (Exception exception)
            {
                WDebug.LogError($"[RefreshToken] DPAPI 파일을 읽지 못했다: {exception.Message}");
                return false;
            }

            if (!TryUnprotect(cipherText, out byte[] plainText)) return false;

            refreshToken = Encoding.UTF8.GetString(plainText);
            Array.Clear(plainText, 0, plainText.Length);
            return refreshToken.Length > 0;
        }

        public void Save(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                Clear();
                return;
            }

            byte[] plainText = Encoding.UTF8.GetBytes(refreshToken);
            try
            {
                if (!TryProtect(plainText, out byte[] cipherText)) return;
                File.WriteAllBytes(ResolvePath(), cipherText);
            }
            catch (Exception exception)
            {
                WDebug.LogError($"[RefreshToken] DPAPI 파일을 쓰지 못했다: {exception.Message}");
            }
            finally
            {
                Array.Clear(plainText, 0, plainText.Length);
            }
        }

        public void Clear()
        {
            try
            {
                string path = ResolvePath();
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception exception)
            {
                WDebug.LogError($"[RefreshToken] DPAPI 파일을 지우지 못했다: {exception.Message}");
            }
        }

        private static string ResolvePath()
        {
            return Path.Combine(Application.persistentDataPath, FileName);
        }

        private static bool TryProtect(byte[] plainText, out byte[] cipherText)
        {
            return TryTransform(plainText, true, out cipherText);
        }

        private static bool TryUnprotect(byte[] cipherText, out byte[] plainText)
        {
            return TryTransform(cipherText, false, out plainText);
        }

        /// <summary>
        /// CryptProtectData 와 CryptUnprotectData 는 인자 모양이 같고 출력 blob 을 LocalFree 로
        /// 돌려줘야 하는 것도 같아서 한 자리에서 처리한다. CRYPTPROTECT_LOCAL_MACHINE 은 쓰지
        /// 않는다. 그 flag 를 주면 같은 PC 의 다른 사용자도 복호화할 수 있다.
        /// </summary>
        private static bool TryTransform(byte[] input, bool protect, out byte[] output)
        {
            output = null;
            if (input == null || input.Length == 0) return false;

            DataBlob inputBlob = new DataBlob();
            DataBlob outputBlob = new DataBlob();
            try
            {
                inputBlob.DataPointer = Marshal.AllocHGlobal(input.Length);
                inputBlob.DataLength = (uint)input.Length;
                Marshal.Copy(input, 0, inputBlob.DataPointer, input.Length);

                bool succeeded = protect
                    ? CryptProtectData(ref inputBlob, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                        CryptProtectUiForbidden, out outputBlob)
                    : CryptUnprotectData(ref inputBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                        CryptProtectUiForbidden, out outputBlob);

                if (!succeeded)
                {
                    int error = Marshal.GetLastWin32Error();
                    WDebug.LogError($"[RefreshToken] DPAPI {(protect ? "protect" : "unprotect")} 실패, GetLastError={error}");
                    return false;
                }

                output = new byte[outputBlob.DataLength];
                Marshal.Copy(outputBlob.DataPointer, output, 0, output.Length);
                return true;
            }
            finally
            {
                if (inputBlob.DataPointer != IntPtr.Zero) Marshal.FreeHGlobal(inputBlob.DataPointer);
                // 출력 blob 의 버퍼는 crypt32 가 LocalAlloc 으로 잡아 주므로 LocalFree 로 돌려준다.
                if (outputBlob.DataPointer != IntPtr.Zero) LocalFree(outputBlob.DataPointer);
            }
        }

        /// <summary>DATA_BLOB. 필드 순서가 cbData, pbData 이므로 바꾸면 안 된다.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct DataBlob
        {
            public uint DataLength;
            public IntPtr DataPointer;
        }

        // BOOL CryptProtectData(DATA_BLOB *pDataIn, LPCWSTR szDataDescr, DATA_BLOB *pOptionalEntropy,
        //                       PVOID pvReserved, CRYPTPROTECT_PROMPTSTRUCT *pPromptStruct,
        //                       DWORD dwFlags, DATA_BLOB *pDataOut);
        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptProtectData(ref DataBlob dataIn, string dataDescription,
            IntPtr optionalEntropy, IntPtr reserved, IntPtr promptStruct, uint flags, out DataBlob dataOut);

        // BOOL CryptUnprotectData(DATA_BLOB *pDataIn, LPWSTR *ppszDataDescr, DATA_BLOB *pOptionalEntropy,
        //                         PVOID pvReserved, CRYPTPROTECT_PROMPTSTRUCT *pPromptStruct,
        //                         DWORD dwFlags, DATA_BLOB *pDataOut);
        // ppszDataDescr 은 optional out 이고, NULL 을 주면 crypt32 가 설명 문자열을 만들지 않는다.
        // 받으면 LocalFree 로 돌려줘야 하므로 받지 않는다.
        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptUnprotectData(ref DataBlob dataIn, IntPtr dataDescription,
            IntPtr optionalEntropy, IntPtr reserved, IntPtr promptStruct, uint flags, out DataBlob dataOut);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr handle);
    }
}
#endif
