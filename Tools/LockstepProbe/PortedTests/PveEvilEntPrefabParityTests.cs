namespace ArcaneCasters.Parity {
    public static class PveEvilEntPrefabParityTests {
        private const string Source = "game/domain/object/prefab/implement/pve/tests.rs";
        [PortedTest(Source)] public static void is_stationary_and_is_the_health_mob_the_installer_finds() => PveEvilEntParityTests.is_stationary_and_is_the_health_mob_the_installer_finds();
        [PortedTest(Source)] public static void max_hp_override_applies() => PveEvilEntParityTests.max_hp_override_applies();
        [PortedTest(Source)] public static void destroyed_boss_is_terminal_and_leaves_no_death_field() => PveEvilEntParityTests.destroyed_boss_is_terminal_and_leaves_no_death_field();
        [PortedTest(Source)] public static void hp_percent_trigger_reads_the_boss_hp() => PveEvilEntParityTests.hp_percent_trigger_reads_the_boss_hp();
    }
}
