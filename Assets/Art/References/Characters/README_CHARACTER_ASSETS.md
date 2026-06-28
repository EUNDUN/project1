# Character Asset Registry

Purpose: keep imported character art discoverable without moving Asset Store package internals.

## Rule
- Do not move files inside imported package folders unless Unity references have been checked.
- Keep original package folders intact. Create prefab variants or lightweight runtime visual prefabs elsewhere later.
- Combat, Health, Team, Ability, hit detection, and networking data must stay separate from visual character assets.
- Imported character models are presentation only. Do not let their colliders drive gameplay collision or damage.

## Imported Top-Level Folders

| Folder | Approx Size | Notes |
| --- | ---: | --- |
| `Assets/o3n` | 399.61 MB | Large UMA-style modular character package. Powerful but heavy/complex; test carefully before runtime hookup. |
| `Assets/Sky_Protective_suit` | 209.78 MB | Simple protective suit candidate. Good first test for a class visual. |
| `Assets/UnityTechnologies` | 15.33 MB | Includes Space Robot Kyle and Starter Assets content. RobotKyle is a simple candidate. |

## Candidate Prefabs / Models

### Protective Suit
- Prefab: `Assets/Sky_Protective_suit/Prefab/sky_protectiv_suit_rig_UNITY.prefab`
- FBX: `Assets/Sky_Protective_suit/Base_Mesh/sky_protectiv_suit.fbx`
- Suggested temporary use: Warrior or heavy class visual.

### Space Robot Kyle
- Prefab: `Assets/UnityTechnologies/SpaceRobotKyle/Prefabs/RobotKyle.prefab`
- FBX: `Assets/UnityTechnologies/SpaceRobotKyle/Models/KyleRobot.fbx`
- Suggested temporary use: Archer/ranged class visual, because it is compact and easy to test.

### o3n Male
- Prefab: `Assets/o3n/o3nBaseUMARaces/Races/o3nMaleRace/Prefabs/o3nMaleAvatar_afro.prefab`
- Prefab: `Assets/o3n/o3nBaseUMARaces/Races/o3nMaleRace/Prefabs/o3nMaleDynamicCharacterAvatar.prefab`
- Prefab: `Assets/o3n/o3nBaseUMARaces/Races/o3nMaleRace/Prefabs/o3nMaleDynamicCharacterAvatarPhysics.prefab`
- FBX: `Assets/o3n/o3nBaseUMARaces/Races/o3nMaleRace/FBX/o3nMale.fbx`
- FBX: `Assets/o3n/o3nBaseUMARaces/Races/o3nMaleRace/FBX/o3nMale_unified.fbx`
- Suggested temporary use: Rogue, only after checking runtime cost and dependencies.

### o3n Female
- Prefab: `Assets/o3n/o3nBaseUMARaces/Races/o3nFemaleRace/Prefabs/o3nFemaleAvatar_afro.prefab`
- Prefab: `Assets/o3n/o3nBaseUMARaces/Races/o3nFemaleRace/Prefabs/o3nFemaleDynamicCharacterAvatar.prefab`
- Prefab: `Assets/o3n/o3nBaseUMARaces/Races/o3nFemaleRace/Prefabs/o3nFemaleDynamicCharacterAvatarPhysics.prefab`
- FBX: `Assets/o3n/o3nBaseUMARaces/Races/o3nFemaleRace/FBX/o3nFemale.fbx`
- FBX: `Assets/o3n/o3nBaseUMARaces/Races/o3nFemaleRace/FBX/o3nFemale_unified.fbx`
- Suggested temporary use: Mage, only after checking runtime cost and dependencies.

## Suggested First Mapping

| CombatClass | Visual Candidate | Reason |
| --- | --- | --- |
| Warrior | Protective Suit prefab | bulky/heavy silhouette |
| Archer | RobotKyle prefab | simple compact ranged-looking model |
| Rogue | o3n Male prefab or temporary capsule | human silhouette, but package is heavy |
| Mage | o3n Female prefab or temporary capsule | human silhouette, but package is heavy |

## Next Safe Step

Create a `CharacterVisualView` layer similar to `FirstPersonWeaponView`:
- purely visual child under each character;
- disable imported gameplay colliders;
- keep CharacterController as the only movement collision source;
- expose per-class visual prefab references or resource paths in `GameConfig`;
- do not change Health, Damage, Ability, targeting, or server-authoritative assumptions.

## Deletion Notes

If an imported pack is not used later, delete its top-level folder from Unity Project view:
- `Assets/o3n`
- `Assets/Sky_Protective_suit`
- `Assets/UnityTechnologies/SpaceRobotKyle` if only RobotKyle should be removed

Avoid deleting random subfolders from Finder/Explorer while Unity is open.