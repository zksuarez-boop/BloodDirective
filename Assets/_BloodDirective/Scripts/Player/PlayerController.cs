using UnityEngine;
using UnityEngine.InputSystem;
using BloodDirective.Combat;
using BloodDirective.Enemies;

namespace BloodDirective.Player
{
    /// <summary>
    /// Handles player input for click-to-move navigation and attack targeting.
    /// Uses Unity's new Input System — Mouse.current for button state and cursor position.
    /// Requires a <see cref="PlayerCharacter"/> and <see cref="CombatController"/> on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(PlayerCharacter))]
    [RequireComponent(typeof(CombatController))]
    public class PlayerController : MonoBehaviour
    {
        // ── Serialized ────────────────────────────────────────────────────────

        [SerializeField] private LayerMask    _groundLayer;
        [SerializeField] private LayerMask    _enemyLayer;
        [SerializeField] private GameObject   _clickIndicatorPrefab;

        // ── Private ───────────────────────────────────────────────────────────

        private PlayerCharacter  _character;
        private CombatController _combat;
        private Camera           _mainCamera;
        private GameObject       _activeIndicator;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            _character  = GetComponent<PlayerCharacter>();
            _combat     = GetComponent<CombatController>();
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleLeftClick();
        }

        // ── Input Handlers ────────────────────────────────────────────────────

        /// <summary>
        /// Left-click: attack if cursor is over an enemy, otherwise move to ground.
        /// Casts against all layers and checks for EnemyCharacter component directly,
        /// so layer mask misconfiguration can never silently block combat.
        /// </summary>
        private void HandleLeftClick()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity)) return;

            // If we hit something with an EnemyCharacter — attack it
            if (hit.collider.TryGetComponent<EnemyCharacter>(out var enemy))
            {
                if (!enemy.IsDead)
                    _combat.SetTarget(enemy);
                return;
            }

            // Otherwise treat the click as ground movement
            _combat.ClearTarget();
            _character.MoveTo(hit.point);
            ShowClickIndicator(hit.point);
        }

        // ── Click Indicator ───────────────────────────────────────────────────

        private void ShowClickIndicator(Vector3 position)
        {
            if (_clickIndicatorPrefab == null) return;

            if (_activeIndicator != null)
                Destroy(_activeIndicator);

            Vector3 spawnPos = position + Vector3.up * 0.05f;
            _activeIndicator = Instantiate(_clickIndicatorPrefab, spawnPos, Quaternion.identity);
            Destroy(_activeIndicator, 0.5f);
        }
    }
}
