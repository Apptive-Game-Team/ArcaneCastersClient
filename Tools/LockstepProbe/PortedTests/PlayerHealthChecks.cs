using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class PlayerHealthChecks {
        public static void Run() {
            var context = new SimulationContext(1) { DeltaTime = 1 };
            context.Players.Add(Master.LeftPlayer, new PlayerData(new ManaCharger(100))); context.Players.Add(Master.RightPlayer, new PlayerData(new ManaCharger(100)));
            context.RegisterPrefab("HealthProbe", owner => owner.AddComponentImmediately(new PlayerHealthComponent(owner, 250)));
            var left = context.Spawn(Master.LeftPlayer, "HealthProbe", new Vector3(1, 0, 5)); var right = context.Spawn(Master.RightPlayer, "HealthProbe", new Vector3(13, 0, 5)); context.FlushObjects();
            Assert.True(!left.IsDestroyed && !right.IsDestroyed); Assert.Equal(250, context.Players[Master.LeftPlayer].Hp); Assert.Equal(250, context.Players[Master.RightPlayer].Hp);
            var health = left.GetComponent<PlayerHealthComponent>(); Assert.True(left.HasComponent<IPlayerHealth>() && left.HasComponent<IFrenzyImmune>() && left.HasComponent<IMindControlImmune>());
            health.OnDamaged(new Hit(20, right.Id, Element.None)); Assert.Equal(230, health.Hp); Assert.Equal(230, context.Players[Master.LeftPlayer].Hp); Assert.Equal(250, context.Players[Master.RightPlayer].Hp);
            health.OnDamaged(new Hit(-30, 0, Element.None)); Assert.Equal(250, context.Players[Master.LeftPlayer].Hp);
            health.OnDamagedWithDelay(new Hit(10, 0, Element.None), 1); health.Update(); Assert.Equal(240, context.Players[Master.LeftPlayer].Hp);
            left.Elements.AddNative(Element.Fire); health.OnDamaged(new Hit(1000, 0, Element.None)); Assert.True(context.Result.Ended); Assert.Equal((Master?)Master.LeftPlayer, context.Result.Loser); Assert.True(!left.IsDestroyed); Assert.Equal(0, context.PendingObjects.Length);
            right.GetComponent<PlayerHealthComponent>().OnDeath(); Assert.Equal((Master?)Master.LeftPlayer, context.Result.Loser); Assert.Equal(2L, context.Result.WinnerId(1, 2));
            Assert.Equal(PlayerResult.Lose, context.Result.ForPlayer(Master.LeftPlayer)); Assert.Equal(PlayerResult.Win, context.Result.ForPlayer(Master.RightPlayer));
            var draw = new BattleResult(); Assert.True(!draw.Ended); draw.SetEnd(); Assert.True(draw.Ended); Assert.Equal(PlayerResult.Draw, draw.ForPlayer(Master.LeftPlayer)); Assert.Equal(PlayerResult.Draw, draw.ForPlayer(Master.RightPlayer));
        }
    }
}
