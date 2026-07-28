using UnityEngine;

namespace NeonZigzag
{
    /// <summary>One cell of the path. Pops up on spawn, crumbles once the ball is well past it.</summary>
    public class Tile : MonoBehaviour
    {
        public const float Height = 0.35f;
        const float RiseTime = 0.22f;

        static MaterialPropertyBlock _block;

        public Vector2Int Cell { get; private set; }
        public int Progress => Cell.x + Cell.y;
        public bool IsBonus { get; private set; }

        MeshRenderer _renderer;
        Vector3 _rest;
        float _rise;
        bool _crumbling;
        float _fallVel;
        Vector3 _spin;

        public static Tile Create(Transform parent)
        {
            var go = new GameObject("Tile");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = NeonTheme.Cube;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = NeonTheme.TileMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var tile = go.AddComponent<Tile>();
            tile._renderer = mr;
            return tile;
        }

        public void Spawn(Vector2Int cell, Color color, bool bonus)
        {
            Cell = cell;
            IsBonus = bonus;
            _rest = new Vector3(cell.x, -Height * 0.5f, cell.y);
            _rise = 0f;
            _crumbling = false;
            _fallVel = 0f;

            var t = transform;
            t.localScale = new Vector3(0.94f, Height, 0.94f);
            t.rotation = Quaternion.identity;
            t.position = _rest + Vector3.down * 1.4f;

            _block ??= new MaterialPropertyBlock();
            _block.Clear();
            _block.SetColor("_BaseColor", color);
            _block.SetColor("_EmissionColor", color * (bonus ? 0.9f : 0.45f));
            _renderer.SetPropertyBlock(_block);

            gameObject.SetActive(true);
        }

        public void Crumble()
        {
            if (_crumbling) return;
            _crumbling = true;
            _fallVel = Random.Range(0.4f, 1.6f);
            _spin = Random.insideUnitSphere * Random.Range(90f, 260f);
        }

        /// <summary>Returns true once the tile has fallen far enough to be recycled.</summary>
        public bool Tick(float dt)
        {
            var t = transform;

            if (_crumbling)
            {
                _fallVel -= 26f * dt;
                t.position += Vector3.up * (_fallVel * dt);
                t.Rotate(_spin * dt, Space.World);
                t.localScale = Vector3.MoveTowards(t.localScale, Vector3.zero, dt * 0.9f);
                return t.position.y < -14f;
            }

            if (_rise < 1f)
            {
                _rise = Mathf.Min(1f, _rise + dt / RiseTime);
                float e = 1f - Mathf.Pow(1f - _rise, 3f);           // ease-out cubic
                t.position = Vector3.LerpUnclamped(_rest + Vector3.down * 1.4f, _rest, e);
            }
            return false;
        }

        public void Recycle()
        {
            gameObject.SetActive(false);
        }
    }
}
