using UnityEngine;

namespace NeonZigzag
{
    /// <summary>
    /// Persistent meta state: lifetime gems and the equipped skin.
    /// Skins unlock purely on gem milestones — no shop, no purchase step, so the reward
    /// for taking the bonus lane is immediate and unambiguous.
    /// </summary>
    public static class Progression
    {
        const string GemsKey = "neon_gems";
        const string SkinKey = "neon_skin";

        public static int Gems { get; private set; }
        public static int Selected { get; private set; }

        public static void Load()
        {
            Gems = PlayerPrefs.GetInt(GemsKey, 0);
            Selected = Mathf.Clamp(PlayerPrefs.GetInt(SkinKey, 0), 0, SkinLibrary.All.Length - 1);
            if (!IsUnlocked(Selected)) Selected = 0;
        }

        public static bool IsUnlocked(int index) =>
            index >= 0 && index < SkinLibrary.All.Length && Gems >= SkinLibrary.All[index].GemsRequired;

        public static int UnlockedCount
        {
            get
            {
                int count = 0;
                foreach (var skin in SkinLibrary.All)
                    if (Gems >= skin.GemsRequired) count++;
                return count;
            }
        }

        public static Skin Current => SkinLibrary.All[Selected];

        /// <summary>Index of the cheapest still-locked skin, or -1 when everything is unlocked.</summary>
        public static int NextLockedIndex
        {
            get
            {
                for (int i = 0; i < SkinLibrary.All.Length; i++)
                    if (Gems < SkinLibrary.All[i].GemsRequired) return i;
                return -1;
            }
        }

        /// <summary>Banks a run's gems. Returns the newly unlocked skin index, or -1.</summary>
        public static int AddGems(int amount)
        {
            if (amount <= 0) return -1;

            int before = UnlockedCount;
            Gems += amount;
            PlayerPrefs.SetInt(GemsKey, Gems);

            int unlocked = UnlockedCount;
            int newIndex = -1;
            if (unlocked > before)
            {
                newIndex = unlocked - 1;      // highest tier just crossed
                Selected = newIndex;          // auto-equip so the reward is visible immediately
                PlayerPrefs.SetInt(SkinKey, Selected);
            }

            PlayerPrefs.Save();
            return newIndex;
        }

        /// <summary>Steps to the next unlocked skin, wrapping around.</summary>
        public static void CycleSkin()
        {
            int count = SkinLibrary.All.Length;
            for (int step = 1; step <= count; step++)
            {
                int candidate = (Selected + step) % count;
                if (!IsUnlocked(candidate)) continue;
                Selected = candidate;
                PlayerPrefs.SetInt(SkinKey, Selected);
                PlayerPrefs.Save();
                return;
            }
        }
    }
}
