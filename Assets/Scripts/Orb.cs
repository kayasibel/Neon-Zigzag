using UnityEngine;

namespace NeonZigzag
{
    /// <summary>A collectible sitting on the bonus branch. Feeds the combo multiplier.</summary>
    public class Orb : MonoBehaviour
    {
        public const float PickupRadius = 0.55f;

        public Vector2Int Cell { get; private set; }
        public int Progress => Cell.x + Cell.y;

        Transform _body;
        float _phase;

        public static Orb Create(Transform parent)
        {
            var go = new GameObject("Orb");
            go.transform.SetParent(parent, false);

            var body = new GameObject("Body").transform;
            body.SetParent(go.transform, false);
            body.localScale = Vector3.one * 0.3f;
            var mf = body.gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = NeonTheme.Sphere;
            var mr = body.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = NeonTheme.NewGlow(NeonTheme.Orb * 3.2f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var orb = go.AddComponent<Orb>();
            orb._body = body;
            return orb;
        }

        public void Spawn(Vector2Int cell)
        {
            Cell = cell;
            _phase = Random.value * 6.28f;
            transform.position = new Vector3(cell.x, 0.55f, cell.y);
            gameObject.SetActive(true);
        }

        public void Tick(float dt)
        {
            _phase += dt * 3.4f;
            _body.localPosition = Vector3.up * (Mathf.Sin(_phase) * 0.09f);
            _body.Rotate(0f, 190f * dt, 0f, Space.Self);
        }

        public void Recycle()
        {
            gameObject.SetActive(false);
        }
    }
}
