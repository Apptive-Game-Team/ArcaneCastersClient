using NUnit.Framework;
using Global.Util;

namespace WordOnline.Tests
{
    public class JwtHelperTests
    {
        // Sample Base64Url header strings:
        // {"alg":"RS256","typ":"JWT"} -> Base64Url: eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9
        // {"alg":"HS256"}              -> Base64Url: eyJhbGciOiJIUzI1NiJ9
        // {"kid":"key123"}             -> Base64Url: eyJraWQiOiJrZXkxMjMifQ (length 20, mod 4 = 0)
        // Base64Url with '-' and '_':
        // {"alg":"RS256","test":"a-b_c"} -> Base64Url: eyJhbGciOiJSUzI1NiIsInRlc3QiOiJhLWJfYyJ9

        private const string ValidHeaderBase64 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9"; // {"alg":"RS256","typ":"JWT"}
        private const string ExpectedHeaderJson = "{\"alg\":\"RS256\",\"typ\":\"JWT\"}";

        private const string ValidPayloadBase64 = "eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJzY29wZSI6IldPUkRPTkxJTkVfQURNSU4gR1VFU1QiLCJndWVzdCI6dHJ1ZX0"; // {"sub":"1234567890","name":"John Doe","iat":1516239022,"scope":"WORDONLINE_ADMIN GUEST","guest":true}
        private const string SignatureBase64 = "somesignature";

        private const string SampleJwt = ValidHeaderBase64 + "." + ValidPayloadBase64 + "." + SignatureBase64;

        [Test]
        public void DecodeHeader_WithValidJwt_ReturnsHeaderJson()
        {
            string header = JwtHelper.DecodeHeader(SampleJwt);
            Assert.AreEqual(ExpectedHeaderJson, header);
        }

        [Test]
        public void DecodeHeader_WithDifferentPaddingLengths_DecodesCorrectly()
        {
            // Case 1: Base64Url string length mod 4 == 2 (requires "==")
            // {"a":"b"} -> eyJhIjoiYiJ9 (length 14, 14 % 4 = 2)
            string jwtMod2 = "eyJhIjoiYiJ9." + ValidPayloadBase64 + "." + SignatureBase64;
            Assert.AreEqual("{\"a\":\"b\"}", JwtHelper.DecodeHeader(jwtMod2));

            // Case 2: Base64Url string length mod 4 == 3 (requires "=")
            // {"a":"abc"} -> eyJhIjoiYWJjIn0 (length 15, 15 % 4 = 3)
            string jwtMod3 = "eyJhIjoiYWJjIn0." + ValidPayloadBase64 + "." + SignatureBase64;
            Assert.AreEqual("{\"a\":\"abc\"}", JwtHelper.DecodeHeader(jwtMod3));

            // Case 3: Base64Url string length mod 4 == 0 (no padding required)
            // {"alg":"HS256"} -> eyJhbGciOiJIUzI1NiJ9 (length 20, 20 % 4 = 0)
            string jwtMod0 = "eyJhbGciOiJIUzI1NiJ9." + ValidPayloadBase64 + "." + SignatureBase64;
            Assert.AreEqual("{\"alg\":\"HS256\"}", JwtHelper.DecodeHeader(jwtMod0));
        }

        [Test]
        public void DecodeHeader_WithUrlSafeCharacters_DecodesCorrectly()
        {
            // Base64Url uses '-' instead of '+' and '_' instead of '/'
            // String with '-' and '_': {"sub":"a-b_c"} -> eyJzdWIiOiJhLWJfYyJ9
            string jwtUrlSafe = "eyJzdWIiOiJhLWJfYyJ9." + ValidPayloadBase64 + "." + SignatureBase64;
            Assert.AreEqual("{\"sub\":\"a-b_c\"}", JwtHelper.DecodeHeader(jwtUrlSafe));
        }

        [Test]
        public void DecodeHeader_WithNullOrEmpty_ReturnsNull()
        {
            Assert.IsNull(JwtHelper.DecodeHeader(null));
            Assert.IsNull(JwtHelper.DecodeHeader(""));
        }

        [Test]
        public void DecodeHeader_WithInvalidPartsCount_ReturnsNull()
        {
            // Less than 3 parts
            Assert.IsNull(JwtHelper.DecodeHeader("headeronly"));
            Assert.IsNull(JwtHelper.DecodeHeader("header.payload"));

            // More than 3 parts
            Assert.IsNull(JwtHelper.DecodeHeader("header.payload.signature.extra"));
        }

        [Test]
        public void DecodeHeader_WithInvalidBase64_ReturnsNull()
        {
            // Invalid characters for Base64 Decoding
            string invalidBase64Jwt = "!!!NotBase64!!!." + ValidPayloadBase64 + "." + SignatureBase64;
            Assert.IsNull(JwtHelper.DecodeHeader(invalidBase64Jwt));
        }

        [Test]
        public void DecodePayload_WithValidJwt_ReturnsPayloadJson()
        {
            string payload = JwtHelper.DecodePayload(SampleJwt);
            Assert.IsNotNull(payload);
            Assert.IsTrue(payload.Contains("\"sub\":\"1234567890\""));
        }

        [Test]
        public void DecodePayload_WithNullOrInvalid_ReturnsNull()
        {
            Assert.IsNull(JwtHelper.DecodePayload(null));
            Assert.IsNull(JwtHelper.DecodePayload("invalid.jwt"));
        }

        [Test]
        public void ExtractRoles_WithValidJwtScope_ReturnsRolesArray()
        {
            string[] roles = JwtHelper.ExtractRoles(SampleJwt);
            Assert.AreEqual(2, roles.Length);
            Assert.AreEqual("WORDONLINE_ADMIN", roles[0]);
            Assert.AreEqual("GUEST", roles[1]);
        }

        [Test]
        public void ExtractRoles_WithInvalidOrEmpty_ReturnsEmptyArray()
        {
            Assert.IsEmpty(JwtHelper.ExtractRoles(null));
            Assert.IsEmpty(JwtHelper.ExtractRoles("invalid.jwt"));
        }

        [Test]
        public void IsGuest_WithGuestTrueInPayload_ReturnsTrue()
        {
            Assert.IsTrue(JwtHelper.IsGuest(SampleJwt));
        }

        [Test]
        public void IsGuest_WithGuestFalseOrMissing_ReturnsFalse()
        {
            // {"guest":false} -> eyJndWVzdCI6ZmFsc2V9
            string nonGuestJwt = ValidHeaderBase64 + ".eyJndWVzdCI6ZmFsc2V9." + SignatureBase64;
            Assert.IsFalse(JwtHelper.IsGuest(nonGuestJwt));

            Assert.IsFalse(JwtHelper.IsGuest(null));
            Assert.IsFalse(JwtHelper.IsGuest("invalid.jwt"));
        }
    }
}
