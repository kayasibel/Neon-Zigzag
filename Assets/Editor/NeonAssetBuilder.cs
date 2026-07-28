using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonZigzag.EditorTools
{
    /// <summary>
    /// Creates the handful of materials the game needs as real assets under
    /// Assets/Resources/Neon so they survive shader stripping in a player build.
    /// Runs automatically on every domain reload; does nothing if they already exist.
    /// </summary>
    public static class NeonAssetBuilder
    {
        const string Dir = "Assets/Resources/Neon";

        [InitializeOnLoadMethod]
        static void EnsureAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            ApplyPlayerSettingsOnce();

            if (AssetDatabase.LoadAssetAtPath<Material>(Dir + "/TileMat.mat") != null) return;

            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Resources", "Neon");

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var particle = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (lit == null || unlit == null)
            {
                Debug.LogError("[NeonZigzag] URP shaders not found. Is the Universal RP package active?");
                return;
            }

            var tile = new Material(lit) { name = "TileMat", enableInstancing = true };
            tile.SetFloat("_Smoothness", 0.25f);
            tile.SetFloat("_Metallic", 0f);
            tile.EnableKeyword("_EMISSION");
            tile.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            Save(tile, "TileMat");

            var glow = new Material(unlit) { name = "GlowMat", enableInstancing = true };
            Save(glow, "GlowMat");

            var additive = new Material(unlit) { name = "AdditiveMat" };
            MakeAdditive(additive);
            Save(additive, "AdditiveMat");

            var particleMat = new Material(particle != null ? particle : unlit) { name = "ParticleMat" };
            MakeAdditive(particleMat);
            Save(particleMat, "ParticleMat");

            AssetDatabase.SaveAssets();
            Debug.Log("[NeonZigzag] Materials generated at " + Dir);
        }

        /// <summary>Portrait-locks the player. Done through the API so Unity owns the write.</summary>
        static void ApplyPlayerSettingsOnce()
        {
            const string key = "NeonZigzag.PlayerSettingsApplied";
            if (EditorPrefs.GetBool(key, false)) return;
            EditorPrefs.SetBool(key, true);

            PlayerSettings.productName = "Neon Zigzag";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
        }

        static void MakeAdditive(Material m)
        {
            m.SetFloat("_Surface", 1f);                 // transparent
            m.SetFloat("_Blend", 2f);                   // additive
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        static void Save(Material m, string fileName)
        {
            AssetDatabase.CreateAsset(m, Dir + "/" + fileName + ".mat");
        }
    }
}
