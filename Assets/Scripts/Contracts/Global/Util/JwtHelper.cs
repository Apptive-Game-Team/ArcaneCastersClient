using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Global.Serialization;

namespace Global.Util
{
    /// <summary>
    /// Utility class for reading and verifying JWT token claims on the client side.
    ///
    /// IsAdmin() verifies the JWT signature using the RS256 public key from the account
    /// server (via <see cref="JwksService"/>) before inspecting the scope claim. When JWKS
    /// has not been fetched yet, the signature cannot be checked, so IsAdmin() returns
    /// false rather than trusting an unverified scope claim. All privileged operations
    /// must still be enforced server-side.
    ///
    /// IsGuest() does not verify the signature. It only decides whether to show a UI
    /// affordance (the guest-to-member conversion button, the guest logout warning); the
    /// server decides guest status for real from its own `is_guest` column and its own
    /// verified copy of the token, so a forged `guest` claim cannot grant anything through
    /// this read. Requiring verification here would also bring back the bug that this claim
    /// was added to fix: guest login does not pre-fetch JWKS (see
    /// <see cref="Global.AdminOnly"/>'s remarks on which flows do), so a signature-gated read
    /// would hide the conversion button from a genuine guest right after that flow, whenever
    /// JWKS has not finished loading yet.
    /// </summary>
    public static class JwtHelper
    {
        [Serializable]
        private class JwtPayload
        {
            public string scope;
            public bool guest;
        }

        [Serializable]
        private class JwtHeader
        {
            public string alg;
            public string kid;
        }

        /// <summary>
        /// Decodes a Base64URL-encoded segment into raw bytes.
        /// </summary>
        private static byte[] Base64UrlDecode(string base64Url)
        {
            string base64 = base64Url.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "=";  break;
            }
            return Convert.FromBase64String(base64);
        }

        /// <summary>
        /// Decodes the header of a JWT token without verifying the signature
        /// and returns the decoded JSON string.
        /// </summary>
        public static string DecodeHeader(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken))
                return null;

            string[] parts = jwtToken.Split('.');
            if (parts.Length != 3)
                return null;

            try
            {
                byte[] bytes = Base64UrlDecode(parts[0]);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Decodes the payload of a JWT token without verifying the signature
        /// and returns the decoded JSON string.
        /// </summary>
        public static string DecodePayload(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken))
                return null;

            string[] parts = jwtToken.Split('.');
            if (parts.Length != 3)
                return null;

            try
            {
                byte[] bytes = Base64UrlDecode(parts[1]);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Verifies the JWT signature using the RSA public key fetched from the
        /// account server via <see cref="JwksService"/>.
        /// Returns false when the signature is invalid or the required key is not
        /// found in the JWKS cache.
        /// </summary>
        public static bool VerifySignature(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken))
                return false;

            string[] parts = jwtToken.Split('.');
            if (parts.Length != 3)
                return false;

            string headerJson = DecodeHeader(jwtToken);
            if (string.IsNullOrEmpty(headerJson))
                return false;

            JwtHeader header;
            try
            {
                header = JsonCodec.Deserialize<JwtHeader>(headerJson);
            }
            catch (Exception)
            {
                return false;
            }

            if (header == null)
                return false;

            if (!string.IsNullOrEmpty(header.kid))
            {
                JwksKey key = JwksService.GetKey(header.kid);
                if (key == null)
                    return false;

                if (!IsSigningKey(key))
                {
                    WDebug.LogWarning($"[JwtHelper] Key '{key.kid}' is not a valid RS256 signing key (use={key.use}, alg={key.alg}).");
                    return false;
                }

                return VerifyRsaSignature(parts[0] + "." + parts[1], parts[2], key);
            }

            // No kid in header: try all cached keys
            return VerifyAgainstAllKeys(parts[0] + "." + parts[1], parts[2]);
        }

        /// <summary>
        /// Returns true when the JWKS key is intended for RS256 signature verification.
        /// Checks kty == "RSA", use == "sig", and alg == "RS256".
        /// Fields that are absent (empty) are treated as acceptable per the JWKS spec.
        /// </summary>
        private static bool IsSigningKey(JwksKey key)
        {
            if (key.kty != "RSA")
                return false;
            if (!string.IsNullOrEmpty(key.use) && key.use != "sig")
                return false;
            if (!string.IsNullOrEmpty(key.alg) && key.alg != "RS256")
                return false;
            return true;
        }

        private static bool VerifyAgainstAllKeys(string headerAndPayload, string signatureBase64Url)
        {
            foreach (JwksKey candidate in JwksService.GetAllKeys())
            {
                if (IsSigningKey(candidate) &&
                    VerifyRsaSignature(headerAndPayload, signatureBase64Url, candidate))
                    return true;
            }
            return false;
        }

        private static bool VerifyRsaSignature(string headerAndPayload, string signatureBase64Url, JwksKey key)
        {
            try
            {
                byte[] data      = Encoding.UTF8.GetBytes(headerAndPayload);
                byte[] signature = Base64UrlDecode(signatureBase64Url);
                byte[] modulus   = Base64UrlDecode(key.n);
                byte[] exponent  = Base64UrlDecode(key.e);

                using RSA rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus  = modulus,
                    Exponent = exponent
                });

                return rsa.VerifyData(
                    data, signature,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                WDebug.LogWarning($"[JwtHelper] RSA signature verification failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extracts the roles from the JWT payload's "scope" claim.
        /// The scope is a space-separated string, e.g.
        /// "WORDONLINE_ADMIN WORDONLINE_USER WORDONLINE_CICD SUPER_ADMIN".
        /// Returns an empty array if no scope is found.
        /// </summary>
        public static string[] ExtractRoles(string jwtToken)
        {
            string payloadJson = DecodePayload(jwtToken);
            if (string.IsNullOrEmpty(payloadJson))
                return Array.Empty<string>();

            try
            {
                JwtPayload payload = JsonCodec.Deserialize<JwtPayload>(payloadJson);
                if (payload == null || string.IsNullOrEmpty(payload.scope))
                    return Array.Empty<string>();

                return payload.scope.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception)
            {
                // Ignore malformed payloads
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Returns true when the JWT token's scope contains "WORDONLINE_ADMIN"
        /// or "SUPER_ADMIN" (case-insensitive) AND the signature verifies against
        /// the cached JWKS keys. When JWKS has not been fetched, the signature
        /// cannot be verified, so this returns false instead of trusting the
        /// scope claim alone.
        /// </summary>
        public static bool IsAdmin(string jwtToken)
        {
            if (!JwksService.IsFetched)
            {
                WDebug.LogWarning("[JwtHelper] JWKS not fetched; cannot verify JWT signature, so admin status is denied.");
                return false;
            }

            if (!VerifySignature(jwtToken))
                return false;

            string[] roles = ExtractRoles(jwtToken);
            foreach (string role in roles)
            {
                if (string.Equals(role, "WORDONLINE_ADMIN", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(role, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true when the JWT payload's "guest" claim is present and true. Absent,
        /// false, or a malformed payload all read as a real member — see the class remarks
        /// for why this is not signature-verified.
        /// </summary>
        public static bool IsGuest(string jwtToken)
        {
            string payloadJson = DecodePayload(jwtToken);
            if (string.IsNullOrEmpty(payloadJson))
                return false;

            try
            {
                JwtPayload payload = JsonCodec.Deserialize<JwtPayload>(payloadJson);
                return payload != null && payload.guest;
            }
            catch (Exception)
            {
                // A token that cannot be parsed must not read as a guest.
            }

            return false;
        }
    }
}
