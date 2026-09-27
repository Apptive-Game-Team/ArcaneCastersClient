using System;
using System.Text;
using Global.Util;
using NUnit.Framework;

namespace WordOnline.Tests
{
    [TestFixture]
    public class JwtHelperTests
    {
        private static string CreateJwtToken(string payloadJson)
        {
            string headerJson = "{\"alg\":\"RS256\",\"typ\":\"JWT\"}";
            string headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            string payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            string signatureB64 = Base64UrlEncode(Encoding.UTF8.GetBytes("dummy_signature"));

            return $"{headerB64}.{payloadB64}.{signatureB64}";
        }

        private static string Base64UrlEncode(byte[] input)
        {
            string base64 = Convert.ToBase64String(input);
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        [Test]
        public void IsGuest_ReturnsFalse_WhenJwtTokenIsNull()
        {
            Assert.IsFalse(JwtHelper.IsGuest(null));
        }

        [Test]
        public void IsGuest_ReturnsFalse_WhenJwtTokenIsEmpty()
        {
            Assert.IsFalse(JwtHelper.IsGuest(string.Empty));
        }

        [Test]
        public void IsGuest_ReturnsFalse_WhenJwtTokenIsMalformed()
        {
            Assert.IsFalse(JwtHelper.IsGuest("invalid.token"));
            Assert.IsFalse(JwtHelper.IsGuest("not_a_jwt"));
            Assert.IsFalse(JwtHelper.IsGuest("part1.part2.part3.part4"));
        }

        [Test]
        public void IsGuest_ReturnsFalse_WhenPayloadIsNotValidJson()
        {
            string invalidJsonB64 = Base64UrlEncode(Encoding.UTF8.GetBytes("not json"));
            string jwt = $"header.{invalidJsonB64}.sig";

            Assert.IsFalse(JwtHelper.IsGuest(jwt));
        }

        [Test]
        public void IsGuest_ReturnsTrue_WhenGuestClaimIsTrue()
        {
            string jwt = CreateJwtToken("{\"sub\":\"user123\",\"guest\":true}");

            Assert.IsTrue(JwtHelper.IsGuest(jwt));
        }

        [Test]
        public void IsGuest_ReturnsFalse_WhenGuestClaimIsFalse()
        {
            string jwt = CreateJwtToken("{\"sub\":\"user123\",\"guest\":false}");

            Assert.IsFalse(JwtHelper.IsGuest(jwt));
        }

        [Test]
        public void IsGuest_ReturnsFalse_WhenGuestClaimIsMissing()
        {
            string jwt = CreateJwtToken("{\"sub\":\"user123\",\"scope\":\"WORDONLINE_USER\"}");

            Assert.IsFalse(JwtHelper.IsGuest(jwt));
        }

        [Test]
        public void IsGuest_ReturnsFalse_WhenGuestClaimIsNotBoolean()
        {
            string jwt = CreateJwtToken("{\"sub\":\"user123\",\"guest\":\"true\"}");

            Assert.IsFalse(JwtHelper.IsGuest(jwt));
        }
    }
}
