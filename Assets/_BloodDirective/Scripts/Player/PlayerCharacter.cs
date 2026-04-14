using System;
using UnityEngine;
using UnityEngine.AI;
using BloodDirective.Data;
using BloodDirective.Stats;

namespace BloodDirective.Player
{
    /// <summary>
    /// Runtime representation of a living player character.
    /// Owns the StatSheet, health/resource pools, XP/levelling, and NavMesh movement.
    /// Requires a CharacterData asset to be assigned before play.
    /// </summary>
    public class PlayerCharacter : MonoBehaviour
    {
        // ── Serialized ────────────────────────────────────────────────────────

        [SerializeField] private CharacterData _characterData;

        // ── Runtime State ─────────────────────────────────────────────────────

        private StatSheet _stats;
        private NavMeshAgent _agent;
        private float _currentHealth;
        private float _currentResource;
        private int   _currentLevel    = 1;
        private int   _statPoints      = 0;
        private float _currentXP       = 0f;
        private float _xpToNextLevel   = 100f;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired whenever current or maximum health changes.</summary>
        public Action<float, float> OnHealthChanged;

        /// <summary>Fired whenever current or maximum resource changes.</summary>
        public Action<float, float> OnResourceChanged;

        /// <summary>Fired when the character gains a level, passing the new level.</summary>
        public Action<int> OnLevelUp;

        /// <summary>Fired when the character's health reaches zero.</summary>
        public Action OnDeath;

        // ── Public Getters ────────────────────────────────────────────────────

        /// <summary>Current health points.</summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>Maximum health points derived from the active StatSheet.</summary>
        public float MaxHealth => _stats?.MaxHealth ?? 0f;

        /// <summary>Current resource points (Op Points, Rage, Psi, etc.).</summary>
        public float CurrentResource => _currentResource;

        /// <summary>Maximum resource capacity defined by CharacterData.</summary>
        public float MaxResource => _characterData?.MaxResource ?? 0f;

        /// <summary>Current character level.</summary>
        public int Level => _currentLevel;

        /// <summary>Unspent stat allocation points.</summary>
        public int StatPoints => _statPoints;

        /// <summary>XP accumulated toward the next level.</summary>
        public float CurrentXP => _currentXP;

        /// <summary>XP threshold required to reach the next level.</summary>
        public float XPToNextLevel => _xpToNextLevel;

        /// <summary>The active StatSheet holding all six stats and derived values.</summary>
        public StatSheet Stats => _stats;

        /// <summary>The ScriptableObject data asset driving this character.</summary>
        public CharacterData CharacterData => _characterData;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();

            if (_characterData != null)
                InitializeCharacter();
        }

        // ── Initialization ────────────────────────────────────────────────────

        /// <summary>
        /// Builds the StatSheet from CharacterData and resets all runtime pools.
        /// Safe to call at runtime when swapping CharacterData assets.
        /// </summary>
        public void InitializeCharacter()
        {
            _stats           = _characterData.CreateStatSheet();
            _currentHealth   = _stats.MaxHealth;
            _currentResource = _characterData.MaxResource;

            // Agent may be disabled at startup until NavMeshRuntimeBaker re-enables it.
            if (_agent != null && _agent.isActiveAndEnabled)
                _agent.speed = _characterData.BaseMoveSpeed * _stats.MoveSpeedMultiplier;
        }

        /// <summary>Called by NavMeshRuntimeBaker after the NavMesh is ready.</summary>
        public void OnNavMeshReady()
        {
            if (_agent != null && _characterData != null && _stats != null)
                _agent.speed = _characterData.BaseMoveSpeed * _stats.MoveSpeedMultiplier;
        }

        // ── Movement ──────────────────────────────────────────────────────────

        /// <summary>Commands the NavMeshAgent to path toward the given world position.</summary>
        /// <param name="destination">Target world-space position.</param>
        public void MoveTo(Vector3 destination)
        {
            if (_agent == null || !_agent.isOnNavMesh) return;
            _agent.SetDestination(destination);
        }

