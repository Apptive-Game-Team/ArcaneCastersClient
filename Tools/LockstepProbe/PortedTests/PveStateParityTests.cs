using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class PveStateParityTests {
        [PortedTest("game/service/pve/pve_state_store.rs")] public static void one_seq_counts_changes_across_channels_and_skips_unchanged_values() {
            var store = new PveStateStore();
            Assert.Equal(new PveChannelState("bgm", "boss", 1), store.Set("bgm", "boss"));
            Assert.Equal(new PveChannelState("weather", "rain", 2), store.Set("weather", "rain"));
            Assert.True(store.Set("bgm", "boss") == null);
            Assert.Equal(new PveChannelState("bgm", null, 3), store.Set("bgm", null));
            Assert.Sequence(new[] { new PveChannelState("weather", "rain", 2), new PveChannelState("bgm", null, 3) }, store.Snapshot());
        }
        public static void CheckSnapshotsAndDefaults() {
            var store = new PveStateStore(); Assert.Equal(0, store.Snapshot().Length);
            var initial = store.Set("bgm", null); Assert.Equal("pveState", initial.Type); Assert.Equal(1, initial.Seq); Assert.True(store.Set("bgm", null) == null);
            var snapshot = store.Snapshot(); store.Set("bgm", "boss"); Assert.True(snapshot[0].Value == null);
            snapshot[0] = new PveChannelState("other", "changed", 99); Assert.Equal("boss", store.Snapshot()[0].Value);
            Assert.Equal(1, new PveStateStore().Set("bgm", "boss").Seq);
        }
    }
}
