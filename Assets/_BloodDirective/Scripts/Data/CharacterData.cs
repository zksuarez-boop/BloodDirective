using UnityEngine;
using BloodDirective.Stats;

namespace BloodDirective.Data
{
    /// <summary>
    /// ScriptableObject data asset defining a player class's base stats,
    /// resistances, resource, and movement values.
    /// One asset per class — create via BloodDirective/Character Data in the asset menu.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "BloodDirective/Character Data")]
    public class CharacterData : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")]
        [SerializeField] private string _className;
        [SerializeField] private ClassCodename _codename;
        [SerializeField] private string _codenameDisplay;
        [SerializeField] private Sprite _classIcon;

        // ── Base Stats ────────────────────────────────────────────────────────

        [Header("Base Stats")]
        [SerializeField] private float _baseFortitude = 5f;
        [SerializeField] private float _basePrecision  = 5f;
        [SerializeField] private float _baseVelocity   = 5f;
        [SerializeField] private float _baseCognition  = 5f;
        [SerializeField] private float _baseEndurance  = 5f;
        [SerializeField] private float _baseInstinct   = 5f;

        // ── Scaling ───────────────────────────────────────────────────────────

        [Header("Scaling")]
        [SerializeField] private StatType _primaryStat;
        [SerializeField] private StatType _secondaryStat;

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

        // ── Resource ──────────────────────────────────────────────────────────

        [Header("Resource")]
        [SerializeField] private string _resourceName      = "Resource";
        [SerializeField] private float  _maxResource       = 100f;
        [SerializeField] private float  _resourceRegenRate = 5f;

        // ── Movement ──────────────────────────────────────────────────────────

        [Header("Movement")]
        [SerializeField] private float      _baseMoveSpeed       = 5f;
        [SerializeField] private float      _baseAttackRange     = 2f;
        [SerializeField] private float      _baseAttackSpeed     = 1f;
        [SerializeField] private float      _baseWeaponDamage    = 10f;
        [SerializeField] private DamageType _defaultDamageType   = DamageType.Solid;

        // ── Identity Getters ──────────────────────────────────────────────────

        /// <summary>Display name shown in UI, e.g. "Green Beret".</summary>
        public string ClassName => _className;

        /// <summary>Internal enum codename, e.g. PhantomArmy.</summary>
        public ClassCodename Codename => _codename;

        /// <summary>Human-readable codename string, e.g. "Phantom Army".</summary>
        public string CodenameDisplay => _codenameDisplay;

        /// <summary>Icon sprite used in class selection and HUD.</summary>
        public Sprite ClassIcon => _classIcon;

        // ── Base Stat Getters ─────────────────────────────────────────────────

        /// <summary>Starting Fortitude before any modifiers are applied.</summary>
        public float BaseFortitude => _baseFortitude;

        /// <summary>Starting Precision before any modifiers are applied.</summary>
        public float BasePrecision => _basePrecision;

        /// <summary>Starting Velocity before any modifiers are applied.</summary>
        public float BaseVelocity => _baseVelocity;

        /// <summary>Starting Cognition before any modifiers are applied.</summary>
        public float BaseCognition => _baseCognition;

        /// <summary>Starting Endurance before any modifiers are applied.</summary>
        public float BaseEndurance => _baseEndurance;

        /// <summary>Starting Instinct before any modifiers are applied.</summary>
        public float BaseInstinct => _baseInstinct;

        // ── Scaling Getters ───────────────────────────────────────────────────

        /// <summary>The primary stat this class scales with at level-up.</summary>
        public StatType PrimaryStat => _primaryStat;

        /// <summary>The secondary stat this class scales with at level-up.</summary>
        public StatType SecondaryStat => _secondaryStat;

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

        // ── Resource Getters ──────────────────────────────────────────────────

        /// <summary>Class-specific resource label, e.g. "Op Points", "Rage", "Pressure".</summary>
        public string ResourceName => _resourceName;

        /// <summary>Maximum resource pool capacity.</summary>
        public float MaxResource => _maxResource;

        /// <summary>Resource regeneration rate per second at rest.</summary>
        public float ResourceRegenRate => _resourceRegenRate;

        // ── Movement Getters ──────────────────────────────────────────────────

        /// <summary>Base movement speed in world units per second.</summary>
        public float BaseMoveSpeed => _baseMoveSpeed;

        /// <summary>Base attack range in world units.</summary>
        public float BaseAttackRange => _baseAttackRange;

        /// <summary>Base number of attacks per second.</summary>
        public float BaseAttackSpeed => _baseAttackSpeed;

        /// <summary>Flat damage value before stat multipliers are applied.</summary>
        public float BaseWeaponDamage => _baseWeaponDamage;

        /// <summary>Damage type used by this class's basic attacks.</summary>
        public DamageType DefaultDamageType => _defaultDamageType;

        // ── Methods ───────────────────────────────────────────────────────────

        /// <summary>
        /// Creates and returns a new <see cref="StatSheet"/> pre-populated with
        /// this class's six base stat values applied as the first modifier on each stat.
        /// </summary>
        public StatSheet CreateStatSheet()
        {
            var sheet = new StatSheet();
            sheet.GetStat(StatType.Fortitude).AddModifier(_baseFortitude);
            sheet.GetStat(StatType.Precision).AddModifier(_basePrecision);
            sheet.GetStat(StatType.Velocity).AddModifier(_baseVelocity);
            sheet.GetStat(StatType.Cognition).AddModifier(_baseCognition);
            sheet.GetStat(StatType.Endurance).AddModifier(_baseEndurance);
            sheet.GetStat(StatType.Instinct).AddModifier(_baseInstinct);
            return sheet;
        }
    }
}
