using UnityEngine;

namespace NeonZigzag
{
    public struct Skin
    {
        public string Name;
        public Color Core;      // HDR — values above 1 drive the bloom
        public Color TrailNear;
        public Color TrailFar;
        public int GemsRequired;
    }

    /// <summary>
    /// Unlock thresholds are tuned against gems-per-run: a cautious run banks ~8 gems,
    /// a run that keeps cutting into the bonus lane banks ~30.
    /// </summary>
    public static class SkinLibrary
    {
        public static readonly Skin[] All =
        {
            new Skin
            {
                Name = "PULSE", GemsRequired = 0,
                Core = new Color(2.4f, 2.6f, 3.4f),
                TrailNear = new Color(0.55f, 0.95f, 1f), TrailFar = new Color(0.9f, 0.3f, 1f)
            },
            new Skin
            {
                Name = "EMBER", GemsRequired = 40,
                Core = new Color(3.4f, 1.5f, 0.5f),
                TrailNear = new Color(1f, 0.75f, 0.35f), TrailFar = new Color(1f, 0.2f, 0.1f)
            },
            new Skin
            {
                Name = "TOXIC", GemsRequired = 110,
                Core = new Color(1.3f, 3.2f, 0.9f),
                TrailNear = new Color(0.75f, 1f, 0.4f), TrailFar = new Color(0.1f, 0.9f, 0.5f)
            },
            new Skin
            {
                Name = "VOID", GemsRequired = 240,
                Core = new Color(1.9f, 0.9f, 3.4f),
                TrailNear = new Color(0.75f, 0.45f, 1f), TrailFar = new Color(0.15f, 0.1f, 0.6f)
            },
            new Skin
            {
                Name = "FROST", GemsRequired = 450,
                Core = new Color(1.5f, 2.9f, 3.4f),
                TrailNear = new Color(0.7f, 0.95f, 1f), TrailFar = new Color(0.3f, 0.5f, 1f)
            },
            new Skin
            {
                Name = "SOLAR", GemsRequired = 800,
                Core = new Color(3.6f, 2.7f, 0.9f),
                TrailNear = new Color(1f, 0.9f, 0.5f), TrailFar = new Color(1f, 0.45f, 0.05f)
            }
        };
    }
}
