using UnityEngine;
using UnityEngine.UI;

namespace NeonZigzag
{
    /// <summary>Runtime-built uGUI. Portrait-first, no buttons — the whole screen is the input.</summary>
    public class HudUI : MonoBehaviour
    {
        /// <summary>Bottom slice of the ready screen that cycles skins instead of starting a run.</summary>
        public const float SkinStripFraction = 0.16f;

        static Font _font;
        static Sprite _white;

        Text _score, _best, _combo, _runGems;
        Image _comboBar;
        CanvasGroup _comboGroup, _readyGroup, _overGroup;
        Text _readyHint, _overHint, _overScore, _overBest, _newBest;
        Text _gemTotal, _nextUnlock, _skinName, _overGems, _unlockBanner;
        RectTransform _scoreRect;
        float _punch;

        public static HudUI Create(Transform parent)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                    ?? Font.CreateDynamicFontFromOSFont("Arial", 48);
            _white = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));

            var go = new GameObject("HUD");
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var hud = go.AddComponent<HudUI>();
            hud.Build(go.transform);
            return hud;
        }

        void Build(Transform root)
        {
            _score = Label(root, "0", 150, NeonTheme.Ink, 130f);
            _scoreRect = _score.rectTransform;
            _best = Label(root, "BEST 0", 44, NeonTheme.Dim, 296f);
            _runGems = Label(root, "", 40, new Color(1f, 0.82f, 0.3f), 362f);

            // Combo readout: multiplier plus a bar that drains while the window closes.
            var comboGo = NewRect("Combo", root, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                  new Vector2(0.5f, 1f), new Vector2(0f, -434f), new Vector2(-80f, 130f));
            _comboGroup = comboGo.gameObject.AddComponent<CanvasGroup>();
            _comboGroup.alpha = 0f;
            _combo = Label(comboGo, "x1.0", 62, new Color(1f, 0.82f, 0.3f), 0f);

            var barBg = NewRect("BarBg", comboGo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(320f, 10f));
            var bgImage = barBg.gameObject.AddComponent<Image>();
            bgImage.sprite = _white;
            bgImage.color = new Color(1f, 1f, 1f, 0.14f);
            bgImage.raycastTarget = false;

            var barFill = NewRect("BarFill", barBg, Vector2.zero, Vector2.one,
                                  new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            barFill.offsetMin = Vector2.zero;
            barFill.offsetMax = Vector2.zero;
            _comboBar = barFill.gameObject.AddComponent<Image>();
            _comboBar.sprite = _white;
            _comboBar.color = new Color(1f, 0.78f, 0.25f, 0.95f);
            _comboBar.type = Image.Type.Filled;
            _comboBar.fillMethod = Image.FillMethod.Horizontal;
            _comboBar.raycastTarget = false;

            // Ready screen
            var ready = FullScreen("Ready", root);
            _readyGroup = ready.gameObject.AddComponent<CanvasGroup>();
            LabelCenter(ready, "NEON", 150, NeonTheme.Ink, -260f);
            LabelCenter(ready, "ZIGZAG", 150, new Color(1f, 0.45f, 0.95f), -110f);
            _readyHint = LabelCenter(ready, "TAP TO START", 62, NeonTheme.Ink, 110f);
            LabelCenter(ready, "TAP TO TURN AT EVERY CORNER", 38, NeonTheme.Dim, 220f);
            LabelCenter(ready, "CUT INTO THE PINK LANE FOR GEMS", 38, new Color(1f, 0.55f, 0.9f, 0.8f), 275f);
            _gemTotal = LabelCenter(ready, "0 GEMS", 46, new Color(1f, 0.82f, 0.3f), 390f);
            _nextUnlock = LabelCenter(ready, "", 34, NeonTheme.Dim, 445f);

            // Bottom strip doubles as the skin selector so the game still needs only taps.
            var strip = NewRect("SkinStrip", ready, Vector2.zero, new Vector2(1f, 0f),
                                new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 250f));
            var stripBg = strip.gameObject.AddComponent<Image>();
            stripBg.sprite = _white;
            stripBg.color = new Color(1f, 1f, 1f, 0.05f);
            stripBg.raycastTarget = false;
            _skinName = Label(strip, "< PULSE >", 58, NeonTheme.Ink, 70f);
            Label(strip, "TAP HERE TO CHANGE SKIN", 32, NeonTheme.Dim, 150f);

            // Game over screen
            var over = FullScreen("GameOver", root);
            _overGroup = over.gameObject.AddComponent<CanvasGroup>();
            _overGroup.alpha = 0f;
            var shade = over.gameObject.AddComponent<Image>();
            shade.sprite = _white;
            shade.color = new Color(0.02f, 0.02f, 0.07f, 0.72f);
            shade.raycastTarget = false;
            LabelCenter(over, "GAME OVER", 84, NeonTheme.Ink, -300f);
            _overScore = LabelCenter(over, "0", 170, new Color(1f, 0.82f, 0.3f), -120f);
            _newBest = LabelCenter(over, "NEW BEST!", 52, new Color(0.5f, 1f, 0.8f), 20f);
            _overBest = LabelCenter(over, "BEST 0", 46, NeonTheme.Dim, 100f);
            _overGems = LabelCenter(over, "", 52, new Color(1f, 0.82f, 0.3f), 200f);
            _unlockBanner = LabelCenter(over, "", 46, new Color(0.6f, 1f, 0.9f), 265f);
            _overHint = LabelCenter(over, "TAP TO RETRY", 62, NeonTheme.Ink, 380f);
        }

        public void ShowReady()
        {
            _readyGroup.alpha = 1f;
            _overGroup.alpha = 0f;
            RefreshMeta();
        }

        public void HideReady() => _readyGroup.alpha = 0f;

        /// <summary>Pulls the ready-screen texts from persisted progression.</summary>
        public void RefreshMeta()
        {
            _gemTotal.text = Progression.Gems + " GEMS";
            _skinName.text = "< " + Progression.Current.Name + " >";

            int next = Progression.NextLockedIndex;
            _nextUnlock.text = next < 0
                ? "ALL SKINS UNLOCKED"
                : (SkinLibrary.All[next].GemsRequired - Progression.Gems) + " MORE TO UNLOCK " +
                  SkinLibrary.All[next].Name;
        }

        public void ShowGameOver(int score, int best, bool isNewBest, int gemsEarned, int unlockedSkin)
        {
            _overScore.text = score.ToString();
            _overBest.text = "BEST " + best;
            _newBest.enabled = isNewBest;
            _overGems.text = gemsEarned > 0 ? "+" + gemsEarned + " GEMS" : "";
            _unlockBanner.text = unlockedSkin >= 0
                ? "SKIN UNLOCKED: " + SkinLibrary.All[unlockedSkin].Name
                : "";
        }

        public void PunchScore() => _punch = 1f;

        public void Tick(float dt, int score, int best, int runGems, float multiplier, float comboFill, bool dead)
        {
            _score.text = score.ToString();
            _best.text = "BEST " + best;
            _runGems.text = runGems > 0 ? runGems + " GEMS" : "";

            _punch = Mathf.Max(0f, _punch - dt * 4f);
            float s = 1f + Mathf.Sin(_punch * Mathf.PI) * 0.18f;
            _scoreRect.localScale = new Vector3(s, s, 1f);

            bool comboOn = multiplier > 1.001f;
            _comboGroup.alpha = Mathf.MoveTowards(_comboGroup.alpha, comboOn ? 1f : 0f, dt * 5f);
            if (comboOn)
            {
                // Invariant so the separator is a dot on every device locale.
                _combo.text = "x" + multiplier.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                _comboBar.fillAmount = comboFill;
            }

            float pulse = 0.55f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f)) * 0.45f;
            SetAlpha(_readyHint, pulse);
            SetAlpha(_overHint, pulse);

            _overGroup.alpha = Mathf.MoveTowards(_overGroup.alpha, dead ? 1f : 0f, dt * 3.5f);
        }

        static void SetAlpha(Text text, float a)
        {
            var c = text.color;
            c.a = a;
            text.color = c;
        }

        static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                     Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            return rt;
        }

        static RectTransform FullScreen(string name, Transform parent)
        {
            var rt = NewRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                             Vector2.zero, Vector2.zero);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Text Label(Transform parent, string value, int size, Color color, float distanceFromTop)
        {
            var rt = NewRect("Label", parent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(0.5f, 1f), new Vector2(0f, -distanceFromTop),
                             new Vector2(-80f, size * 1.35f));
            return Fill(rt, value, size, color);
        }

        static Text LabelCenter(Transform parent, string value, int size, Color color, float offsetY)
        {
            var rt = NewRect("Label", parent, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                             new Vector2(0.5f, 0.5f), new Vector2(0f, -offsetY),
                             new Vector2(-80f, size * 1.35f));
            return Fill(rt, value, size, color);
        }

        static Text Fill(RectTransform rt, string value, int size, Color color)
        {
            var text = rt.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
