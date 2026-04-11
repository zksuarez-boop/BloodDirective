namespace BloodDirective.Stats
{
    /// <summary>
    /// Defines the elemental or physical category of damage dealt.
    /// Used to drive resistances, vulnerabilities, and damage calculations.
    /// </summary>
    public enum DamageType
    {
        /// <summary>Blunt, pierce, or slash physical damage.</summary>
        Solid,

        /// <summary>Corrosive, poison, or fluid-based damage.</summary>
        Liquid,

        /// <summary>Toxic cloud, vapour, or airborne damage.</summary>
        Gas,

        /// <summary>Energy, fire, lightning, or high-heat damage.</summary>
        Plasma,

        /// <summary>Mind-based, psionic, or reality-distortion damage.</summary>
        Psychic
    }
}
