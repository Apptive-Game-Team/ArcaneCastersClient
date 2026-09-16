using GameScene.Dto;
using GameScene.Dto.Projectile;
using Global.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    /// <summary>
    /// Frame payloads shaped like the ones the game server actually sends.
    /// </summary>
    public class FrameContractTests
    {
        private const string FramePayload = @"{
            ""type"":""frame"",
            ""remainingTime"":95,
            ""updatedMana"":5,
            ""leftPlayerHp"":100,
            ""rightPlayerHp"":100,
            ""cards"":{""added"":[19,20]},
            ""objects"":{
                ""create"":[{
                    ""id"":12,
                    ""type"":""FireSpirit"",
                    ""position"":{""x"":1.5,""y"":0.0,""z"":-2.25},
                    ""master"":""Left"",
                    ""gizmos"":[{
                        ""relativePosition"":{""x"":0.0,""y"":0.0,""z"":0.0},
                        ""radius"":1.0,
                        ""boxSize"":null,
                        ""type"":""Circle"",
                        ""category"":""AttackRange""
                    }]
                }],
                ""update"":[{
                    ""id"":12,
                    ""status"":""Attack"",
                    ""effects"":[""Burn""],
                    ""master"":""Left"",
                    ""position"":{""x"":2.0,""y"":0.0,""z"":-2.0},
                    ""gauges"":[{""value"":30.0,""maxValue"":50.0,""category"":""HP""}]
                }],
                ""projectile"":[{
                    ""type"":""ArrowShot"",
                    ""start"":{""targetType"":""reference"",""id"":12},
                    ""end"":{""targetType"":""position"",""x"":4.0,""y"":0.0,""z"":1.0},
                    ""duration"":0.35
                }]
            }
        }";

        [Test]
        public void CreatedObjectCarriesPositionAndGizmos()
        {
            FrameInfoDto frame = (FrameInfoDto)JsonCodec.Deserialize<ServerMessage>(FramePayload);

            CreatedObjectDto created = frame.objects.create[0];
            Assert.AreEqual(12, created.id);
            Assert.AreEqual("FireSpirit", created.type);
            Assert.AreEqual(new Vector3(1.5f, 0f, -2.25f), created.position);
            Assert.AreEqual(1, created.gizmos.Count);
            Assert.AreEqual("AttackRange", created.gizmos[0].category);
            Assert.AreEqual(1f, created.gizmos[0].radius);
        }

        [Test]
        public void UpdatedObjectCarriesGauges()
        {
            FrameInfoDto frame = (FrameInfoDto)JsonCodec.Deserialize<ServerMessage>(FramePayload);

            UpdatedObjectDto updated = frame.objects.update[0];
            Assert.AreEqual("Attack", updated.status);
            Assert.AreEqual(new Vector3(2f, 0f, -2f), updated.position);
            Assert.AreEqual(new[] { "Burn" }, updated.effects.ToArray());
            Assert.AreEqual(1, updated.gauges.Count);
            Assert.AreEqual("HP", updated.gauges[0].category);
            Assert.AreEqual(30f, updated.gauges[0].value);
            Assert.AreEqual(50f, updated.gauges[0].maxValue);
        }

        [Test]
        public void ProjectileKeepsBothTargetShapes()
        {
            FrameInfoDto frame = (FrameInfoDto)JsonCodec.Deserialize<ServerMessage>(FramePayload);

            ProjectileDto projectile = frame.objects.projectile[0];
            Assert.AreEqual("ArrowShot", projectile.type);
            Assert.AreEqual(0.35f, projectile.duration);
            Assert.IsInstanceOf<ReferenceProjectileTarget>(projectile.start);
            Assert.AreEqual(12, ((ReferenceProjectileTarget)projectile.start).id);
            Assert.IsInstanceOf<PositionProjectileTarget>(projectile.end);
            Assert.AreEqual(new Vector3(4f, 0f, 1f), ((PositionProjectileTarget)projectile.end).ToVector3());
        }

        /// <summary>
        /// 서버는 비어 있는 프레임에서 컬렉션을 통째로 생략한다. 소비 측이 순회 전에 반드시 가드해야 한다.
        /// </summary>
        [Test]
        public void OmittedCollectionsStayNull()
        {
            const string json = @"{""type"":""frame"",""remainingTime"":10,""updatedMana"":1,
                ""leftPlayerHp"":100,""rightPlayerHp"":100,""cards"":{},""objects"":{}}";

            FrameInfoDto frame = (FrameInfoDto)JsonCodec.Deserialize<ServerMessage>(json);

            Assert.IsNull(frame.cards.added);
            Assert.IsNull(frame.objects.create);
            Assert.IsNull(frame.objects.update);
            Assert.IsNull(frame.objects.projectile);
        }

        [Test]
        public void SnapshotObjectsCarryGaugesAndGizmos()
        {
            const string json = @"{""type"":""sync"",""remainingTime"":42,""updatedMana"":9,
                ""leftPlayerHp"":55,""rightPlayerHp"":60,
                ""snapshotResponseDto"":{
                    ""frame"":840,
                    ""objects"":[{
                        ""id"":31,
                        ""prefab"":""WaterSlime"",
                        ""x"":-3.0,""y"":0.0,""z"":2.5,
                        ""master"":""Right"",
                        ""status"":""Move"",
                        ""effects"":[],
                        ""gizmos"":[],
                        ""gauges"":[{""value"":10.0,""maxValue"":20.0,""category"":""HP""}]
                    }],
                    ""myCards"":[54]
                },
                ""projectileDtos"":[]}";

            SyncFrameInfo sync = (SyncFrameInfo)JsonCodec.Deserialize<ServerMessage>(json);

            SnapshotObjectDto snapshotObject = sync.snapshotResponseDto.objects[0];
            Assert.AreEqual(31, snapshotObject.id);
            Assert.AreEqual("WaterSlime", snapshotObject.prefab);
            Assert.AreEqual(-3f, snapshotObject.x);
            Assert.AreEqual("Move", snapshotObject.status);
            Assert.AreEqual(20f, snapshotObject.gauges[0].maxValue);
        }
    }
}
