# Human hero meshes

`Models/Human_Man.fbx` and `Models/Human_Woman.fbx` are clothed adult human models generated for Ashlight using the MakeHuman/MPFB base mesh, morph targets and game-engine rig. The fitted bodices, legwear, hair ribbons, eyes, materials and seven simple animation actions were prepared for this project in Blender. They are not unmodified commercial character assets or finished photorealistic models.

## Upstream source and license

- Project: [MakeHuman / MPFB](https://github.com/makehumancommunity/mpfb2)
- Source revision: [`d0a32e57a7f915cb2f2b95410e2117648c7bbb7e`](https://github.com/makehumancommunity/mpfb2/tree/d0a32e57a7f915cb2f2b95410e2117648c7bbb7e)
- Used data: `src/mpfb/data/3dobjs/base.obj`, bundled morph targets, and `data/rigs/standard` game-engine rig/weights.
- These data assets are **CC0 1.0 Universal**. Commercial use and modification are permitted. Full text: [LICENSE-CC0.md](LICENSE-CC0.md).
- [Upstream license explanation](https://github.com/makehumancommunity/mpfb2/blob/d0a32e57a7f915cb2f2b95410e2117648c7bbb7e/LICENSE.md) explicitly distinguishes GPL program code from CC0 assets and exported output. No MPFB program code is vendored into the game or FBX files.
- Base-mesh credits include MakeHuman Team, Data Collection AB, Joel Palmius, and Jonas Hauquier. Attribution is retained voluntarily.

The original additions authored for these two exported models are also made available under CC0 1.0 so the assembled model assets can be reused commercially. This applies to the model data, not unrelated application code.

The male variant uses stronger jaw/chin/brow and neck morphs, broader shoulders, cropped scalp hair, a short beard/moustache mesh, muted lip color, and a higher-neck sleeveless garment. The female model retains its previous silhouette and wardrobe. Regenerate class/gender prefabs with **Ashlight > Characters > Build Realistic Humans** when updating existing Unity projects so the additional facial-hair material slot is assigned correctly.

## Preparation and verification

Blender **4.3.2** was used. `MobileGame/Tools/build_human_models.py` regenerates the models from the pinned MPFB source checkout; pass its `src` directory and an output directory after `--`. Set Blender's user config to a writable directory when needed. It exports FBX plus editable Blender working files; only the FBX exports are bundled here.

`MobileGame/Tools/validate_human_models.py` independently imports the FBX files and checks mesh budget, one skeleton, hand/head/pelvis bones, weighted meshes, expected material roles, optional robe panels, and seven nonempty animation actions. `MODEL_REPORT.json` records export counts; `SHA256SUMS.txt` records the bundled FBX checksums. A Blender wardrobe render was inspected separately.

Unity's editor setup creates 12 class/gender prefab variants with colors, materials, basic props and animation controllers. Unity import, compilation, Play mode, clipping during animation and mobile performance need local testing. The models use shared base faces and simple materials; unique hero likenesses and detailed skin/hair textures are not included.
