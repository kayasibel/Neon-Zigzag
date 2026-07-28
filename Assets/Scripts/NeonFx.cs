using UnityEngine;

namespace NeonZigzag
{
    /// <summary>Two burst-only particle systems, driven by explicit Emit calls.</summary>
    public class NeonFx : MonoBehaviour
    {
        ParticleSystem _sparks;
        ParticleSystem _debris;

        public static NeonFx Create(Transform parent)
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<NeonFx>();
            fx._sparks = Build(go.transform, "Sparks", 0.55f, 7f, 0.16f, 0.6f);
            fx._debris = Build(go.transform, "Debris", 1.1f, 11f, 0.24f, 1.6f);
            return fx;
        }

        public void Pickup(Vector3 position)
        {
            Emit(_sparks, position, NeonTheme.Orb * 2.6f, 16);
        }

        public void Death(Vector3 position)
        {
            Emit(_debris, position, new Color(1.4f, 1.8f, 2.6f), 40);
        }

        static void Emit(ParticleSystem ps, Vector3 position, Color color, int count)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = position,
                startColor = color,
                applyShapeToPosition = true
            };
            ps.Emit(p, count);
        }

        static ParticleSystem Build(Transform parent, string name, float life, float speed,
                                    float size, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // a fresh system auto-plays

            var main = ps.main;
            main.playOnAwake = false;
            main.maxParticles = 600;
            main.startLifetime = life;
            main.startSpeed = speed;
            main.startSize = size;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            colorOverLife.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.4f), new GradientAlphaKey(0f, 1f) }
            });

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = NeonTheme.ParticleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Play();
            return ps;
        }
    }
}
