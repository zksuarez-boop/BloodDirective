using System;
using UnityEngine;
using BloodDirective.Stats;

namespace BloodDirective.Enemies
{
    /// <summary>
    /// Runtime representation of a living enemy.
    /// Owns health, resistance-based damage mitigation, and death handling.
    /// Requires an EnemyData asset to be assigned before play.
    /// </summary>
    public class EnemyCharacter : MonoBehaviour
    {
        // ── Serialized ────────────────────────────────────────────────────────

        [SerializeField] private EnemyData _enemyData;

        // ── Runtime State ─────────────────────────────────────────────────────

        private float _currentHealth;
        private bool  _isDead;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired whenever current or maximum health changes.</summary>
        public Action<float, float> OnHealthChanged;

        /// <summary>Fired when the enemy's health reaches zero, before the GameObject is destroyed.</summary>
        public Action OnDeath;

        // ── Public Getters ────────────────────────────────────────────────────

        /// <summary>Whether this enemy has already died. Guards against double-death on the same frame.</summary>
        public bool IsDead => _isDead;

        /// <summary>Current health points.</summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>Maximum health points sourced from EnemyData.</summary>
        public float MaxHealth => _enemyData?.MaxHealth ?? 0f;

        /// <summary>The data asset driving this enemy instance.</summary>
        public EnemyData EnemyData => _enemyData;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_enemyData != null)
                _currentHealth = _enemyData.MaxHealth;
        }

        /// <summary>Initializes this enemy with the given data asset. Use when spawning at runtime.</summary>
        public void Initialize(EnemyData data)
        {
            _enemyData     = data;
            _currentHealth = data.MaxHealth;
            _isDead        = false;
        }

        // ── Damage ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies incoming damage after resistance mitigation.
        /// Mirrors PlayerCharacter.TakeDamage: resistance reduces damage, vulnerability increases it.
        /// Minimum 1 damage always applies. Triggers <see cref="Die"/> if health reaches zero.
        /// </summary>
        /// <param name="amount">Raw damage before mitigation.</param>
        /// <param name="damageType">Elemental or physical category of the incoming damage.</param>
        public void TakeDamage(float amount, DamageType damageType)
        {
            if (_isDead) return;

            float resistance  = GetResistance(damageType);
            float mitigated   = amount * (1f - resistance);
            float finalDamage = Mathf.Max(1f, mitigated);

            _currentHealth = Mathf.Max(0f, _currentHealth - finalDamage);
            OnHealthChanged?.Invoke(_currentHealth, _enemyData.MaxHealth);

            Debug.Log($"[EnemyCharacter] {_enemyData.EnemyName} took {finalDamage:F1} {damageType} damage. " +
                      $"HP: {_currentHealth:F1}/{_enemyData.MaxHealth}");

            if (_currentHealth <= 0f)
                Die();
        }

        // ── Resistance ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the enemy's resistance fraction for the given damage type.
        /// Sourced from the EnemyData asset.
        /// </summary>
        private float GetResistance(DamageType type) => type switch
        {
            DamageType.Solid   => _enemyData.SolidResistance,
            DamageType.Liquid  => _enemyData.LiquidResistance,
            DamageType.Gas     => _enemyData.GasResistance,
            DamageType.Plasma  => _enemyData.PlasmaResistance,
            DamageType.Psychic => _enemyData.PsychicResistance,
            _                  => 0f
        };

        // ── Death ─────────────────────────────────────────────────────────────

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log($"[EnemyCharacter] {_enemyData.EnemyName} has died.");
            OnDeath?.Invoke();
            Destroy(gameObject);
        }
    }
}
