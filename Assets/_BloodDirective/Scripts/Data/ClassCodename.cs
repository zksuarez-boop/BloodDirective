namespace BloodDirective.Data
{
    /// <summary>
    /// Internal codenames for each of the ten player classes.
    /// Maps to a display name and class archetype via <see cref="CharacterData"/>.
    /// Hard constraint: these values must never use real military unit names.
    /// </summary>
    public enum ClassCodename
    {
        /// <summary>Green Beret — tactical operator. Resource: Op Points.</summary>
        PhantomArmy,

        /// <summary>Knight/SAS — armoured vanguard. Resource: Valour.</summary>
        WhoDaresWins,

        /// <summary>SEAL/Diver — aquatic infiltrator. Resource: Pressure.</summary>
        Frogman,

        /// <summary>Ninja/Spy — shadow assassin. Resource: Shadow.</summary>
        Kage,

        /// <summary>Samurai — disciplined duelist. Resource: Ki.</summary>
        Bushido,

        /// <summary>Spartan — shield-wall defender. Resource: Resolve.</summary>
        Phalanx,

        /// <summary>Viking — berserker frontliner. Resource: Rage.</summary>
        Berserkr,

        /// <summary>Zulu Impi — relentless skirmisher. Resource: Fury.</summary>
        Ibutho,

        /// <summary>Mongol — hit-and-run raider. Resource: Spirit.</summary>
        Tengri,

        /// <summary>Psion — psionic manipulator. Resource: Psi.</summary>
        NullPoint
    }
}
