using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NeonZigzag
{
    /// <summary>Fixed isometric camera that trails the ball, plus a little shake for impacts.</summary>
    public class CameraRig : MonoBehaviour
    {
        static readonly Quaternion Angle = Quaternion.Euler(38f, 45f, 0f);
        const float Distance = 24f;
        const float MinSize = 6.8f;

        /// <summary>
        /// The zigzag swings sideways by up to a full run length, so the framing is driven by the
        /// horizontal half-extent we must guarantee. Tall phones widen the ortho size instead of
        /// cropping the next corner out of view.
        /// </summary>
        const float RequiredHalfWidth = 4.5f;

        public Camera Camera { get; private set; }

        Vector3 _focus;
        float _shake;
        float _sizeBoost;

        public static CameraRig Create(Transform parent)
        {
            var go = new GameObject("CameraRig");
            go.transform.SetParent(parent, false);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(go.transform, false);

            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = MinSize;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = NeonTheme.Background;
            cam.transform.rotation = Angle;

            camGo.AddComponent<AudioListener>();   // the default scene camera carrying one was removed

            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            data.renderShadows = false;

            var rig = go.AddComponent<CameraRig>();
            rig.Camera = cam;
            return rig;
        }

        public void Snap(Vector3 target)
        {
            _focus = target;
            _shake = 0f;
            _sizeBoost = 0f;
            Apply(0f);
        }

        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);

        public void Tick(float dt, Vector3 target, float speed01)
        {
            // Lead the ball slightly so faster runs still show enough road ahead.
            _focus = Vector3.Lerp(_focus, target, 1f - Mathf.Exp(-9f * dt));
            _sizeBoost = Mathf.Lerp(_sizeBoost, speed01 * 1.1f, 1f - Mathf.Exp(-2f * dt));
            _shake = Mathf.Lerp(_shake, 0f, 1f - Mathf.Exp(-8f * dt));
            Apply(dt);
        }

        void Apply(float dt)
        {
            float aspect = Mathf.Max(0.35f, Camera.aspect);
            Camera.orthographicSize = Mathf.Max(MinSize, RequiredHalfWidth / aspect) + _sizeBoost;

            Vector3 offset = Angle * Vector3.back * Distance;
            Vector3 jitter = _shake > 0.001f
                ? (Vector3)(Random.insideUnitCircle * _shake)
                : Vector3.zero;

            Camera.transform.position = _focus + offset + Camera.transform.rotation * jitter;
        }

        /// <summary>Global bloom + vignette. Built in code so no profile asset needs wiring up.</summary>
        public static void CreatePostFx(Transform parent)
        {
            var go = new GameObject("PostFX");
            go.transform.SetParent(parent, false);
            go.layer = 0;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>(true);
            // Tight and bright rather than wide and hazy — high scatter washed out the whole frame.
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.85f);
            bloom.scatter.Override(0.55f);
            bloom.tint.Override(new Color(0.85f, 0.9f, 1f));

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.30f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.02f, 0.01f, 0.06f));

            var ca = profile.Add<ChromaticAberration>(true);
            ca.intensity.Override(0.07f);

            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.sharedProfile = profile;
        }
    }
}
