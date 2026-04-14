using UnityEngine;
using BloodDirective.Stats;

namespace BloodDirective.Enemies
{
    /// <summary>
    /// ScriptableObject data asset defining an enemy type's combat stats,
    /// resistances, movement, and reward values.
    /// One asset per enemy type — create via BloodDirective/Enemy Data in the asset menu.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "BloodDirective/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")]
        [SerializeField] private string _enemyName;
        [SerializeField] private Sprite _enemyIcon;

        // ── Combat ────────────────────────────────────────────────────────────

        [Header("Combat")]
        [SerializeField] private float      _maxHealth        = 50f;
        [SerializeField] private float      _baseAttackDamage = 8f;
        [SerializeField] private float      _baseAttackSpeed  = 1f;
        [SerializeField] private float      _baseAttackRange  = 1.5f;
        [SerializeField] private float      _baseWeaponDamage = 10f;
        [SerializeField] private DamageType _damageType       = DamageType.Solid;

        // ── Resistances ───────────────────────────────────────────────────────

        [Header("Resistances")]
        [Range(-0.5f, 0.75f)]
        [SerializeField] private float _solidResistance   = 0f;
        [Range(-0.5f, 0.75f)]
        [SerializeField] private float _liquidResistance  = 0f;
        [Range(-0.5f, 0.75f)]
        [SerializeField] private float _gasResistance     = 0f;
        [Range(-0.5f, 0.75f)]
        [SerializeField] private float _plasmaResistance  = 0f;
        [Range(-0.5f, 0.75f)]
        [SerializeField] private float _psychicResistance = 0f;

        // ── Rewards ───────────────────────────────────────────────────────────

        [Header("Rewards")]
        [SerializeField] private float _xpReward = 25f;

        // ── Movement ──────────────────────────────────────────────────────────

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3f;

        // ── Identity Getters ──────────────────────────────────────────────────

        /// <summary>Display name shown in UI and debug logs.</summary>
        public string EnemyName => _enemyName;

        /// <summary>Icon sprite used in UI.</summary>
        public Sprite EnemyIcon => _enemyIcon;

        // ── Combat Getters ────────────────────────────────────────────────────

        /// <summary>Maximum health pool for this enemy type.</summary>
        public float MaxHealth => _maxHealth;

        /// <summary>Flat attack damage this enemy deals before its own modifiers.</summary>
        public float BaseAttackDamage => _baseAttackDamage;

        /// <summary>Attacks per second for this enemy type.</summary>
        public float BaseAttackSpeed => _baseAttackSpeed;

        /// <summary>Distance in world units at which this enemy can strike.</summary>
        public float BaseAttackRange => _baseAttackRange;

        /// <summary>Base weapon damage scalar used in damage calculations.</summary>
        public float BaseWeaponDamage => _baseWeaponDamage;

        /// <summary>Damage type dealt by this enemy's basic attacks.</summary>
        public DamageType DamageType => _damageType;

        // ── Resistance Getters ────────────────────────────────────────────────

        /// <summary>Solid damage resistance fraction. Negative values indicate vulnerability.</summary>
        public float SolidResistance => _solidResistance;

        /// <summary>Liquid damage resistance fraction. Negative values indicate vulnerability.</summary>
        public float LiquidResistance => _liquidResistance;

        /// <summary>Gas damage resistance fraction. Negative values indicate vulnerability.</summary>
        public float GasResistance => _gasResistance;

        /// <summary>Plasma damage resistance fraction. Negative values indicate vulnerability.</summary>
        public float PlasmaResistance => _plasmaResistance;

        /// <summary>Psychic damage resistance fraction. Negative values indicate vulnerability.</summary>
        public float PsychicResistance => _psychicResistance;

        // ── Reward Getters ────────────────────────────────────────────────────

        /// <summary>Experience points awarded to the player when this enemy is killed.</summary>
        public float XpReward => _xpReward;

        // ── Movement Getters ──────────────────────────────────────────────────

        /// <summary>Base movement speed in world units per second.</summary>
        public float MoveSpeed => _moveSpeed;
    }
}
