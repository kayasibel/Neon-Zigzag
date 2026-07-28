using System.Collections.Generic;
using UnityEngine;

namespace NeonZigzag
{
    /// <summary>
    /// Builds the endless path one segment at a time. Movement is always +X or +Z, so
    /// (x + z) is a monotonic "progress" value we use for generation, crumbling and scoring.
    ///
    /// Most segments are a plain run followed by a corner. Occasionally the path forks into a
    /// diamond: cruising straight is the safe line, snapping onto the short bonus leg pays orbs.
    /// Both legs rejoin at the same merge cell, so either choice stays playable.
    /// </summary>
    public class PathGenerator : MonoBehaviour
    {
        const int LookAhead = 34;
        const int CrumbleBehind = 7;
        const int RecycleBehind = 18;

        static readonly Vector2Int[] AxisDirs = { new Vector2Int(1, 0), new Vector2Int(0, 1) };

        readonly Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>(512);
        readonly List<Tile> _live = new List<Tile>(512);
        readonly Stack<Tile> _tilePool = new Stack<Tile>(256);
        readonly List<Orb> _orbs = new List<Orb>(64);
        readonly Stack<Orb> _orbPool = new Stack<Orb>(32);
        readonly List<Tile> _sweep = new List<Tile>(64);

        Transform _tileRoot, _orbRoot;
        Vector2Int _head;
        int _axis;
        int _sinceFork;

        public IReadOnlyList<Orb> Orbs => _orbs;
        public int FrontProgress => _head.x + _head.y;

        void Awake()
        {
            _tileRoot = new GameObject("Tiles").transform;
            _tileRoot.SetParent(transform, false);
            _orbRoot = new GameObject("Orbs").transform;
            _orbRoot.SetParent(transform, false);
        }

        public bool HasTile(Vector2Int cell) => _tiles.ContainsKey(cell);

        public void ResetPath()
        {
            foreach (var tile in _live) { tile.Recycle(); _tilePool.Push(tile); }
            _live.Clear();
            _tiles.Clear();
            foreach (var orb in _orbs) { orb.Recycle(); _orbPool.Push(orb); }
            _orbs.Clear();

            _head = Vector2Int.zero;
            _axis = 0;
            _sinceFork = 0;

            // A one-wide runway. Anything wider would leave cells with no continuation,
            // which reads as an unfair trap rather than a missed corner.
            for (int x = -4; x <= 2; x++) Place(new Vector2Int(x, 0), false);
            _head = new Vector2Int(2, 0);

            EnsureAhead(0);
        }

        public void EnsureAhead(float ballProgress)
        {
            int guard = 0;
            while (FrontProgress < ballProgress + LookAhead && guard++ < 64)
                GenerateSegment();
        }

        public void Tick(float dt, float ballProgress)
        {
            _sweep.Clear();
            for (int i = 0; i < _live.Count; i++)
            {
                var tile = _live[i];
                if (tile.Progress < ballProgress - CrumbleBehind) tile.Crumble();
                bool spent = tile.Tick(dt) || tile.Progress < ballProgress - RecycleBehind;
                if (spent) _sweep.Add(tile);
            }

            for (int i = 0; i < _sweep.Count; i++)
            {
                var tile = _sweep[i];
                _live.Remove(tile);
                if (_tiles.TryGetValue(tile.Cell, out var owner) && owner == tile) _tiles.Remove(tile.Cell);
                tile.Recycle();
                _tilePool.Push(tile);
            }

            for (int i = _orbs.Count - 1; i >= 0; i--)
            {
                var orb = _orbs[i];
                orb.Tick(dt);
                if (orb.Progress < ballProgress - CrumbleBehind) RemoveOrb(i);
            }
        }

        public void RemoveOrb(int index)
        {
            var orb = _orbs[index];
            _orbs.RemoveAt(index);
            orb.Recycle();
            _orbPool.Push(orb);
        }

        void GenerateSegment()
        {
            float t = GameManager.Instance != null ? GameManager.Instance.Difficulty01 : 0f;
            // Runs stay short enough that the next corner is always inside the camera's
            // horizontal window (see CameraRig.RequiredHalfWidth).
            int minRun = Mathf.RoundToInt(Mathf.Lerp(3f, 2f, t));
            int maxRun = Mathf.RoundToInt(Mathf.Lerp(6f, 3f, t));
            float forkChance = Mathf.Lerp(0.08f, 0.40f, t);

            Vector2Int turn = AxisDirs[_axis];          // direction the player must turn onto
            Vector2Int straight = AxisDirs[1 - _axis];  // direction the ball is already travelling

            if (_sinceFork >= 2 && Random.value < forkChance)
            {
                int shortLeg = Random.Range(2, 4);
                int longLeg = Mathf.Clamp(Random.Range(maxRun - 1, maxRun + 2), 3, 5);
                var merge = _head + turn * shortLeg + straight * longLeg;

                // Safe line: keep cruising, then one relaxed corner.
                for (int j = 1; j <= longLeg; j++) Place(_head + straight * j, false);
                for (int i = 1; i <= shortLeg; i++) Place(_head + straight * longLeg + turn * i, false);

                // Bonus line: snap-turn now, then a second turn only a couple of tiles later.
                var bonusCells = new List<Vector2Int>(shortLeg + longLeg);
                for (int i = 1; i <= shortLeg; i++) bonusCells.Add(_head + turn * i);
                for (int j = 1; j <= longLeg; j++) bonusCells.Add(_head + turn * shortLeg + straight * j);
                foreach (var cell in bonusCells) Place(cell, true);

                for (int i = 0; i < bonusCells.Count; i++)
                {
                    var cell = bonusCells[i];
                    if (cell == merge) continue;
                    if (i % 2 == 0) SpawnOrb(cell);
                }

                _head = merge;
                _axis = Random.value < 0.5f ? _axis : 1 - _axis;
                _sinceFork = 0;
                return;
            }

            int run = Random.Range(minRun, maxRun + 1);
            for (int i = 1; i <= run; i++) Place(_head + turn * i, false);
            if (run >= 3 && Random.value < 0.3f) SpawnOrb(_head + turn * Random.Range(1, run));

            _head += turn * run;
            _axis = 1 - _axis;
            _sinceFork++;
        }

        void Place(Vector2Int cell, bool bonus)
        {
            if (_tiles.ContainsKey(cell)) return;

            var tile = _tilePool.Count > 0 ? _tilePool.Pop() : Tile.Create(_tileRoot);
            int progress = cell.x + cell.y;
            tile.Spawn(cell, bonus ? NeonTheme.BonusColor(progress) : NeonTheme.PathColor(progress), bonus);
            _tiles[cell] = tile;
            _live.Add(tile);
        }

        void SpawnOrb(Vector2Int cell)
        {
            var orb = _orbPool.Count > 0 ? _orbPool.Pop() : Orb.Create(_orbRoot);
            orb.Spawn(cell);
            _orbs.Add(orb);
        }
    }
}
