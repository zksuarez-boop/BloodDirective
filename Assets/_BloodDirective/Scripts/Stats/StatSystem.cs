using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BloodDirective.Stats
{
    /// <summary>
    /// A single primary stat with a serialized base value and runtime modifiers.
    /// Effective value is soft-capped at 300: above the cap each additional raw
    /// point yields 50% of its face value.
    /// </summary>
    [Serializable]
    public class Stat
    {
        private const float SoftCap = 300f;

        [SerializeField] private float _baseValue;

        private readonly List<float> _modifiers = new();

        /// <summary>The base value set at character creation or item equip time.</summary>
        public float BaseValue => _baseValue;

        /// <summary>
        /// Effective value after all modifiers, with soft cap applied.
        /// Values at or below 300 are returned as-is.
        /// Values above 300 follow: 300 + (excess × 0.5).
        /// </summary>
        public float Value
        {
            get
            {
                float raw = _baseValue + _modifiers.Sum();
                if (raw <= SoftCap)
                    return raw;
                return SoftCap + (raw - SoftCap) * 0.5f;
            }
        }

        /// <summary>Creates a new Stat with the specified base value.</summary>
        /// <param name="baseValue">Starting value before any modifiers.</param>
        public Stat(float baseValue = 0f) => _baseValue = baseValue;

        /// <summary>Adds a flat modifier to this stat (positive or negative).</summary>
        /// <param name="modifier">Amount to add.</param>
        public void AddModifier(float modifier) => _modifiers.Add(modifier);

        /// <summary>
        /// Removes the first occurrence of the given modifier value.
        /// Silently does nothing if the modifier is not present.
        /// </summary>
        /// <param name="modifier">Exact modifier value to remove.</param>
        public void RemoveModifier(float modifier) => _modifiers.Remove(modifier);
    }

    /// <summary>
    /// Holds one <see cref="Stat"/> per <see cref="StatType"/> and exposes
    /// all derived gameplay values calculated from those stats.
    /// Attach to any data class that needs a full stat profile (player, enemy, etc.).
    /// </summary>
    [Serializable]
    public class StatSheet
    {
        [SerializeField] private Stat _fortitude = new();
        [SerializeField] private Stat _precision  = new();
        [SerializeField] private Stat _velocity   = new();
        [SerializeField] private Stat _cognition  = new();
        [SerializeField] private Stat _endurance  = new();
        [SerializeField] private Stat _instinct   = new();

        // ── Stat Accessor ────────────────────────────────────────────────────

        /// <summary>Returns the <see cref="Stat"/> associated with the given <see cref="StatType"/>.</summary>
        /// <param name="type">The stat to retrieve.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an unhandled StatType is passed.</exception>
        public Stat GetStat(StatType type) => type switch
        {
            StatType.Fortitude => _fortitude,
            StatType.Precision => _precision,
            StatType.Velocity  => _velocity,
            StatType.Cognition => _cognition,
            StatType.Endurance => _endurance,
            StatType.Instinct  => _instinct,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        // ── Derived Properties ────────────────────────────────────────────────

        /// <summary>Maximum health pool. Formula: 50 + (Endurance × 8).</summary>
        public float MaxHealth => 50f + (_endurance.Value * 8f);

        /// <summary>Melee damage multiplier. Formula: 1 + (Fortitude × 0.02).</summary>
        public float MeleeDamage => 1f + (_fortitude.Value * 0.02f);

        /// <summary>
        /// Critical strike chance as a 0–1 fraction, clamped to [0, 0.60].
        /// Formula: 5% base + (Instinct × 0.5%).
        /// </summary>
        public float CritChance => Mathf.Clamp(0.05f + (_instinct.Value * 0.005f), 0f, 0.60f);

        /// <summary>Critical strike damage multiplier. Formula: 1.5 + (Precision × 0.015).</summary>
        public float CritDamage => 1.5f + (_precision.Value * 0.015f);

        /// <summary>
        /// Cooldown reduction as a 0–1 fraction, clamped to [0, 0.50].
        /// Formula: Cognition × 0.5%.
        /// </summary>
        public float CooldownReduction => Mathf.Clamp(_cognition.Value * 0.005f, 0f, 0.50f);

        /// <summary>Movement speed multiplier. Formula: 1 + (Velocity × 0.5%).</summary>
        public float MoveSpeedMultiplier => 1f + (_velocity.Value * 0.005f);
    }
}
