using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BEPUphysics;
using BEPUphysics.Constraints.Collision;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUutilities;
using FixMath.NET;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class PhysicsCheckpointChecks {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("Physics checkpoint: " + message); }
        public static void Run() {
            Inventory(); IndependentWorlds(); HiddenStateMutations();
            Console.WriteLine("PASS physics-checkpoint: field inventory, independent worlds, hidden body/contact/GJK/solver mutations traceSha256=" + ReplayDigest());
        }
        private static byte[] Bytes(PhysicsWorld world) {
            using (var stream = new MemoryStream()) { using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) world.WriteCheckpoint(writer); return stream.ToArray(); }
        }
        private static Space Space(PhysicsWorld world) => (Space)typeof(PhysicsWorld).GetField("space", Fields).GetValue(world);
        private static SortedDictionary<int, BEPUphysics.Entities.Entity> Bodies(PhysicsWorld world) => (SortedDictionary<int, BEPUphysics.Entities.Entity>)typeof(PhysicsWorld).GetField("bodies", Fields).GetValue(world);
        private static PhysicsWorld Create() {
            var world = new PhysicsWorld(new Vector3(0, -10, 0));
            world.AddBox(1, new Vector3(0, -1, 0), new Vector3(20, 1, 20), Fix64.Zero);
            world.AddSphere(2, new Vector3(0, 1, 0), Fix64.One, Fix64.One);
            world.AddSphere(3, new Vector3(1, 1, 0), Fix64.One, Fix64.One);
            world.AddEdge(4, new Vector3(-5, 0, -4), new Vector3(-5, 0, 4));
            world.AddSphere(5, new Vector3(-5, 0, 0), Fix64.One, Fix64.One);
            world.ConfigureBattleBody(2); world.ConfigureBattleBody(3); world.ConfigureBattleBody(4, true); world.ConfigureBattleBody(5);
            return world;
        }
        private static string CanonicalType(Type type) {
            if (type.IsArray) return CanonicalType(type.GetElementType()) + "[]";
            if (type.IsGenericType) return type.GetGenericTypeDefinition().FullName + "[" + string.Join(",", type.GetGenericArguments().Select(CanonicalType)) + "]";
            return type.FullName;
        }
        private static void Inventory() {
            var assembly = typeof(Space).Assembly;
            foreach (var group in PhysicsStateFieldManifest.Expected.Select(line => line.Split('|')).GroupBy(parts => parts[0])) {
                var type = assembly.GetType(group.Key, true);
                var expected = group.Select(parts => parts[1] + "|" + parts[2]).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                var actual = type.GetFields(Fields).Select(field => field.Name + "|" + CanonicalType(field.FieldType)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                Require(expected.SequenceEqual(actual), "new or changed engine state needs an explicit capture policy: " + group.Key);
            }
        }
        private static void IndependentWorlds() {
            var first = Create(); var second = Create();
            Require(Bytes(first).SequenceEqual(Bytes(second)), "fresh worlds must be independent of process IDs");
            for (int tick = 0; tick < 300; tick++) {
                if (tick % 29 == 0) { var velocity = new Vector3((tick % 3) - 1, 3, 0); first.SetVelocity(2, velocity); second.SetVelocity(2, velocity); }
                first.Step(); second.Step();
                                var leftBytes = Bytes(first); var rightBytes = Bytes(second);
                if (!leftBytes.SequenceEqual(rightBytes)) {
                    int difference = Enumerable.Range(0, Math.Min(leftBytes.Length, rightBytes.Length)).FirstOrDefault(index => leftBytes[index] != rightBytes[index]);
                    throw new InvalidOperationException("Physics checkpoint tick=" + tick + " difference=" + difference + " lengths=" + leftBytes.Length + "/" + rightBytes.Length + " context=" + System.Text.Encoding.UTF8.GetString(leftBytes.Skip(Math.Max(0, difference - 160)).Take(180).ToArray()));
                }
            }
            // 먼저 만든 경기의 pair/node를 반환한 뒤에도 같은 상태로 시작해야 한다.
            for (int id = 1; id <= 5; id++) { first.Remove(id); second.Remove(id); }
            first.Step(); second.Step();
            var third = Create(); var fourth = Create(); third.Step(); fourth.Step();
                        var thirdBytes = Bytes(third); var fourthBytes = Bytes(fourth);
            if (!thirdBytes.SequenceEqual(fourthBytes)) {
                int difference = Enumerable.Range(0, Math.Min(thirdBytes.Length, fourthBytes.Length)).FirstOrDefault(index => thirdBytes[index] != fourthBytes[index]);
                throw new InvalidOperationException("Physics checkpoint differs at " + difference + " lengths=" + thirdBytes.Length + "/" + fourthBytes.Length + " context=" + System.Text.Encoding.UTF8.GetString(thirdBytes.Skip(Math.Max(0, difference - 160)).Take(170).ToArray()));
            }
        }
        public static string ReplayDigest() {
            var world = Create();
            using (var hash = System.Security.Cryptography.SHA256.Create()) {
                for (int tick = 0; tick < 300; tick++) {
                    if (tick % 29 == 0) world.SetVelocity(2, new Vector3((tick % 3) - 1, 3, 0));
                    world.Step(); var bytes = Bytes(world); hash.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
                }
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return BitConverter.ToString(hash.Hash).Replace("-", "").ToLowerInvariant();
            }
        }
        private static void HiddenStateMutations() {
            var world = Create(); world.Step();
            var body = Bodies(world)[2]; var initial = Bytes(world);
            var position = body.Position; var velocity = body.LinearVelocity;
            var damping = typeof(BEPUphysics.Entities.Entity).GetField("linearDampingBoost", Fields);
            var previous = damping.GetValue(body); damping.SetValue(body, (Fix64)0.125m);
            Require(!initial.SequenceEqual(Bytes(world)) && body.Position == position && body.LinearVelocity == velocity, "hidden body damping must change hash without moving the body");
            damping.SetValue(body, previous);
            var pair = Space(world).NarrowPhase.Pairs.OfType<StandardPairHandler>().First(item => item.ContactManifold.Contacts.Count > 0);
            var constraint = pair.ContactConstraint is ConvexContactManifoldConstraint convex ? convex.ContactPenetrationConstraints[0] : ((NonConvexContactManifoldConstraint)pair.ContactConstraint).ContactPenetrationConstraints[0];
            initial = Bytes(world); var impulseField = typeof(ContactPenetrationConstraint).GetField("accumulatedImpulse", Fields); var impulse = (Fix64)impulseField.GetValue(constraint); impulseField.SetValue(constraint, impulse + Fix64.One);
            Require(!initial.SequenceEqual(Bytes(world)), "solver cached impulse must change hash"); impulseField.SetValue(constraint, impulse);
            var contact = pair.ContactManifold.Contacts[0]; initial = Bytes(world); var depth = contact.PenetrationDepth; contact.PenetrationDepth += Fix64.One;
            Require(!initial.SequenceEqual(Bytes(world)), "persistent contact depth must change hash"); contact.PenetrationDepth = depth;
            var general = Space(world).NarrowPhase.Pairs.OfType<StandardPairHandler>().Select(item => item.ContactManifold).OfType<BEPUphysics.CollisionTests.Manifolds.GeneralConvexContactManifold>().FirstOrDefault();
            Require(general != null, "fixture must exercise a persistent GJK manifold");
            if (general != null) {
                initial = Bytes(world); var axis = general.PairTester.GetType().GetField("localSeparatingAxis", Fields); var oldAxis = axis.GetValue(general.PairTester);
                axis.SetValue(general.PairTester, new Vector3(123, 456, 789)); Require(!initial.SequenceEqual(Bytes(world)), "GJK cached separating axis must change hash"); axis.SetValue(general.PairTester, oldAxis);
            }
        }
    }
}
