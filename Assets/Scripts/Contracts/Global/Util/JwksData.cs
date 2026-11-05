using System;
using UnityEngine;

namespace Global.Util
{
    /// <summary>
    /// Represents a single key entry in a JSON Web Key Set (JWKS).
    /// </summary>
    [Serializable]
    public class JwksKey
    {
        public string kty; // Key type: "RSA"
        public string use; // 용도: 서명 검증 (예: "sig")
        public string alg; // 알고리즘: RS256
        public string kid; // 키 식별자 (보통 날짜나 해시값, 예: "2026-04-06-01")
        public string n;   // RSA modulus (Base64URL-encoded)
        public string e;   // RSA exponent (Base64URL-encoded)
    }

    /// <summary>
    /// Represents a JSON Web Key Set (JWKS) response from the account server.
    /// </summary>
    [Serializable]
    internal class JwksResponse
    {
        public JwksKey[] keys;
    }
}
