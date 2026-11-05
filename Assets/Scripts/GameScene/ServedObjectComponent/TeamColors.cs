using Global;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// Team colors by relation to the local player, not by side. A player on the left used to see
    /// their own units in red because the colors were fixed per side; now the local player's units
    /// are always <see cref="Ally"/> and the opponent's are always <see cref="Enemy"/>.
    /// <para>
    /// When <see cref="SceneContext.Me"/> is neither side (spectator, admin, no match) there is no
    /// local player to relate to, so the old fixed mapping stays: LeftPlayer red, RightPlayer blue.
    /// </para>
    /// </summary>
    public static class TeamColors
    {
        private const string LeftPlayer = "LeftPlayer";
        private const string RightPlayer = "RightPlayer";

        public static readonly Color Ally = new Color(0.25f, 0.55f, 0.98f, 1f);
        public static readonly Color Enemy = new Color(0.92f, 0.24f, 0.24f, 1f);

        public static bool TryGet(string master, out Color color)
        {
            if (master != LeftPlayer && master != RightPlayer)
            {
                color = Color.clear;
                return false;
            }

            string me = SceneContext.Me;
            if (me == LeftPlayer || me == RightPlayer)
            {
                color = master == me ? Ally : Enemy;
                return true;
            }

            color = master == LeftPlayer ? Enemy : Ally;
            return true;
        }
    }
}
