using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace NeonZigzag
{
    public enum GameState { Ready, Playing, Dead }

    /// <summary>
    /// Owns the run: builds every subsystem, drives them in a fixed order each frame,
    /// and holds score / combo / difficulty.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        const float WarmupDistance = 80f;    // fully easy while the player finds the rhythm
        const float RampDistance = 650f;     // distance from the end of warmup to full difficulty
        const float RampCurve = 1.4f;        // >1 keeps the early ramp shallow and back-loads the pressure
        const float SlowSpeed = 6f;
        const float FastSpeed = 12.5f;
        const float ComboWindow = 5f;
        const int ComboCap = 20;
        const float RetryDelay = 0.65f;

        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Ready;
        public float Difficulty01
        {
            get
            {
                float past = Mathf.Max(0f, _distance - WarmupDistance);
                return Mathf.Pow(Mathf.Clamp01(past / RampDistance), RampCurve);
            }
        }
        public float Multiplier => 1f + Mathf.Min(_combo, ComboCap) * 0.15f;

        PathGenerator _path;
        BallController _ball;
        CameraRig _rig;
        HudUI _hud;
        Sfx _sfx;
        NeonFx _fx;

        float _distance;
        float _score;
        float _lastProgress;
        float _comboTimer;
        float _deadTimer;
        int _combo;
        int _best;
        int _gemsThisRun;

        void Awake()
        {
            Instance = this;
            _best = PlayerPrefs.GetInt("neon_best", 0);
            Progression.Load();

            SetupEnvironment();

            CameraRig.CreatePostFx(transform);
            _rig = CameraRig.Create(transform);

            var pathGo = new GameObject("Path");
            pathGo.transform.SetParent(transform, false);
            _path = pathGo.AddComponent<PathGenerator>();

            _ball = BallController.Create(transform, _path);
            _hud = HudUI.Create(transform);
            _sfx = Sfx.Create(transform);
            _fx = NeonFx.Create(transform);

            ResetRun();
        }

        void SetupEnvironment()
        {
            var lightGo = new GameObject("Key Light");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(0.85f, 0.9f, 1f);
            light.shadows = LightShadows.None;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = NeonTheme.Ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = NeonTheme.Background;
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 68f;
        }

        void ResetRun()
        {
            _path.ResetPath();
            _ball.ResetBall();
            _ball.ApplySkin(Progression.Current);
            _ball.Speed = SlowSpeed;

            _distance = 0f;
            _score = 0f;
            _gemsThisRun = 0;
            _combo = 0;
            _comboTimer = 0f;
            _deadTimer = 0f;
            _lastProgress = _ball.Progress;

            _rig.Snap(Focus());
            State = GameState.Ready;
            _hud.ShowReady();
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);   // a hitch should never teleport the ball off the path
            Vector2 tapPos;
            bool tapped = ReadTap(out tapPos);

            switch (State)
            {
                case GameState.Ready:
                    _ball.Idle(dt);
                    _path.Tick(dt, _ball.Progress);
                    if (tapped)
                    {
                        if (tapPos.y < Screen.height * HudUI.SkinStripFraction) CycleSkin();
                        else BeginRun();
                    }
                    break;

                case GameState.Playing:
                    TickRun(dt, tapped);
                    break;

                case GameState.Dead:
                    _deadTimer += dt;
                    _ball.Tick(dt);
                    _path.Tick(dt, _ball.Progress);
                    if (tapped && _deadTimer > RetryDelay) ResetRun();
                    break;
            }

            _rig.Tick(dt, Focus(), Difficulty01);
            _hud.Tick(dt, Mathf.FloorToInt(_score), _best, _gemsThisRun, Multiplier,
                      Mathf.Clamp01(_comboTimer / ComboWindow), State == GameState.Dead);
        }

        void CycleSkin()
        {
            Progression.CycleSkin();
            _ball.ApplySkin(Progression.Current);
            _hud.RefreshMeta();
            _sfx.Turn();
        }

        void BeginRun()
        {
            State = GameState.Playing;
            _lastProgress = _ball.Progress;
            _hud.HideReady();
            _sfx.Start();
        }

        void TickRun(float dt, bool tapped)
        {
            if (tapped)
            {
                _ball.RequestTurn();
                _sfx.Turn();
            }

            _ball.Speed = Mathf.Lerp(SlowSpeed, FastSpeed, Difficulty01);
            _ball.Tick(dt);

            _path.EnsureAhead(_ball.Progress);
            _path.Tick(dt, _ball.Progress);
            CollectOrbs();

            float progress = _ball.Progress;
            float gained = Mathf.Max(0f, progress - _lastProgress);
            _lastProgress = progress;
            _distance += gained;
            _score += gained * Multiplier * 1.5f;

            if (_combo > 0)
            {
                _comboTimer -= dt;
                if (_comboTimer <= 0f) _combo = 0;
            }

            if (_ball.Falling) Die();
        }

        void CollectOrbs()
        {
            var orbs = _path.Orbs;
            Vector3 ballPos = _ball.transform.position;

            for (int i = orbs.Count - 1; i >= 0; i--)
            {
                Vector3 delta = orbs[i].transform.position - ballPos;
                delta.y = 0f;
                if (delta.sqrMagnitude > Orb.PickupRadius * Orb.PickupRadius) continue;

                _combo++;
                _gemsThisRun++;
                _comboTimer = ComboWindow;
                _score += 12f * Multiplier;
                _fx.Pickup(orbs[i].transform.position);
                _sfx.Pickup(_combo);
                _rig.Shake(0.07f);
                _hud.PunchScore();
                _path.RemoveOrb(i);
            }
        }

        void Die()
        {
            State = GameState.Dead;
            _deadTimer = 0f;
            _combo = 0;

            int score = Mathf.FloorToInt(_score);
            bool isNewBest = score > _best;
            if (isNewBest)
            {
                _best = score;
                PlayerPrefs.SetInt("neon_best", _best);
                PlayerPrefs.Save();
            }

            int unlockedSkin = Progression.AddGems(_gemsThisRun);

            _fx.Death(_ball.transform.position);
            _rig.Shake(0.4f);
            _sfx.Die();
            _hud.ShowGameOver(score, _best, isNewBest, _gemsThisRun, unlockedSkin);
        }

        /// <summary>
        /// Look slightly down-path so the ball sits low on screen and more road is visible.
        /// The lead is along (1,0,1), which projects to a pure vertical shift under the 45° camera.
        /// </summary>
        Vector3 Focus()
        {
            Vector3 p = _ball.transform.position;
            const float lead = 2.2f;
            return new Vector3(p.x + lead, 0f, p.z + lead);
        }

        /// <summary>Screen-space tap position comes back too, so the ready screen can zone the input.</summary>
        static bool ReadTap(out Vector2 position)
        {
            position = Vector2.zero;

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touches = touchscreen.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (!touches[i].press.wasPressedThisFrame) continue;
                    position = touches[i].position.ReadValue();
                    return true;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                position = mouse.position.ReadValue();
                return true;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);   // never the skin strip
                return true;
            }

            return false;
        }
    }
}
