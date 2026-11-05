using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Global.Util;
using NUnit.Framework;

namespace WordOnline.Tests
{
    [TestFixture]
    public class JwtHelperIsAdminTests
    {
        private RSA _rsaKey;
        private JwksKey _jwksKey;
        private const string TestKid = "test-key-id-1";

        [SetUp]
        public void SetUp()
        {
            ResetForTesting();

            _rsaKey = RSA.Create(2048);
            RSAParameters rsaParams = _rsaKey.ExportParameters(false);

            _jwksKey = new JwksKey
            {
                kty = "RSA",
                use = "sig",
                alg = "RS256",
                kid = TestKid,
                n = Base64UrlEncode(rsaParams.Modulus),
                e = Base64UrlEncode(rsaParams.Exponent)
            };
        }

        [TearDown]
        public void TearDown()
        {
            ResetForTesting();
            _rsaKey?.Dispose();
            _rsaKey = null;
        }

        #region Helper Methods

        private static void ResetForTesting() => SetJwksKeys(null, false);

        private static void InjectKeysForTesting(IEnumerable<JwksKey> keys) => SetJwksKeys(keys, true);

        private static void SetJwksKeys(IEnumerable<JwksKey> keys, bool isFetched)
        {
            var keysField = typeof(JwksService).GetField("_cachedKeys", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = (Dictionary<string, JwksKey>)keysField.GetValue(null);
            dict.Clear();
            if (keys != null)
            {
                foreach (JwksKey key in keys)
                    dict[string.IsNullOrEmpty(key.kid) ? Guid.NewGuid().ToString() : key.kid] = key;
            }

            typeof(JwksService).GetField("_isFetched", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, isFetched);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private string CreateJwt(string kid, string scope, bool guest = false, RSA customRsa = null)
        {
            string headerJson = string.IsNullOrEmpty(kid)
                ? @"{""alg"":""RS256""}"
                : $"{{\"alg\":\"RS256\",\"kid\":\"{kid}\"}}";

            string guestJson = guest ? "true" : "false";
            string payloadJson = string.IsNullOrEmpty(scope)
                ? $"{{\"guest\":{guestJson}}}"
                : $"{{\"scope\":\"{scope}\",\"guest\":{guestJson}}}";

            string headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            string payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            string headerAndPayload = $"{headerB64}.{payloadB64}";

            RSA signingRsa = customRsa ?? _rsaKey;
            byte[] signatureBytes = signingRsa.SignData(
                Encoding.UTF8.GetBytes(headerAndPayload),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            string signatureB64 = Base64UrlEncode(signatureBytes);
            return $"{headerAndPayload}.{signatureB64}";
        }

        #endregion

        #region JWKS State Tests

        [Test]
        public void IsAdmin_WhenJwksNotFetched_ReturnsFalse()
        {
            // JwksService.IsFetched is false (ResetForTesting was called)
            string jwt = CreateJwt(TestKid, "WORDONLINE_ADMIN");

            Assert.IsFalse(JwksService.IsFetched);
            Assert.IsFalse(JwtHelper.IsAdmin(jwt));
        }

        #endregion

        #region IsAdmin Tests - Valid Signature

        [TestCase("WORDONLINE_ADMIN")]
        [TestCase("SUPER_ADMIN")]
        [TestCase("wordonline_admin")]
        [TestCase("super_admin")]
        [TestCase("WORDONLINE_USER WORDONLINE_ADMIN")]
        [TestCase("SUPER_ADMIN WORDONLINE_USER")]
        public void IsAdmin_WithValidSignatureAndAdminRole_ReturnsTrue(string scope)
        {
            InjectKeysForTesting(new[] { _jwksKey });
            string jwt = CreateJwt(TestKid, scope);

            Assert.IsTrue(JwtHelper.IsAdmin(jwt));
        }

        [TestCase("WORDONLINE_USER")]
        [TestCase("GUEST")]
        [TestCase("ADMINISTRATOR")]
        [TestCase("")]
        [TestCase(null)]
        public void IsAdmin_WithValidSignatureAndNonAdminRole_ReturnsFalse(string scope)
        {
            InjectKeysForTesting(new[] { _jwksKey });
            string jwt = CreateJwt(TestKid, scope);

            Assert.IsFalse(JwtHelper.IsAdmin(jwt));
        }

        [Test]
        public void IsAdmin_WithNoKidInHeader_MatchesAgainstAllKeys()
        {
            InjectKeysForTesting(new[] { _jwksKey });
            string jwt = CreateJwt(kid: null, scope: "WORDONLINE_ADMIN");

            Assert.IsTrue(JwtHelper.IsAdmin(jwt));
        }

        #endregion

        #region IsAdmin Tests - Invalid Signature & Key Mismatch

        [Test]
        public void IsAdmin_WithSignatureCreatedByDifferentKey_ReturnsFalse()
        {
            InjectKeysForTesting(new[] { _jwksKey });

            using RSA otherKey = RSA.Create(2048);
            string jwtSignedByOtherKey = CreateJwt(TestKid, "WORDONLINE_ADMIN", customRsa: otherKey);

            Assert.IsFalse(JwtHelper.IsAdmin(jwtSignedByOtherKey));
        }

        [Test]
        public void IsAdmin_WithModifiedPayload_ReturnsFalse()
        {
            InjectKeysForTesting(new[] { _jwksKey });
            string validJwt = CreateJwt(TestKid, "WORDONLINE_ADMIN");

            string[] parts = validJwt.Split('.');
            string tamperedPayloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(@"{""scope"":""SUPER_ADMIN""}"));
            string tamperedJwt = $"{parts[0]}.{tamperedPayloadB64}.{parts[2]}";

            Assert.IsFalse(JwtHelper.IsAdmin(tamperedJwt));
        }

        [Test]
        public void IsAdmin_WithUnknownKidInHeader_ReturnsFalse()
        {
            InjectKeysForTesting(new[] { _jwksKey });
            string jwt = CreateJwt("unknown-kid", "WORDONLINE_ADMIN");

            Assert.IsFalse(JwtHelper.IsAdmin(jwt));
        }

        [Test]
        public void IsAdmin_WhenKeyIsNonSigningKey_ReturnsFalse()
        {
            JwksKey nonSigningKey = new JwksKey
            {
                kty = "RSA",
                use = "enc", // Encrypted rather than signing
                alg = "RS256",
                kid = TestKid,
                n = _jwksKey.n,
                e = _jwksKey.e
            };

            InjectKeysForTesting(new[] { nonSigningKey });
            string jwt = CreateJwt(TestKid, "WORDONLINE_ADMIN");

            Assert.IsFalse(JwtHelper.IsAdmin(jwt));
        }

        [Test]
        public void IsAdmin_WhenKeyIsNonRsaKey_ReturnsFalse()
        {
            JwksKey nonRsaKey = new JwksKey
            {
                kty = "EC",
                use = "sig",
                alg = "ES256",
                kid = TestKid,
                n = _jwksKey.n,
                e = _jwksKey.e
            };

            InjectKeysForTesting(new[] { nonRsaKey });
            string jwt = CreateJwt(TestKid, "WORDONLINE_ADMIN");

            Assert.IsFalse(JwtHelper.IsAdmin(jwt));
        }

        #endregion

        #region IsAdmin Tests - Null and Malformed Input

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not.a.valid.jwt.token")]
        [TestCase("onlytwo.parts")]
        [TestCase("invalidbase64header.invalidpayload.invalidsig")]
        public void IsAdmin_WithMalformedToken_ReturnsFalse(string malformedToken)
        {
            InjectKeysForTesting(new[] { _jwksKey });

            Assert.IsFalse(JwtHelper.IsAdmin(malformedToken));
        }

        #endregion

        #region Helper Method Tests

        [Test]
        public void DecodeHeader_WithValidToken_ReturnsHeaderJson()
        {
            string jwt = CreateJwt(TestKid, "WORDONLINE_USER");
            string headerJson = JwtHelper.DecodeHeader(jwt);

            Assert.IsNotNull(headerJson);
            StringAssert.Contains(TestKid, headerJson);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid.token")]
        public void DecodeHeader_WithInvalidToken_ReturnsNull(string invalidToken)
        {
            Assert.IsNull(JwtHelper.DecodeHeader(invalidToken));
        }

        [Test]
        public void DecodePayload_WithValidToken_ReturnsPayloadJson()
        {
            string jwt = CreateJwt(TestKid, "WORDONLINE_USER");
            string payloadJson = JwtHelper.DecodePayload(jwt);

            Assert.IsNotNull(payloadJson);
            StringAssert.Contains("WORDONLINE_USER", payloadJson);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid.token")]
        public void DecodePayload_WithInvalidToken_ReturnsNull(string invalidToken)
        {
            Assert.IsNull(JwtHelper.DecodePayload(invalidToken));
        }

        [Test]
        public void ExtractRoles_WithSpaceSeparatedRoles_ReturnsArrayOfRoles()
        {
            string jwt = CreateJwt(TestKid, "WORDONLINE_USER WORDONLINE_ADMIN SUPER_ADMIN");
            string[] roles = JwtHelper.ExtractRoles(jwt);

            Assert.AreEqual(3, roles.Length);
            Assert.AreEqual("WORDONLINE_USER", roles[0]);
            Assert.AreEqual("WORDONLINE_ADMIN", roles[1]);
            Assert.AreEqual("SUPER_ADMIN", roles[2]);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid.token")]
        public void ExtractRoles_WithInvalidOrEmptyToken_ReturnsEmptyArray(string token)
        {
            string[] roles = JwtHelper.ExtractRoles(token);
            Assert.IsNotNull(roles);
            Assert.IsEmpty(roles);
        }

        [Test]
        public void IsGuest_WhenGuestClaimIsTrue_ReturnsTrue()
        {
            string jwt = CreateJwt(TestKid, "WORDONLINE_USER", guest: true);
            Assert.IsTrue(JwtHelper.IsGuest(jwt));
        }

        [Test]
        public void IsGuest_WhenGuestClaimIsFalse_ReturnsFalse()
        {
            string jwt = CreateJwt(TestKid, "WORDONLINE_USER", guest: false);
            Assert.IsFalse(JwtHelper.IsGuest(jwt));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid.token")]
        public void IsGuest_WithInvalidOrEmptyToken_ReturnsFalse(string token)
        {
            Assert.IsFalse(JwtHelper.IsGuest(token));
        }

        #endregion
    }
}
