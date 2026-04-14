using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BloodDirective.Player;

namespace BloodDirective.UI
{
    /// <summary>
    /// Screen-space HUD showing player HP, XP, level, and a death overlay with restart.
    /// Attach to any persistent GameObject — builds its own Canvas on Start.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        private Image      _hpFill;
        private Image      _xpFill;
        private Text       _hpText;
        private Text       _levelText;
        private GameObject _deathPanel;

        private void Start()
        {
            BuildHUD();

            var player = FindFirstObjectByType<PlayerCharacter>();
            if (player == null) return;

            player.OnHealthChanged += OnHPChanged;
            player.OnXPChanged     += OnXPChanged;
            player.OnLevelUp       += OnLevelUp;
            player.OnDeath         += ShowDeathScreen;

            // Seed with current values
            OnHPChanged(player.CurrentHealth, player.MaxHealth);
            OnXPChanged(player.CurrentXP, player.XPToNextLevel);
            OnLevelUp(player.Level);
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void BuildHUD()
        {
            var root = new GameObject("HUDCanvas");
            root.transform.SetParent(transform, false);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ── HP bar ────────────────────────────────────────────────────────
            var hpBG = Bar(root.transform, new Vector2(220, 22), new Vector2(10, 10),
                           new Color(0.12f, 0.03f, 0.03f, 0.9f));

            _hpFill = Fill(hpBG, new Color(0.82f, 0.10f, 0.10f));

            _hpText = Label(root.transform, "HP 100/100", font, 13,
                            new Vector2(238, 10), new Vector2(160, 22), TextAnchor.MiddleLeft);

            // ── XP bar ────────────────────────────────────────────────────────
            var xpBG = Bar(root.transform, new Vector2(220, 12), new Vector2(10, 38),
                           new Color(0.04f, 0.04f, 0.14f, 0.9f));

            _xpFill = Fill(xpBG, new Color(0.28f, 0.48f, 1.00f));

            // ── Level text ────────────────────────────────────────────────────
            _levelText = Label(root.transform, "LVL 1", font, 13,
                               new Vector2(10, 56), new Vector2(220, 16), TextAnchor.MiddleLeft);

            // ── Death overlay ─────────────────────────────────────────────────
            _deathPanel = Overlay(root.transform, new Color(0f, 0f, 0f, 0.82f));

            var died = Label(_deathPanel.transform, "YOU DIED", font, 72,
                             new Vector2(0, 60), new Vector2(600, 100), TextAnchor.MiddleCenter,
                             new Color(0.85f, 0.08f, 0.08f));
            CenterAnchor(died.rectTransform);

            var btn = Button(_deathPanel.transform, "RESTART", font,
                             new Vector2(0, -40), new Vector2(220, 52));
            btn.onClick.AddListener(() => SceneManager.LoadScene(
                SceneManager.GetActiveScene().buildIndex));

            _deathPanel.SetActive(false);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnHPChanged(float current, float max)
        {
            if (_hpFill)  _hpFill.fillAmount = max > 0f ? current / max : 0f;
            if (_hpText)  _hpText.text = $"HP  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void OnXPChanged(float current, float max)
        {
            if (_xpFill) _xpFill.fillAmount = max > 0f ? current / max : 0f;
        }

        private void OnLevelUp(int level)
        {
            if (_levelText) _levelText.text = $"LVL  {level}";
        }

        private void ShowDeathScreen()
        {
            if (_deathPanel) _deathPanel.SetActive(true);
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        /// <summary>Creates a background panel anchored to the bottom-left.</summary>
        private static GameObject Bar(Transform parent, Vector2 size, Vector2 pos, Color color)
        {
            var go  = new GameObject("Bar");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot      = Vector2.zero;
            rt.sizeDelta  = size;
            rt.anchoredPosition = pos;
            return go;
        }

        /// <summary>Creates a horizontal fill image that stretches inside its parent.</summary>
        private static Image Fill(GameObject parent, Color color)
        {
            var go  = new GameObject("Fill");
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color      = color;
            img.type       = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 1f;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        /// <summary>Creates a Text label anchored to the bottom-left.</summary>
        private static Text Label(Transform parent, string text, Font font, int size,
                                  Vector2 pos, Vector2 dimensions, TextAnchor anchor,
                                  Color? color = null)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text      = text;
            t.font      = font;
            t.fontSize  = size;
            t.color     = color ?? Color.white;
            t.alignment = anchor;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot      = Vector2.zero;
            rt.sizeDelta  = dimensions;
            rt.anchoredPosition = pos;
            return t;
        }

        /// <summary>Creates a full-screen stretched overlay panel.</summary>
        private static GameObject Overlay(Transform parent, Color color)
        {
            var go  = new GameObject("Overlay");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        /// <summary>Creates a centered button with label text.</summary>
        private static Button Button(Transform parent, string label, Font font,
                                     Vector2 pos, Vector2 size)
        {
            var go  = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.06f, 0.06f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot      = new Vector2(0.5f, 0.5f);
            rt.sizeDelta  = size;
            rt.anchoredPosition = pos;

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var t = txtGO.AddComponent<Text>();
            t.text      = label;
            t.font      = font;
            t.fontSize  = 20;
            t.color     = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            var trt = t.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            return btn;
        }

        private static void CenterAnchor(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot      = new Vector2(0.5f, 0.5f);
        }
    }
}
