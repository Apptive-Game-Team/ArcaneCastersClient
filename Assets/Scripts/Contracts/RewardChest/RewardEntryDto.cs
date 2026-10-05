using System;

namespace RewardChest
{
    /// <summary>
    /// One reward as every lobby endpoint sends it: quest check, chest list preview and chest open.
    /// <para>
    /// <see cref="rewardType"/> is a plain string on purpose. The server adds reward types before the
    /// client ships them, and an enum here would make Json.NET throw on the whole response
    /// (see <c>.agents/docs/json-payloads.md</c>).
    /// </para>
    /// </summary>
    [Serializable]
    public class RewardEntryDto
    {
        /// <summary><c>MAGIC</c>, <c>DECORATION</c>, <c>APPEARANCE</c>, <c>CHEST</c> or a type this client does not know yet.</summary>
        public string rewardType;

        public long rewardId;

        /// <summary>Appearance key for <c>APPEARANCE</c>, chest key for <c>CHEST</c>, null for every other type.</summary>
        public string rewardKey;

        public int amount;
    }
}