        // ── Health ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies incoming damage after resistance mitigation.
        /// Damage is reduced by the character's resistance to the given type;
        /// negative resistance (vulnerability) increases damage. Minimum 1 damage always applies.
        /// Fires <see cref="OnHealthChanged"/> and triggers <see cref="Die"/> if health reaches zero.
        /// </summary>
        /// <param name="amount">Raw damage amount before mitigation.</param>
        /// <param name="damageType">Elemental or physical category of the damage.</param>
        public void TakeDamage(float amount, DamageType damageType)
        {
            float resistance    = GetResistance(damageType);
            float mitigated     = amount * (1f - resistance);
            float finalDamage   = Mathf.Max(1f, mitigated);

            _currentHealth = Mathf.Max(0f, _currentHealth - finalDamage);
            OnHealthChanged?.Invoke(_currentHealth, _stats.MaxHealth);

            if (_currentHealth <= 0f)
                Die();
        }

        /// <summary>
        /// Restores health up to the current maximum. Fires <see cref="OnHealthChanged"/>.
        /// </summary>
        /// <param name="amount">Amount of health to restore.</param>
        public void Heal(float amount)
        {
            _currentHealth = Mathf.Min(_stats.MaxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, _stats.MaxHealth);
        }

        // ── Resource ──────────────────────────────────────────────────────────

        /// <summary>
        /// Spends resource, clamping to zero. Fires <see cref="OnResourceChanged"/>.
        /// </summary>
        /// <param name="amount">Amount of resource to spend.</param>
        public void UseResource(float amount)
        {
            _currentResource = Mathf.Max(0f, _currentResource - amount);
            OnResourceChanged?.Invoke(_currentResource, _characterData.MaxResource);
        }

        /// <summary>
        /// Restores resource up to the current maximum. Fires <see cref="OnResourceChanged"/>.
        /// </summary>
        /// <param name="amount">Amount of resource to restore.</param>
        public void GainResource(float amount)
        {
            _currentResource = Mathf.Min(_characterData.MaxResource, _currentResource + amount);
            OnResourceChanged?.Invoke(_currentResource, _characterData.MaxResource);
        }

        // ── XP and Levelling ──────────────────────────────────────────────────

        /// <summary>
        /// Adds XP and triggers <see cref="LevelUp"/> as many times as the accumulated
        /// total crosses the threshold.
        /// </summary>
        /// <param name="amount">XP amount to award.</param>
        public void GainXP(float amount)
        {
            _currentXP += amount;
            while (_currentXP >= _xpToNextLevel)
            {
                _currentXP -= _xpToNextLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            _currentLevel++;
            _statPoints    += 5;
            _xpToNextLevel *= 1.5f;

            _currentHealth   = _stats.MaxHealth;
            _currentResource = _characterData.MaxResource;

            OnLevelUp?.Invoke(_currentLevel);
            OnHealthChanged?.Invoke(_currentHealth, _stats.MaxHealth);
            OnResourceChanged?.Invoke(_currentResource, _characterData.MaxResource);
        }

        // ── Stat Allocation ───────────────────────────────────────────────────

        /// <summary>
        /// Spends one stat point to add a +1 modifier to the given stat.
        /// If the stat is <see cref="StatType.Velocity"/>, the NavMeshAgent speed is updated immediately.
        /// </summary>
        /// <param name="type">The stat to increase.</param>
        /// <returns>True if the point was spent; false if no points remain.</returns>
        public bool AllocateStat(StatType type)
        {
            if (_statPoints <= 0)
                return false;

            _stats.GetStat(type).AddModifier(1f);
            _statPoints--;

            if (type == StatType.Velocity)
                _agent.speed = _characterData.BaseMoveSpeed * _stats.MoveSpeedMultiplier;

            return true;
        }

        // ── Resistance ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the character's resistance fraction for the given damage type.
        /// Sourced directly from the CharacterData asset.
        /// </summary>
        /// <param name="type">Damage type to query.</param>
        public float GetResistance(DamageType type) => type switch
        {
            DamageType.Solid   => _characterData.SolidResistance,
            DamageType.Liquid  => _characterData.LiquidResistance,
            DamageType.Gas     => _characterData.GasResistance,
            DamageType.Plasma  => _characterData.PlasmaResistance,
            DamageType.Psychic => _characterData.PsychicResistance,
            _                  => 0f
        };

        // ── Death ─────────────────────────────────────────────────────────────

        private void Die()
        {
            Debug.Log($"[PlayerCharacter] {_characterData.ClassName} has died.");
            OnDeath?.Invoke();
        }
    }
}
