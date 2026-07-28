using UnityEngine;

namespace NeonZigzag
{
    /// <summary>
    /// Entry point. The game builds its own scene at runtime, so any scene works as the
    /// startup scene and there is nothing to wire up in the Inspector.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.Portrait;

            // The default scene ships with a camera and a light; the game supplies its own.
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                cam.enabled = false;
                Object.Destroy(cam.gameObject);
            }
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                light.enabled = false;
                Object.Destroy(light.gameObject);
            }

            new GameObject("[NeonZigzag]").AddComponent<GameManager>();
        }
    }
}
