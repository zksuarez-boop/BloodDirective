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
            HandleMovementInput();
            HandleAttackInput();
        }

        // ── Input Handlers ────────────────────────────────────────────────────

        private void HandleMovementInput()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _groundLayer)) return;

            _combat.ClearTarget();
            _character.MoveTo(hit.point);
            ShowClickIndicator(hit.point);
        }

        private void HandleAttackInput()
        {
            if (Mouse.current == null || !Mouse.current.rightButton.wasPressedThisFrame) return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _enemyLayer)) return;

            if (!hit.collider.TryGetComponent<EnemyCharacter>(out var enemy)) return;

            _combat.SetTarget(enemy);
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
