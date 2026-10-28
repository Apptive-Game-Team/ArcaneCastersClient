using System.Linq;
using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class RandomParityTests {
        private const string Source = "game/util/random.rs";
        [PortedTest(Source)] public static void next_int_bound_matches_java() {
            var random = new JavaRandom(42); Assert.Sequence(new[] { 30,63,48,84,70 }, Enumerable.Range(0,5).Select(_ => random.NextBound(100)));
            random = new JavaRandom(1); Assert.Sequence(new[] { 46,6,26,26,13 }, Enumerable.Range(0,5).Select(_ => random.NextBound(64)));
            random = new JavaRandom(1); Assert.Sequence(new[] { 215764588,880641847,874970313,446064254,77814904 }, Enumerable.Range(0,5).Select(_ => random.NextBound(1500000000)));
        }
        [PortedTest(Source)] public static void next_int_matches_java() {
            var random = new JavaRandom(42); Assert.Sequence(new[] { -1170105035,234785527,-1360544799 }, Enumerable.Range(0,3).Select(_ => random.NextInt()));
        }
        [PortedTest(Source)] public static void next_long_matches_java() {
            var random = new JavaRandom(-7); Assert.Sequence(new[] { 4964420944606563203L,-6028743012628894217L,-8826958116857042465L }, Enumerable.Range(0,3).Select(_ => random.NextLong()));
        }
        [PortedTest(Source)] public static void next_boolean_matches_java() {
            var random = new JavaRandom(5); Assert.Sequence(new[] { true,false,false,true,false,true,false,true }, Enumerable.Range(0,8).Select(_ => random.NextBoolean()));
        }
        // 원본의 IEEE 비트 기대값을 정수 연산으로 Q32.32에 내린다. 런타임의 난수 생성식을 재사용하지 않는다.
        private static long DoubleFractionRaw(ulong bits) => (long)(((bits & ((1UL << 52) - 1)) | (1UL << 52)) >> (1043 - (int)((bits >> 52) & 2047)));
        private static long FloatFractionRaw(uint bits) => (long)((bits & ((1U << 23) - 1)) | (1U << 23)) << ((int)((bits >> 23) & 255) - 118);
        private static long SignedDoubleRaw(long bits) {
            ulong unsigned = unchecked((ulong)bits); long magnitude = DoubleFractionRaw(unsigned & long.MaxValue); return bits < 0 ? -magnitude : magnitude;
        }
        private static long SignedFloatRaw(int bits) {
            uint unsigned = unchecked((uint)bits); long magnitude = FloatFractionRaw(unsigned & int.MaxValue); return bits < 0 ? -magnitude : magnitude;
        }
        // 원본 float 반올림과 고정 소수점 Ln/Sqrt의 차이는 1e-6 안에서 비교한다.
        private static void Near(long expected, Fix64 actual) => Assert.True(Fix64.Abs(Fix64.FromRaw(expected) - actual) <= (Fix64)0.000001m);
        [PortedTest(Source)] public static void bounded_default_methods_match_java() {
            var random = new JavaRandom(1); Assert.Sequence(new[] { 10,1,14,14,15 }, Enumerable.Range(0,5).Select(_ => random.NextRange(-5,17)));
            random = new JavaRandom(3); foreach (int bits in new[] { 1074798254,1066658960,-1073580125 }) Near(SignedFloatRaw(bits), random.NextFloatRange((Fix64)(-2.5m),4));
            random = new JavaRandom(3); foreach (long bits in new[] { 4612253185775481416L,-4611599207549624262L,-4611542533880705794L }) Near(SignedDoubleRaw(bits), random.NextFractionRange((Fix64)(-2.5m),4));
            random = new JavaRandom(3); Assert.Sequence(new long[] { 276,741,442 }, Enumerable.Range(0,3).Select(_ => random.NextLongBound(1000)));
            random = new JavaRandom(3); foreach (uint bits in new uint[] { 1074552046,1071118088,1046091360 }) Near(FloatFractionRaw(bits), random.NextFloatBound(3));
        }
        [PortedTest(Source)] public static void next_gaussian_matches_java() {
            var random = new JavaRandom(99);
            foreach (long bits in new[] { 4608510154852676553L,-4617188881996588056L,4597610358971709409L,4598579684266223040L,4605829603386657876L,-4618290767308032432L }) Near(SignedDoubleRaw(bits), random.NextGaussian());
            var cached = new JavaRandom(99); cached.NextGaussian(); long state = cached.State; Assert.True(cached.HasGaussianSpare);
            var spare = cached.GaussianSpare; Assert.Equal(spare, cached.NextGaussian()); Assert.Equal(state, cached.State); Assert.True(!cached.HasGaussianSpare);
            CheckMixedTrace();
        }
        private static void CheckMixedTrace() {
            var random = new JavaRandom(-17);
            using (var stream = new System.IO.MemoryStream()) {
                using (var writer = new System.IO.BinaryWriter(stream, System.Text.Encoding.UTF8, true)) {
                    for (int i = 0; i < 10000; i++) {
                        int integer = random.NextRange(int.MinValue, int.MaxValue); Assert.True(integer < int.MaxValue); writer.Write(integer);
                        long bounded = random.NextLongBound(long.MaxValue); Assert.True(bounded >= 0 && bounded < long.MaxValue); writer.Write(bounded);
                        writer.Write(random.NextGaussian().RawValue); writer.Write(random.State); writer.Write(random.HasGaussianSpare); writer.Write(random.GaussianSpare.RawValue);
                    }
                }
                using (var hash = System.Security.Cryptography.SHA256.Create())
                    System.Console.WriteLine("PASS random draws=10000 traceSha256=" + System.BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", ""));
            }
        }
        [PortedTest(Source)] public static void next_double_and_float_match_java() {
            var random = new JavaRandom(123456789);
            Assert.Sequence(new ulong[] { 4604156343054189992,4601903331488821802,4600706359258197490 }.Select(DoubleFractionRaw), Enumerable.Range(0,3).Select(_ => random.NextFraction().RawValue));
            random = new JavaRandom(123456789);
            Assert.Sequence(new uint[] { 1059716710,1061382325,1055520148 }.Select(FloatFractionRaw), Enumerable.Range(0,3).Select(_ => random.NextFloatFraction().RawValue));
        }
    }
}
