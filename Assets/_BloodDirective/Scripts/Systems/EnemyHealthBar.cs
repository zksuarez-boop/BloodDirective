using UnityEngine;
using UnityEngine.UI;
using BloodDirective.Enemies;

namespace BloodDirective.Systems
{
    /// <summary>
    /// Floating world-space health bar above an enemy.
    /// Auto-creates its own Canvas and fills on spawn; subscribes to EnemyCharacter events.
    /// </summary>
    [RequireComponent(typeof(EnemyCharacter))]
    public class EnemyHealthBar : MonoBehaviour
    {
        private Image     _fill;
        private Transform _canvasTransform;

        private void Start()
        {
            var enemy = GetComponent<EnemyCharacter>();
            BuildBar();
            enemy.OnHealthChanged += UpdateBar;
            UpdateBar(enemy.CurrentHealth, enemy.MaxHealth);
        }

        private void BuildBar()
        {
            var canvasGO = new GameObject("HealthBar");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            canvasGO.transform.localScale    = new Vector3(0.012f, 0.012f, 0.012f);
            _canvasTransform = canvasGO.transform;

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();

            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 18f);

            // Dark background
            var bg = new GameObject("BG").AddComponent<Image>();
            bg.transform.SetParent(canvasGO.transform, false);
            bg.color = new Color(0.15f, 0.04f, 0.04f, 0.85f);
            bg.rectTransform.sizeDelta      = new Vector2(200f, 18f);
            bg.rectTransform.anchoredPosition = Vector2.zero;

            // Red fill — Filled type so fillAmount drives width cleanly
            var fillGO = new GameObject("Fill").AddComponent<Image>();
            fillGO.transform.SetParent(canvasGO.transform, false);
            fillGO.color = new Color(0.85f, 0.1f, 0.1f);
            fillGO.type        = Image.Type.Filled;
            fillGO.fillMethod  = Image.FillMethod.Horizontal;
            fillGO.fillAmount  = 1f;
            fillGO.rectTransform.sizeDelta      = new Vector2(200f, 18f);
            fillGO.rectTransform.anchoredPosition = Vector2.zero;
            _fill = fillGO;
        }

        private void UpdateBar(float current, float max)
        {
            if (_fill == null) return;
            _fill.fillAmount = max > 0f ? current / max : 0f;
        }

        private void LateUpdate()
        {
            // Billboard — always face camera
            if (_canvasTransform != null && Camera.main != null)
                _canvasTransform.rotation = Camera.main.transform.rotation;
        }
    }
}
