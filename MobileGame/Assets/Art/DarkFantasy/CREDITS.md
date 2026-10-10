# Dark fantasy reference assets

Rowan, Lantern Warden and the twelve class/gender models use the adult male and female anatomical meshes and 53-bone skeleton derived from MakeHuman/MPFB CC0 data at source revision `d0a32e57a7f915cb2f2b95410e2117648c7bbb7e`. See [MakeHuman credits](../../ThirdParty/MakeHuman/CREDITS.md) and [CC0 license](../../ThirdParty/MakeHuman/LICENSE-CC0.md). No MPFB GPL program code is bundled with these art assets.

The armor, helmets, cloaks, weapons, lantern, UV layout, procedural textures and animation authored for this reference are dedicated to the public domain under CC0 1.0. No paid assets are required. ForgedMetal, ForgedMetalNormal and CourtyardStone are original 512×512 procedural bakes.

## Reproduction

Generate the male source blend using `Tools/build_human_models.py` as described in the MakeHuman credits. Then run Blender 4.3.2 headlessly:

```sh
blender -b --factory-startup --python-exit-code 1 --python Tools/build_dark_fantasy.py -- /tmp/ashlight-human-output/Human_Man.blend /tmp/ashlight-dark-fantasy
blender -b --factory-startup --python-exit-code 1 --python Tools/validate_dark_fantasy.py -- /tmp/ashlight-dark-fantasy
blender -b --factory-startup --python-exit-code 1 --python Tools/render_dark_fantasy.py -- /tmp/ashlight-dark-fantasy /tmp/ashlight-dark-preview.png
```

For the class roster, supply the directory containing both `Human_Man.blend` and `Human_Woman.blend` and add `--roster`:

```sh
blender -b --factory-startup --python-exit-code 1 --python Tools/build_dark_fantasy.py -- /tmp/ashlight-human-output /tmp/ashlight-dark-roster --roster
blender -b --factory-startup --python-exit-code 1 --python Tools/validate_dark_fantasy.py -- /tmp/ashlight-dark-roster --roster
```

The roster reuses the reference textures. Copy them into the roster output directory before rendering. Run `Tools/render_dark_fantasy.py` with the output directory, preview PNG path and `Man` or `Woman` as the final argument for a six-class lineup.

Copy the FBX and texture outputs to Models and Textures respectively. MODEL_REPORT.json and ROSTER_REPORT.json record generated geometry and SHA256SUMS.txt records the shipped asset hashes.

## Validation limits

Independent Blender FBX reimport passed: Rowan 17,558 triangles and 8,936 weighted vertices; Warden 17,750 triangles and 9,037 weighted vertices. Each contains one rig, one mesh, UVs and Idle, Attack, Cast, Dodge, Parry, Hit and Death states. Closed helmets replace visible realistic faces. The Blender art preview was inspected. Unity compilation/import, animation playback, courtyard framing, action clipping and device performance are not verified here.

The twelve class/gender FBX files also independently reimported successfully: 16,670–17,778 triangles each, one weighted mesh and one 53-bone rig, nonempty UVs, class palette roles, at most seven material slots and seven animation states. Both male and female Blender lineup previews were inspected. These checks do not verify Unity or device playback.

## Enemy additions

The Ash Hound mesh and its 14-bone skeleton, all enemy armor additions, talismans, crown, new attack and Windup poses are original CC0 1.0 work. Humanoid enemies and the female HollowMatron caster base derive from the same licensed adult MakeHuman sources above. No new paid or downloaded assets are required. ENEMY_REPORT.json records six source models; the eight paired-boss prefabs reuse these source meshes with distinct palette/effect settings in Unity.

After generating both reference and roster outputs, run:

```sh
blender -b --factory-startup --python-exit-code 1 --python Tools/build_chapter_one_enemies.py -- /tmp/ashlight-dark-fantasy /tmp/ashlight-dark-roster /tmp/ashlight-enemies
blender -b --factory-startup --python-exit-code 1 --python Tools/validate_chapter_one_enemies.py -- /tmp/ashlight-enemies
```

Copy the six FBX outputs to Models (the updated LanternWarden replaces the earlier reference export). Copy the original reference textures into the temporary enemy output directory, then render the lineup with `Tools/render_dark_fantasy.py`, passing that directory, a PNG path and `Enemies` as the final argument. The newer ENEMY_REPORT.json supersedes the earlier Warden animation report; the shipped MODEL_REPORT.json includes its new Windup state.

Independent import/evaluated-pose checks passed for all six models: 3,362–17,992 triangles, at most seven material slots, weighted skeleton, UVs, Idle/Attack/Cast/Dodge/Parry/Hit/Death/Windup clips. The original Hound has 14 bones; humanoids retain 53. The five-enemy Blender lineup was visually inspected. These checks do not verify Unity import or actual battle playback.
