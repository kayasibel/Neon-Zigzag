using UnityEngine;

namespace NeonZigzag
{
    /// <summary>Shared colors, meshes and materials. Everything is built lazily at runtime.</summary>
    public static class NeonTheme
    {
        public static readonly Color Background = new Color(0.027f, 0.035f, 0.086f);
        public static readonly Color Ambient = new Color(0.10f, 0.13f, 0.26f);
        public static readonly Color Ink = new Color(0.85f, 0.90f, 1f);
        public static readonly Color Dim = new Color(0.45f, 0.52f, 0.72f);
        public static readonly Color Orb = new Color(1f, 0.78f, 0.20f);

        static Mesh _cube, _sphere;
        static Material _tile, _glow, _additive, _particle;

        public static Mesh Cube => _cube != null ? _cube : (_cube = Grab(PrimitiveType.Cube));
        public static Mesh Sphere => _sphere != null ? _sphere : (_sphere = Grab(PrimitiveType.Sphere));

        public static Material TileMaterial => _tile != null ? _tile : (_tile = Load("TileMat"));
        public static Material GlowMaterial => _glow != null ? _glow : (_glow = Load("GlowMat"));
        public static Material AdditiveMaterial => _additive != null ? _additive : (_additive = Load("AdditiveMat"));
        public static Material ParticleMaterial => _particle != null ? _particle : (_particle = Load("ParticleMat"));

        /// <summary>An instance of the glow material tinted to <paramref name="hdrColor"/>.</summary>
        public static Material NewGlow(Color hdrColor)
        {
            var m = new Material(GlowMaterial);
            m.SetColor("_BaseColor", hdrColor);
            m.color = hdrColor;
            return m;
        }

        /// <summary>Neon hue that drifts slowly as the player travels, so the run never looks static.</summary>
        public static Color PathColor(int progress)
        {
            float h = Mathf.Repeat(0.52f + progress * 0.0035f, 1f);
            return Color.HSVToRGB(h, 0.72f, 1f);
        }

        /// <summary>Complementary hue used for the bonus branch so the choice reads instantly.</summary>
        public static Color BonusColor(int progress)
        {
            float h = Mathf.Repeat(0.52f + progress * 0.0035f + 0.42f, 1f);
            return Color.HSVToRGB(h, 0.85f, 1f);
        }

        static Mesh Grab(PrimitiveType type)
        {
            // Preferred path: the built-in mesh directly. CreatePrimitive also attaches a
            // collider, and since nothing else in the game touches Physics, managed stripping
            // drops those classes from a player build and it logs an error.
            var mesh = Resources.GetBuiltinResource<Mesh>(type == PrimitiveType.Cube ? "Cube.fbx" : "Sphere.fbx");
            if (mesh != null) return mesh;

            var go = GameObject.CreatePrimitive(type);
            mesh = go.GetComponent<MeshFilter>().sharedMesh;
            go.SetActive(false);   // never let the scratch primitive render a frame
            Object.Destroy(go);
            return mesh;
        }

        static Material Load(string name)
        {
            var m = Resources.Load<Material>("Neon/" + name);
            if (m == null) Debug.LogError("[NeonZigzag] Missing material Resources/Neon/" + name +
                                          ". Let the editor recompile once so NeonAssetBuilder can create it.");
            return m;
        }
    }
}
