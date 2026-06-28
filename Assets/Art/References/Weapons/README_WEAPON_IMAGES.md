# Weapon Asset Drop Zone

Use these folders by asset type.

Reference images:
- Assets/Art/References/Weapons

Imported weapon models:
- Assets/Models/Weapons

Weapon textures/material references:
- Assets/Textures/Weapons

Weapon prefabs:
- Assets/Prefabs/Weapons

Current basic weapon model names:
- warrior_sword_basic_01.fbx
- rogue_shuriken_basic_01.fbx
- archer_cannon_basic_01.obj
- mage_staff_basic_01.obj

Naming rules:
- Use lowercase English names.
- Use underscores instead of spaces.
- Use `_basic_01` for the first gameplay test model of each weapon type.
- Use `_ref_01` only for image/reference files, not real imported models.

Current plan:
- Keep gameplay code independent from art assets.
- First import models, then connect them through a small visual/prefab layer.
- Do not hard-wire art assets directly into combat logic.
## Runtime weapon view

The imported source models are kept in `Assets/Models/Weapons`.
For the current prototype, runtime-loadable copies are also placed in `Assets/Resources/Weapons` so `FirstPersonWeaponView` can show a local first-person weapon immediately without scene/prefab wiring.

Current runtime names:
- `warrior_sword_basic_01`
- `rogue_shuriken_basic_01`
- `archer_cannon_basic_01`
- `mage_staff_basic_01`
