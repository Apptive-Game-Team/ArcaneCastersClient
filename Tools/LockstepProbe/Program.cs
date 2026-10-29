using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using BEPUutilities;
using FixMath.NET;

// 엔진 채택 전 검증용이다. 게임 상태 해시나 실제 전투 구현으로 사용하지 않는다.
public static class LockstepPhysicsProbe {
    private const int TickCount = 10000;

    private static int Main(string[] args) {
        try {
            if (args.Length == 1 && args[0] == "--checkpoint-only") { ArcaneCasters.Parity.CheckpointChecks.Run(); return 0; }
#if NET8_0_OR_GREATER
            if (args.Length == 1 && (args[0] == "--network-battle" || args[0] == "--network-model-battle")) {
                Console.WriteLine(NetworkBattleProbe.Run(requireModel: args[0] == "--network-model-battle").GetAwaiter().GetResult());
                return 0;
            }
#endif
            Console.WriteLine(Run());
            return 0;
        } catch (Exception exception) {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    public static string Run() {
        ArcaneCasters.Parity.ParityTests.Run();
        CoreTests.Run();
        var first = Replay();
        var second = Replay();
        for (var tick = 0; tick < TickCount; tick++) {
            if (first[tick] != second[tick]) {
                throw new InvalidOperationException($"동일 프로세스 재생 불일치: tick={tick}");
            }
        }
        using var digest = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(string.Join("\n", first));
        return $"PASS ticks={TickCount} traceSha256={Hex(digest.ComputeHash(bytes))}";
    }

    private static string[] Replay() {
        var space = new Space();
        if (space.ParallelLooper != null) throw new InvalidOperationException("단일 스레드가 필요합니다.");
        space.TimeStepSettings.TimeStepDuration = Fix64.One / (Fix64)20;
        space.ForceUpdater.Gravity = new Vector3(0, -10, 0);
        space.Add(new Box(new Vector3(0, -1, 0), 40, 1, 40));
        space.Add(new Box(new Vector3(-20, 10, 0), 1, 22, 40));
        space.Add(new Box(new Vector3(20, 10, 0), 1, 22, 40));
        space.Add(new Box(new Vector3(0, 10, -20), 40, 22, 1));
        space.Add(new Box(new Vector3(0, 10, 20), 40, 22, 1));
        var bodies = new List<Entity>();
        for (var i = 0; i < 32; i++) {
            var body = new Sphere(new Vector3(i % 8 - 4, 2 + i / 8, 0), Fix64.One / (Fix64)3, 1);
            bodies.Add(body);
            space.Add(body);
        }
        var hashes = new string[TickCount];
        using var digest = SHA256.Create();
        var collisionObserved = false;
        for (var tick = 0; tick < TickCount; tick++) {
            // 순서와 수치는 tick으로만 결정한다. 렌더링 시간이나 float 입력이 없다.
            if (tick % 37 == 0) {
                var body = bodies[(tick / 37) % bodies.Count];
                body.LinearVelocity = new Vector3((tick % 3) - 1, 5, 0);
            }
            space.Update();
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) {
                writer.Write(tick);
                for (var id = 0; id < bodies.Count; id++) {
                    var body = bodies[id];
                    if (body.CollisionInformation.Pairs.Count > 0) collisionObserved = true;
                    if (body.Position.Y < (Fix64)(-2)) throw new InvalidOperationException("바닥 관통");
                    writer.Write(id);
                    Write(writer, body.Position);
                    Write(writer, body.LinearVelocity);
                    Write(writer, body.AngularVelocity);
                    var orientation = body.Orientation;
                    writer.Write(orientation.X.RawValue);
                    writer.Write(orientation.Y.RawValue);
                    writer.Write(orientation.Z.RawValue);
                    writer.Write(orientation.W.RawValue);
                    writer.Write(body.ActivityInformation.IsActive);
                }
            }
            hashes[tick] = Hex(digest.ComputeHash(stream.ToArray()));
        }
        if (!collisionObserved) throw new InvalidOperationException("충돌이 발생하지 않았습니다.");
        return hashes;
    }

    private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "");

    private static void Write(BinaryWriter writer, Vector3 value) {
        writer.Write(value.X.RawValue);
        writer.Write(value.Y.RawValue);
        writer.Write(value.Z.RawValue);
    }
}
