using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Data;
using Global.Serialization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    /// <summary>
    /// Fixes the fallback order of <see cref="PlayerAppearanceResolver"/>: requested set, default set, serialized sprites.
    /// Strings stand in for sprites; a path present in the set means the asset exists.
    /// </summary>
    public class PlayerAppearanceResolverTests
    {
        private const string PlayerPrefab = "Assets/Resources/Prefabs/Player.prefab";

        [TestCase("default", new[] { "attacking", "cast-recoil", "cast-recover" }, new[] { 0.4f, 2f / 3f })]
        [TestCase("corrupted_finn", new[] { "attacking", "cast-recoil", "cast-recover", "cast-lower" }, new[] { 0.4f, 0.6f, 0.8f })]
        [TestCase("blaze", new[] { "attacking", "cast-recoil", "cast-recover", "cast-lower" }, new[] { 0.4f, 0.6f, 0.8f })]
        [TestCase("golem", new[] { "attacking", "cast-recoil", "cast-recover", "cast-arc", "cast-lower" }, new[] { 0.4f, 0.56666666f, 0.73333335f, 0.8666667f })]
        [TestCase("grass", new[] { "attacking", "cast-recoil", "cast-recover", "cast-arc", "cast-lower" }, new[] { 0.4f, 0.56666666f, 0.73333335f, 0.8666667f })]
        [TestCase("storm", new[] { "attacking", "cast-recoil", "cast-recover", "cast-arc", "cast-lower" }, new[] { 0.4f, 0.56666666f, 0.73333335f, 0.8666667f })]
        [TestCase("summoner", new[] { "attacking", "cast-recoil", "cast-recover", "cast-arc", "cast-lower" }, new[] { 0.4f, 0.56666666f, 0.73333335f, 0.8666667f })]
        [TestCase("tide", new[] { "attacking", "cast-recoil", "cast-recover", "cast-arc", "cast-lower" }, new[] { 0.4f, 0.56666666f, 0.73333335f, 0.8666667f })]
        public void StaffMotionUsesRegisteredSpritesAndAnchors(string appearance, string[] castNames, float[] castCues)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            var controller = StaffController(prefab);
            var serialized = new SerializedObject(controller);
            Assert.That(serialized.FindProperty("staffAura").objectReferenceValue, Is.Not.Null);

            var motion = StaffMotion(serialized, appearance);
            string path = "PlayerAppearances/" + appearance + "/";
            var idle = Resources.Load<Sprite>(path + "idle");
            CheckFrameList(motion.FindPropertyRelative("aimFrames"), path, idle, "idle", "aim-lift-start", "aim-lift-end", "raised");
            CheckFrameList(motion.FindPropertyRelative("castFrames"), path, idle, castNames);
            var cues = motion.FindPropertyRelative("castCueFractions");
            Assert.That(cues.arraySize, Is.EqualTo(castCues.Length));
            for (int index = 0; index < castCues.Length; index++)
            {
                Assert.That(cues.GetArrayElementAtIndex(index).floatValue, Is.EqualTo(castCues[index]));
            }
        }

        [Test]
        public void FinnMotionWithMissingSpriteOrDuplicateCueFallsBack()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab));
            try
            {
                var controller = StaffController(instance);
                using var serialized = new SerializedObject(controller);
                var motion = StaffMotion(serialized, "corrupted_finn");
                var cues = motion.FindPropertyRelative("castCueFractions");
                var castFrames = motion.FindPropertyRelative("castFrames");
                var checkedFrame = castFrames.GetArrayElementAtIndex(1).FindPropertyRelative("sprite");
                var previousSprite = checkedFrame.objectReferenceValue;
                var find = controller.GetType().GetMethod("FindMotion", BindingFlags.Instance | BindingFlags.NonPublic);
                const string prefix = "PlayerAppearances/corrupted_finn/";
                object[] args =
                {
                    "corrupted_finn", Resources.Load<Sprite>(prefix + "idle"),
                    Resources.Load<Sprite>(prefix + "raised"), Resources.Load<Sprite>(prefix + "attacking"),
                };
                Assert.That(find.Invoke(controller, args), Is.Not.Null);

                checkedFrame.objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(find.Invoke(controller, args), Is.Null);

                checkedFrame.objectReferenceValue = previousSprite;
                cues.GetArrayElementAtIndex(1).floatValue = cues.GetArrayElementAtIndex(0).floatValue;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(find.Invoke(controller, args), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static MonoBehaviour StaffController(GameObject root) => root.GetComponents<MonoBehaviour>()
            .Single(component => component != null && component.GetType().Name == "PlayerStaffPoseController");

        private static SerializedProperty StaffMotion(SerializedObject owner, string id)
        {
            var motions = owner.FindProperty("appearanceMotions");
            return Enumerable.Range(0, motions.arraySize).Select(motions.GetArrayElementAtIndex)
                .Single(entry => entry.FindPropertyRelative("appearanceId").stringValue == id);
        }

        private static void CheckFrameList(SerializedProperty frames, string path, Sprite idle, params string[] names)
        {
            Assert.That(frames.arraySize, Is.EqualTo(names.Length));
            for (int index = 0; index < names.Length; index++)
            {
                var frame = frames.GetArrayElementAtIndex(index);
                var sprite = frame.FindPropertyRelative("sprite").objectReferenceValue as Sprite;
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite, Is.SameAs(Resources.Load<Sprite>(path + names[index])));
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(idle.pixelsPerUnit));
                Assert.That(sprite.pivot.y, Is.EqualTo(idle.pivot.y).Within(0.01f));
                float centeredPivotDifference = sprite.pivot.x - idle.pivot.x - (sprite.rect.width - idle.rect.width) / 2f;
                Assert.That(centeredPivotDifference, Is.EqualTo(0f).Within(0.05f));
                Assert.That(frame.FindPropertyRelative("tip").vector2Value, Is.Not.EqualTo(Vector2.zero));
            }
        }

        [Test]
        public void CorruptedFinnLoadsAllThreePosesWithOnePivot()
        {
            string id = PlayerAppearanceResolver.Resolve("corrupted_finn", Resources.Load<Sprite>,
                out Sprite idle, out Sprite raised, out Sprite attacking);

            Assert.AreEqual("corrupted_finn", id);
            Assert.AreEqual(idle.pivot, raised.pivot);
            Assert.AreEqual(idle.pivot, attacking.pivot);
            Assert.AreEqual(idle.pixelsPerUnit, raised.pixelsPerUnit);
            Assert.AreEqual(idle.pixelsPerUnit, attacking.pixelsPerUnit);
        }

        private static string[] Set(string id)
        {
            return new[] { "PlayerAppearances/" + id + "/idle", "PlayerAppearances/" + id + "/raised", "PlayerAppearances/" + id + "/attacking" };
        }

        private static string Resolve(string appearance, ICollection<string> existing, out string idle, out string raised, out string attacking)
        {
            return PlayerAppearanceResolver.Resolve(appearance, path => existing.Contains(path) ? path : null, out idle, out raised, out attacking);
        }

        [Test]
        public void UsesRequestedSetWhenComplete()
        {
            var existing = new List<string>(Set("storm"));
            existing.AddRange(Set("default"));

            string id = Resolve("storm", existing, out string idle, out string raised, out string attacking);

            Assert.AreEqual("storm", id);
            Assert.AreEqual("PlayerAppearances/storm/idle", idle);
            Assert.AreEqual("PlayerAppearances/storm/raised", raised);
            Assert.AreEqual("PlayerAppearances/storm/attacking", attacking);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void EmptyAppearanceUsesDefaultSet(string appearance)
        {
            string id = Resolve(appearance, Set("default"), out string idle, out _, out _);

            Assert.AreEqual("default", id);
            Assert.AreEqual("PlayerAppearances/default/idle", idle);
        }

        [Test]
        public void IncompleteRequestedSetFallsBackToWholeDefaultSet()
        {
            var existing = new List<string> { "PlayerAppearances/blaze/idle", "PlayerAppearances/blaze/raised" };
            existing.AddRange(Set("default"));

            string id = Resolve("blaze", existing, out string idle, out string raised, out string attacking);

            Assert.AreEqual("default", id);
            Assert.AreEqual("PlayerAppearances/default/idle", idle);
            Assert.AreEqual("PlayerAppearances/default/raised", raised);
            Assert.AreEqual("PlayerAppearances/default/attacking", attacking);
        }

        [Test]
        public void UnknownIdFallsBackToDefaultSet()
        {
            string id = Resolve("no-such-look", Set("default"), out _, out _, out _);

            Assert.AreEqual("default", id);
        }

        [Test]
        public void ReturnsNullWhenDefaultSetIsAlsoMissing()
        {
            string id = Resolve("grass", new List<string>(), out string idle, out string raised, out string attacking);

            Assert.IsNull(id);
            Assert.IsNull(idle);
            Assert.IsNull(raised);
            Assert.IsNull(attacking);
        }

        [TestCase("../default")]
        [TestCase("a/b")]
        [TestCase("a\\b")]
        public void UnsafeIdIsTreatedAsDefault(string appearance)
        {
            Assert.AreEqual("default", PlayerAppearanceResolver.ResolveId(appearance));
        }

        [Test]
        public void AppearanceSurvivesJsonDeserialization()
        {
            MatchedInfoDto dto = JsonCodec.Deserialize<MatchedInfoDto>(
                @"{ ""leftUser"": { ""id"": 1, ""name"": ""bot"", ""appearance"": ""tide"" }, ""rightUser"": { ""id"": 2, ""name"": ""human"" } }");

            Assert.AreEqual("tide", dto.FindUserByMaster("LeftPlayer").appearance);
            Assert.IsNull(dto.FindUserByMaster("RightPlayer").appearance);
            Assert.IsNull(dto.FindUserByMaster("Other"));
        }
    }
}
