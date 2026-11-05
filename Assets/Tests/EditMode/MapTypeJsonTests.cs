using System.Collections.Generic;
using Admin.Dto;
using Data;
using Global.Serialization;
using LobbyScene;
using LobbyScene.Debugger;
using NUnit.Framework;

namespace WordOnline.Tests
{
    /// <summary>
    /// 경기를 만드는 응답마다 <c>mapType</c> 이 DTO 에 실리는지 고정한다. 필드는 string 이라
    /// 모르는 이름이 와도 응답 전체가 버려지지 않아야 하고, 없는 필드는 null 로 남아야 한다.
    /// </summary>
    public class MapTypeJsonTests
    {
        private const string MatchedInfoJson = @"{
            ""message"": ""matched"",
            ""server"": ""http://localhost:7777"",
            ""sessionId"": ""session-1"",
            ""mapType"": ""FORTRESS"",
            ""leftUser"": { ""id"": 11, ""name"": ""left"" },
            ""rightUser"": { ""id"": 22, ""name"": ""right"" }
        }";

        [Test]
        public void MatchedInfoCarriesMapType()
        {
            MatchedInfoDto dto = JsonCodec.Deserialize<MatchedInfoDto>(MatchedInfoJson);

            Assert.AreEqual("FORTRESS", dto.mapType);
            Assert.AreEqual(MapKind.Fortress, dto.MapKind);
        }

        [Test]
        public void MatchedInfoWithoutMapTypeIsUnspecified()
        {
            MatchedInfoDto dto = JsonCodec.Deserialize<MatchedInfoDto>(@"{ ""sessionId"": ""session-1"" }");

            Assert.IsNull(dto.mapType);
            Assert.AreEqual(MapKind.Unspecified, dto.MapKind);
        }

        [Test]
        public void MatchedInfoWithNullMapTypeIsUnspecified()
        {
            MatchedInfoDto dto = JsonCodec.Deserialize<MatchedInfoDto>(@"{ ""sessionId"": ""s"", ""mapType"": null }");

            Assert.IsNull(dto.mapType);
            Assert.AreEqual(MapKind.Unspecified, dto.MapKind);
        }

        [Test]
        public void MatchedInfoWithUnknownMapTypeStillParsesTheRestOfTheResponse()
        {
            bool parsed = JsonCodec.TryDeserialize(
                @"{ ""sessionId"": ""session-9"", ""mapType"": ""DESERT"" }",
                out MatchedInfoDto dto,
                out string error);

            Assert.IsTrue(parsed, error);
            Assert.AreEqual("session-9", dto.sessionId);
            Assert.AreEqual(MapKind.Unknown, dto.MapKind);
        }

        [Test]
        public void MatchTicketEventCarriesMapTypeInsideMatchInfo()
        {
            MatchTicket ticket = JsonCodec.Deserialize<MatchTicket>(
                @"{ ""ticketId"": ""t1"", ""state"": ""MATCHED"", ""version"": 3,
                    ""matchInfo"": { ""sessionId"": ""s1"", ""mapType"": ""RIVER"" } }");

            Assert.AreEqual(MatchTicketState.Matched, ticket.ParsedState);
            Assert.AreEqual(MapKind.River, ticket.matchInfo.MapKind);
        }

        [Test]
        public void SerializedMatchedInfoDoesNotWriteTheComputedMapKind()
        {
            MatchedInfoDto dto = JsonCodec.Deserialize<MatchedInfoDto>(MatchedInfoJson);

            string json = JsonCodec.Serialize(dto);

            StringAssert.Contains("\"mapType\":\"FORTRESS\"", json.Replace(" ", ""));
            StringAssert.DoesNotContain("MapKind", json);
        }

        [Test]
        public void DebugGameResponseCarriesMapType()
        {
            DebugGameResponse response = JsonCodec.Deserialize<DebugGameResponse>(
                @"{ ""sessionId"": ""debug-1"", ""mapType"": ""GATE"" }");

            Assert.AreEqual("debug-1", response.sessionId);
            Assert.AreEqual(MapKind.Gate, MapKinds.Parse(response.mapType));
        }

        [Test]
        public void DebugGameResponseWithoutMapTypeLeavesItNull()
        {
            DebugGameResponse response = JsonCodec.Deserialize<DebugGameResponse>(@"{ ""sessionId"": ""debug-1"" }");

            Assert.IsNull(response.mapType);
        }

        [Test]
        public void DebugSessionKeepsTheResponseMapType()
        {
            MatchedInfoDto dto = MatchedInfoDto.CreateDebugSession("debug-1", "left", 7L, "FOREST");

            Assert.AreEqual(MapKind.Forest, dto.MapKind);
            Assert.AreEqual("debug-1", dto.sessionId);
        }

        [Test]
        public void DebugSessionWithoutMapTypeIsUnspecified()
        {
            MatchedInfoDto dto = MatchedInfoDto.CreateDebugSession("debug-1", "left", 7L);

            Assert.AreEqual(MapKind.Unspecified, dto.MapKind);
        }

        [Test]
        public void RoomInfoCarriesMapTypeAndSpectatingSessionKeepsIt()
        {
            List<RoomInfo> rooms = JsonCodec.Deserialize<List<RoomInfo>>(
                @"[ { ""sessionId"": ""r1"", ""leftUserId"": 1, ""rightUserId"": 2, ""serverUrl"": ""http://x"", ""mapType"": ""RIVER"" },
                    { ""sessionId"": ""r2"", ""leftUserId"": 3, ""rightUserId"": 4, ""serverUrl"": ""http://x"" } ]");

            MatchedInfoDto withMap = MatchedInfoDto.CreateSpectatingSession(rooms[0]);
            MatchedInfoDto withoutMap = MatchedInfoDto.CreateSpectatingSession(rooms[1]);

            Assert.AreEqual(MapKind.River, withMap.MapKind);
            Assert.AreEqual(MapKind.Unspecified, withoutMap.MapKind);
        }
    }
}
