# KayKit Adventurers: free Knight source

Creator: **Kay Lousberg / KayKit**.

- Official source: https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0
- Author's asset page: https://kaylousberg.itch.io/kaykit-adventurers
- Imported commit: `672074b73ba276876a19e8816ecdc5241817ab47`
- License: **CC0 1.0**, included verbatim in `LICENSE.txt`. The author permits personal/commercial use; attribution is optional and retained here as credit.
- Included upstream files: `Characters/fbx/Knight.fbx`, `Characters/fbx/knight_texture.png`, and the pack's license.
- `SHA256SUMS.txt` records byte checksums. The included source files are unchanged from the official repository.

Unity generates a prefab, animation controller, and a dark-tinted material locally under `Assets/Resources/Heroes`. The atlas is imported at a maximum resolution of 512. The source contains the character rig and combat animations; the setup selects seven clips when Unity exposes the expected clip names. Only Knight-class heroes use this asset in the current integration. No paid/EXTRA-tier content was added.

The mesh and atlas are stylized low-poly, not realistic scanned artwork. Dark fantasy direction is applied through subdued materials and cooler lighting. Mesh/cape recoloring, further character variants, and bespoke cinematic animations remain future work. The source FBX is about 20 MB; import/build time and actual runtime size must be measured in Unity.

The corresponding upstream GLB reports 6,952 triangles and 75 animations. This was inspected to confirm asset structure; Unity's imported FBX mesh/animation counts must be checked locally. Unity import, rig binding, animation playback, weapon visibility/orientation, and Web/Android rendering have not been executed in this cloud workspace.
