using UnityEngine;

namespace NeonZigzag
{
    /// <summary>
    /// The player. Always rolling along +X or +Z; a tap swaps between the two.
    /// No physics involved — position is integrated by hand and "am I still on the path?"
    /// is a dictionary lookup against the generated grid.
    /// </summary>
    public class BallController : MonoBehaviour
    {
        public const float Radius = 0.3f;
        const float CoyoteDistance = 0.34f;   // how far past a corner a late tap still works
        const float Gravity = 30f;

        public Vector3 Dir { get; private set; } = Vector3.right;
        public bool Falling { get; private set; }
        public float Speed { get; set; } = 7f;
        public float Progress => transform.position.x + transform.position.z;

        PathGenerator _path;
        Transform _body;
        Material _bodyMaterial;
        TrailRenderer _trail;
        float _fallVel;
        float _pop;

        public static BallController Create(Transform parent, PathGenerator path)
        {
            var go = new GameObject("Ball");
            go.transform.SetParent(parent, false);

            var body = new GameObject("Body").transform;
            body.SetParent(go.transform, false);
            body.localScale = Vector3.one * (Radius * 2f);
            body.gameObject.AddComponent<MeshFilter>().sharedMesh = NeonTheme.Sphere;
            var mr = body.gameObject.AddComponent<MeshRenderer>();
            var bodyMaterial = NeonTheme.NewGlow(SkinLibrary.All[0].Core);
            mr.sharedMaterial = bodyMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var trail = go.AddComponent<TrailRenderer>();
            trail.material = NeonTheme.AdditiveMaterial;
            trail.time = 0.4f;
            trail.widthCurve = AnimationCurve.EaseInOut(0f, 0.34f, 1f, 0f);
            trail.numCapVertices = 4;
            trail.alignment = LineAlignment.View;

            var ball = go.AddComponent<BallController>();
            ball._path = path;
            ball._body = body;
            ball._bodyMaterial = bodyMaterial;
            ball._trail = trail;
            ball.ApplySkin(SkinLibrary.All[0]);
            return ball;
        }

        public void ApplySkin(Skin skin)
        {
            _bodyMaterial.SetColor("_BaseColor", skin.Core);
            _bodyMaterial.color = skin.Core;
            _trail.colorGradient = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(skin.TrailNear, 0f),
                    new GradientColorKey(skin.TrailFar, 1f)
                },
                alphaKeys = new[] { new GradientAlphaKey(0.75f, 0f), new GradientAlphaKey(0f, 1f) }
            };
        }

        public void ResetBall()
        {
            transform.position = new Vector3(-2f, Radius, 0f);
            Dir = Vector3.right;
            Falling = false;
            _fallVel = 0f;
            _pop = 0f;
            _body.localScale = Vector3.one * (Radius * 2f);
            _trail.Clear();
        }

        /// <summary>Swaps travel direction. Returns true if the turn lands on a real tile.</summary>
        public bool RequestTurn()
        {
            Vector3 newDir = Dir == Vector3.right ? Vector3.forward : Vector3.right;
            Vector2Int step = newDir == Vector3.right ? new Vector2Int(1, 0) : new Vector2Int(0, 1);
            Vector3 pos = transform.position;
            _pop = 1f;

            for (float back = 0f; back <= CoyoteDistance; back += 0.06f)
            {
                Vector2Int cell = ToCell(pos - Dir * back);
                if (!_path.HasTile(cell + step)) continue;

                if (back > 0.001f)
                {
                    // Re-centre on the corner we just overshot so a late tap still reads clean.
                    if (Dir.x > 0.5f) pos.x = cell.x; else pos.z = cell.y;
                    transform.position = pos;
                }
                Dir = newDir;
                return true;
            }

            Dir = newDir;   // no tile that way: the player has committed to the mistake
            return false;
        }

        public void Tick(float dt)
        {
            var t = transform;

            if (Falling)
            {
                // Stop integrating once it is well out of frame; a game-over screen left open
                // would otherwise drift the transform into the thousands.
                if (t.position.y > -30f)
                {
                    _fallVel -= Gravity * dt;
                    t.position += Dir * (Speed * 0.35f * dt) + Vector3.up * (_fallVel * dt);
                }
            }
            else
            {
                t.position += Dir * (Speed * dt);
                if (!_path.HasTile(ToCell(t.position)))
                {
                    Falling = true;
                    _fallVel = 1.5f;
                }
            }

            if (_pop > 0f)
            {
                _pop = Mathf.Max(0f, _pop - dt * 5f);
                float s = 1f + Mathf.Sin(_pop * Mathf.PI) * 0.28f;
                _body.localScale = new Vector3(s, 2f - s, s) * (Radius * 2f);
            }
        }

        public void Idle(float dt)
        {
            float s = 1f + Mathf.Sin(Time.time * 4f) * 0.06f;
            _body.localScale = new Vector3(s, 2f - s, s) * (Radius * 2f);
        }

        static Vector2Int ToCell(Vector3 p) =>
            new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z));
    }
}
