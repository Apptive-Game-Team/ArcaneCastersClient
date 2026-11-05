using System;
using System.Collections.Generic;
using GameScene.Dto;
using Global;
using UnityEngine;

namespace GameScene
{
    // Match data outlives the game scene, but never the account or the next match.
    public static class BotThoughtLog
    {
        public const int Capacity = 100;
        private static readonly List<string> entries = new List<string>(Capacity);
        private static string ownerToken;
        private static float startedAt;

        public static IReadOnlyList<string> Entries => entries;
        public static int TotalCount { get; private set; }
        public static bool HasEnded { get; private set; }
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Changed = null;
            Clear();
        }

        public static void Clear()
        {
            entries.Clear();
            ownerToken = SceneContext.JwtToken;
            startedAt = Time.unscaledTime;
            TotalCount = 0;
            HasEnded = false;
            Changed?.Invoke();
        }

        public static void BeginMatch()
        {
            entries.Clear();
            ownerToken = SceneContext.JwtToken;
            startedAt = Time.unscaledTime;
            TotalCount = 0;
            HasEnded = false;
            Changed?.Invoke();
        }

        public static void DiscardOtherOwner()
        {
            if (ownerToken != null && ownerToken != SceneContext.JwtToken) Clear();
        }

        public static void Append(BotThoughtInfo thought)
        {
            if (thought == null || HasEnded) return;
            if (ownerToken != SceneContext.JwtToken) BeginMatch();
            int seconds = Mathf.Max(0, (int)(Time.unscaledTime - startedAt));
            string reason = thought.reason ?? "";
            if (reason.Length > 1024) reason = reason.Substring(0, 1024) + "…";
            string side = thought.botSide == "LeftPlayer" ? "L" : "R";
            entries.Add($"[{seconds / 60:00}:{seconds % 60:00}] {side} · {reason}");
            if (entries.Count > Capacity) entries.RemoveAt(0);
            TotalCount++;
            Changed?.Invoke();
        }

        public static void EndMatch()
        {
            HasEnded = true;
            Changed?.Invoke();
        }
    }
}
