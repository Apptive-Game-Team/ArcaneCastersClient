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
    public class JwtHelperTests
    {
        private RSA _rsa;
        private JwksKey _testKey;
        private string _testKid;

        [SetUp]
        public void SetUp()
        {
            _rsa = RSA.Create(2048);
            RSAParameters pubParams = _rsa.ExportParameters(false);

            _testKid = "test-key-id-1";
            _testKey = new JwksKey
            {
                kty = "RSA",
                use = "sig",
                alg = "RS256",
                kid = _testKid,
                n = Base64UrlEncode(pubParams.Modulus),
                e = Base64UrlEncode(pubParams.Exponent)
            };

            SetJwksKeys(new[] { _testKey }, isFetched: true);
        }

        [TearDown]
        public void TearDown()
        {
            SetJwksKeys(null, isFetched: false);
            _rsa?.Dispose();
        }

        #region Helper Methods

        private static void SetJwksKeys(IEnumerable<JwksKey> keys, bool isFetched)
        {
            FieldInfo keysField = typeof(JwksService).GetField("_cachedKeys", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = (Dictionary<string, JwksKey>)keysField.GetValue(null);
            dict.Clear();

            if (keys != null)
            {
                foreach (JwksKey key in keys)
                {
                    if (!string.IsNullOrEmpty(key.kid))
                        dict[key.kid] = key;
                    else
                        dict[Guid.NewGuid().ToString()] = key;
                }
            }

            FieldInfo fetchedField = typeof(JwksService).GetField("_isFetched", BindingFlags.NonPublic | BindingFlags.Static);
            fetchedField.SetValue(null, isFetched);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            string base64 = Convert.ToBase64String(input);
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private string CreateJwt(string headerJson, string payloadJson, RSA signingRsa = null)
        {
            signingRsa ??= _rsa;

            string headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            string payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            string headerAndPayload = $"{headerB64}.{payloadB64}";

            byte[] data = Encoding.UTF8.GetBytes(headerAndPayload);
            byte[] signature = signingRsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            string sigB64 = Base64UrlEncode(signature);

            return $"{headerAndPayload}.{sigB64}";
        }

        #endregion

        #region VerifySignature Tests

        [TestCase(null)]
        [TestCase("")]
        public void VerifySignature_NullOrEmptyToken_ReturnsFalse(string token)
        {
            Assert.IsFalse(JwtHelper.VerifySignature(token));
        }

        [TestCase("invalidToken")]
        [TestCase("header.payload")]
        [TestCase("a.b.c.d")]
        public void VerifySignature_InvalidPartsCount_ReturnsFalse(string token)
        {
            Assert.IsFalse(JwtHelper.VerifySignature(token));
        }

        [Test]
        public void VerifySignature_InvalidHeaderBase64_ReturnsFalse()
        {
            string token = "!!!invalidBase64!!!.eyJzdWIiOiIxMjM0NTY3ODkwIn0.signature";
            Assert.IsFalse(JwtHelper.VerifySignature(token));
        }

        [Test]
        public void VerifySignature_InvalidHeaderJson_ReturnsFalse()
        {
            string headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes("not json"));
            string payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes("{}"));
            string token = $"{headerB64}.{payloadB64}.signature";

            Assert.IsFalse(JwtHelper.VerifySignature(token));
        }

        [Test]
        public void VerifySignature_MatchingKidKeyValidSignature_ReturnsTrue()
        {
            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"sub\":\"1234567890\",\"scope\":\"WORDONLINE_USER\"}";
            string jwt = CreateJwt(headerJson, payloadJson);

            Assert.IsTrue(JwtHelper.VerifySignature(jwt));
        }

        [Test]
        public void VerifySignature_KidNotFoundInJwks_ReturnsFalse()
        {
            string headerJson = "{\"alg\":\"RS256\",\"kid\":\"unknown-kid\"}";
            string payloadJson = "{\"sub\":\"1234567890\"}";
            string jwt = CreateJwt(headerJson, payloadJson);

            Assert.IsFalse(JwtHelper.VerifySignature(jwt));
        }

        [Test]
        public void VerifySignature_KidKeyNotRsa_ReturnsFalse()
        {
            _testKey.kty = "EC";
            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"sub\":\"1234567890\"}";
            string jwt = CreateJwt(headerJson, payloadJson);

            Assert.IsFalse(JwtHelper.VerifySignature(jwt));
        }

        [Test]
        public void VerifySignature_KidKeyUseNotSig_ReturnsFalse()
        {
            _testKey.use = "enc";
            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"sub\":\"1234567890\"}";
            string jwt = CreateJwt(headerJson, payloadJson);

            Assert.IsFalse(JwtHelper.VerifySignature(jwt));
        }

        [Test]
        public void VerifySignature_KidKeyAlgNotRS256_ReturnsFalse()
        {
            _testKey.alg = "HS256";
            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"sub\":\"1234567890\"}";
            string jwt = CreateJwt(headerJson, payloadJson);

            Assert.IsFalse(JwtHelper.VerifySignature(jwt));
        }

        [Test]
        public void VerifySignature_KeyWithEmptyUseAndAlg_ReturnsTrue()
        {
            _testKey.use = null;
            _testKey.alg = "";
            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"sub\":\"1234567890\"}";
            string jwt = CreateJwt(headerJson, payloadJson);

            Assert.IsTrue(JwtHelper.VerifySignature(jwt));
        }

        [Test]
        public void VerifySignature_TamperedPayload_ReturnsFalse()
        {
            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"sub\":\"1234567890\"}";
            string jwt = CreateJwt(headerJson, payloadJson);

            string[] parts = jwt.Split('.');
            string tamperedPayloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes("{\"sub\":\"hacked\"}"));
            string tamperedJwt = $"{parts[0]}.{tamperedPayloadB64}.{parts[2]}";

            Assert.IsFalse(JwtHelper.VerifySignature(tamperedJwt));
        }

        [Test]
        public void VerifySignature_TamperedSignature_ReturnsFalse()
        {
            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"sub\":\"1234567890\"}";
            string jwt = CreateJwt(headerJson, payloadJson);

            string[] parts = jwt.Split('.');
            string tamperedSig = parts[2].Substring(0, parts[2].Length - 4) + "AAAA";
            string tamperedJwt = $"{parts[0]}.{parts[1]}.{tamperedSig}";

            Assert.IsFalse(JwtHelper.VerifySignature(tamperedJwt));
        }

        [Test]
        public void VerifySignature_SignedWithDifferentPrivateKey_ReturnsFalse()
        {
            using RSA anotherRsa = RSA.Create(2048);
            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"sub\":\"1234567890\"}";
            string jwt = CreateJwt(headerJson, payloadJson, signingRsa: anotherRsa);

            Assert.IsFalse(JwtHelper.VerifySignature(jwt));
        }

        [Test]
        public void VerifySignature_NoKidInHeader_MatchesOneOfCachedKeys_ReturnsTrue()
        {
            using RSA otherRsa = RSA.Create(2048);
            RSAParameters otherPub = otherRsa.ExportParameters(false);
            JwksKey otherKey = new JwksKey
            {
                kty = "RSA",
                use = "sig",
                alg = "RS256",
                kid = "other-key",
                n = Base64UrlEncode(otherPub.Modulus),
                e = Base64UrlEncode(otherPub.Exponent)
            };

            SetJwksKeys(new[] { otherKey, _testKey }, isFetched: true);

            string headerJson = "{\"alg\":\"RS256\"}";
            string payloadJson = "{\"sub\":\"1234567890\"}";
            string jwt = CreateJwt(headerJson, payloadJson, signingRsa: _rsa);

            Assert.IsTrue(JwtHelper.VerifySignature(jwt));
        }

        [Test]
        public void VerifySignature_NoKidInHeader_NoMatchingKeys_ReturnsFalse()
        {
            using RSA otherRsa = RSA.Create(2048);
            string headerJson = "{\"alg\":\"RS256\"}";
            string payloadJson = "{\"sub\":\"1234567890\"}";
            string jwt = CreateJwt(headerJson, payloadJson, signingRsa: otherRsa);

            Assert.IsFalse(JwtHelper.VerifySignature(jwt));
        }

        #endregion

        #region DecodeHeader and DecodePayload Tests

        [Test]
        public void DecodeHeader_ValidToken_ReturnsHeaderJson()
        {
            string headerJson = "{\"alg\":\"RS256\",\"typ\":\"JWT\"}";
            string jwt = CreateJwt(headerJson, "{}");

            string decoded = JwtHelper.DecodeHeader(jwt);
            Assert.AreEqual(headerJson, decoded);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid")]
        public void DecodeHeader_InvalidToken_ReturnsNull(string token)
        {
            Assert.IsNull(JwtHelper.DecodeHeader(token));
        }

        [Test]
        public void DecodePayload_ValidToken_ReturnsPayloadJson()
        {
            string payloadJson = "{\"sub\":\"user123\",\"guest\":true}";
            string jwt = CreateJwt("{\"alg\":\"RS256\"}", payloadJson);

            string decoded = JwtHelper.DecodePayload(jwt);
            Assert.AreEqual(payloadJson, decoded);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid")]
        public void DecodePayload_InvalidToken_ReturnsNull(string token)
        {
            Assert.IsNull(JwtHelper.DecodePayload(token));
        }

        #endregion

        #region ExtractRoles Tests

        [Test]
        public void ExtractRoles_ValidScope_ReturnsRoleArray()
        {
            string payloadJson = "{\"scope\":\"WORDONLINE_ADMIN WORDONLINE_USER SUPER_ADMIN\"}";
            string jwt = CreateJwt("{\"alg\":\"RS256\"}", payloadJson);

            string[] roles = JwtHelper.ExtractRoles(jwt);

            Assert.AreEqual(3, roles.Length);
            Assert.AreEqual("WORDONLINE_ADMIN", roles[0]);
            Assert.AreEqual("WORDONLINE_USER", roles[1]);
            Assert.AreEqual("SUPER_ADMIN", roles[2]);
        }

        [Test]
        public void ExtractRoles_EmptyScopeOrMalformed_ReturnsEmptyArray()
        {
            string jwtNoScope = CreateJwt("{\"alg\":\"RS256\"}", "{\"sub\":\"123\"}");
            Assert.IsEmpty(JwtHelper.ExtractRoles(jwtNoScope));

            Assert.IsEmpty(JwtHelper.ExtractRoles(null));
            Assert.IsEmpty(JwtHelper.ExtractRoles("invalid"));
        }

        #endregion

        #region IsAdmin Tests

        [Test]
        public void IsAdmin_ValidSignatureWithAdminRole_ReturnsTrue()
        {
            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"scope\":\"WORDONLINE_ADMIN WORDONLINE_USER\"}";
            string jwt = CreateJwt(headerJson, payloadJson);

            Assert.IsTrue(JwtHelper.IsAdmin(jwt));
        }

        [Test]
        public void IsAdmin_ValidSignatureWithSuperAdminRole_ReturnsTrue()
        {
            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"scope\":\"SUPER_ADMIN\"}";
            string jwt = CreateJwt(headerJson, payloadJson);

            Assert.IsTrue(JwtHelper.IsAdmin(jwt));
        }

        [Test]
        public void IsAdmin_ValidSignatureWithUserRoleOnly_ReturnsFalse()
        {
            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"scope\":\"WORDONLINE_USER\"}";
            string jwt = CreateJwt(headerJson, payloadJson);

            Assert.IsFalse(JwtHelper.IsAdmin(jwt));
        }

        [Test]
        public void IsAdmin_InvalidSignatureWithAdminRole_ReturnsFalse()
        {
            using RSA otherRsa = RSA.Create(2048);
            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"scope\":\"WORDONLINE_ADMIN\"}";
            string jwt = CreateJwt(headerJson, payloadJson, signingRsa: otherRsa);

            Assert.IsFalse(JwtHelper.IsAdmin(jwt));
        }

        [Test]
        public void IsAdmin_JwksNotFetched_ReturnsFalse()
        {
            SetJwksKeys(new[] { _testKey }, isFetched: false);

            string headerJson = $"{{\"alg\":\"RS256\",\"kid\":\"{_testKid}\"}}";
            string payloadJson = "{\"scope\":\"WORDONLINE_ADMIN\"}";
            string jwt = CreateJwt(headerJson, payloadJson);

            Assert.IsFalse(JwtHelper.IsAdmin(jwt));
        }

        #endregion

        #region IsGuest Tests

        [Test]
        public void IsGuest_GuestTrue_ReturnsTrue()
        {
            string payloadJson = "{\"guest\":true}";
            string jwt = CreateJwt("{\"alg\":\"RS256\"}", payloadJson);

            Assert.IsTrue(JwtHelper.IsGuest(jwt));
        }

        [Test]
        public void IsGuest_GuestFalseOrMissing_ReturnsFalse()
        {
            string jwtFalse = CreateJwt("{\"alg\":\"RS256\"}", "{\"guest\":false}");
            Assert.IsFalse(JwtHelper.IsGuest(jwtFalse));

            string jwtMissing = CreateJwt("{\"alg\":\"RS256\"}", "{\"sub\":\"123\"}");
            Assert.IsFalse(JwtHelper.IsGuest(jwtMissing));
        }

        [Test]
        public void IsGuest_InvalidToken_ReturnsFalse()
        {
            Assert.IsFalse(JwtHelper.IsGuest(null));
            Assert.IsFalse(JwtHelper.IsGuest("invalid"));
        }

        #endregion
    }
}
