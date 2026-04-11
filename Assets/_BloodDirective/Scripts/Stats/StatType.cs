namespace BloodDirective.Stats
{
    /// <summary>
    /// Enumerates every primary stat in the Blood Directive stat system.
    /// Each value maps to one <see cref="Stat"/> entry inside a <see cref="StatSheet"/>.
    /// </summary>
    public enum StatType
    {
        /// <summary>Raw physical power — drives melee damage and carry capacity.</summary>
        Fortitude,

        /// <summary>Accuracy and fine motor control — drives crit damage and ranged accuracy.</summary>
        Precision,

        /// <summary>Agility and reaction speed — drives movement speed and dodge.</summary>
        Velocity,

        /// <summary>Mental acuity — drives cooldown reduction and skill potency.</summary>
        Cognition,

        /// <summary>Stamina and resilience — drives maximum health and status resistance.</summary>
        Endurance,

        /// <summary>Intuition and awareness — drives crit chance and threat detection.</summary>
        Instinct
    }
}
