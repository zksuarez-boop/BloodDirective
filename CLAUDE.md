# Blood Directive — Project Context

## Game Overview
Isometric action RPG inspired by Diablo 2 and Path of Exile 2.
Solo developer. Unity 6 URP. Steam PC first.
Visual style: Dark desaturated, single-source lighting, heavy post-processing.

## Architecture Rules
- Data-driven design — all content lives in ScriptableObjects, not hardcoded scripts
- Singletons for managers (GameManager, AudioManager, etc.)
- Private fields with [SerializeField] — never public fields
- async/await over coroutines where possible
- Event-driven communication between systems

## Class Codenames (enum → display)
- PhantomArmy → "Phantom Army" (Green Beret)
- WhoDaresWins → "Who Dares Wins" (Knight/SAS)
- Frogman → "Frogman" (SEAL/Diver)
- Kage → "Kage" (Ninja/Spy)
- Bushido → "Bushido" (Samurai)
- Phalanx → "Phalanx" (Spartan)
- Berserkr → "Berserkr" (Viking)
- Ibutho → "Ibutho" (Zulu Impi)
- Tengri → "Tengri" (Mongol)
- NullPoint → "Null Point" (Psion)

## Stats (StatType enum)
Fortitude, Precision, Velocity, Cognition, Endurance, Instinct

## Damage Types (DamageType enum)
Solid, Liquid, Gas, Plasma, Psychic

## Passive Tree
PoE-style open web

## Pricing
- Base game: $29.99
- Expansions: $19.99 each
- Complete Edition: $74.99

## Hard Constraints
- No real military unit names in code or assets
- No ECS/DOTS
- No public fields — use [SerializeField] private
- No coroutines where async/await works better
- Dead Zero = Sniper codename (not White Feather)
- Null Point = Psion codename (not Stargate)
- Abzu = Annunaki homeworld (not Nibiru)

## Folder Structure
Assets/_BloodDirective/
├── Scripts/ (Player, Enemies, Combat, Inventory, Stats, Skills, UI, Systems, Data)
├── Prefabs/ (Player, Enemies, Skills, UI)
├── ScriptableObjects/ (Classes, Skills, Items, Enemies)
├── Scenes/ (MainMenu, Sanctum, Act1)
├── Art/ (Characters, Environment, UI)
└── Audio/ (Music, SFX)

## Lore Summary
- Secret society spanning all human history
- Watchers: galactic council, two factions (Conservators vs Interventionists)
- Annunaki: exiled creators, homeworld Abzu, want Earth's genetic archive back
- Enemies: Greys (drones), Reptilians (elite soldiers), Hybrids (experiments)
- Hub: The Sanctum (Cold War bunker, 7 levels deep)
- 5 acts at launch, expanding to 8 with paid expansions
