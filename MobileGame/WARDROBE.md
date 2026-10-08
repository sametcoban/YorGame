# Free dark fantasy wardrobe shortlist

These are candidate outfits for the game's adult, human-proportioned heroes. They have been found and their published license labels checked in the official MakeHuman community website source. **They are not yet imported into Unity or fitted to a character.**

| Candidate | Suggested use | Published license | Official catalog / source |
| --- | --- | --- | --- |
| Margaret Toigo's Bodice-style Top (`toigo_bodice-style_top`) | Rogue or Ranger: exposed shoulders, fitted bodice, add leather belts and boots | CC0 | [Tops pack](https://static.makehumancommunity.org/assets/assetpacks/shirts01.html), [individual asset](http://www.makehumancommunity.org/node/1667) |
| Margaret Toigo's Turtleneck Halter Top (`toigo_turtleneck_halter_top`) | Rogue: exposed shoulders and back, recolor the bright fabric charcoal or burgundy | CC0 | [Tops pack](https://static.makehumancommunity.org/assets/assetpacks/shirts01.html), [individual asset](http://www.makehumancommunity.org/node/1642) |
| Elvaerwyn's Goddess Dress 1 (`elvs_goddess_dress1`) | Sorceress or Cleric: sleeveless, deep V neckline and long draped fabric; recolor dark violet | CC-BY; attribution required | [Dress pack](https://static.makehumancommunity.org/assets/assetpacks/dress02.html), [individual asset](http://www.makehumancommunity.org/node/2558) |
| Elvaerwyn's Goddess Dress 5 (`elvs_goddess_dress5`) | Sorceress: sleeveless V-neck dress, muted green fabric suitable for a worn fantasy treatment | CC-BY; attribution required | [Dress pack](https://static.makehumancommunity.org/assets/assetpacks/dress02.html), [individual asset](http://www.makehumancommunity.org/node/2562) |

The official pack downloads are [shirts01 CC0 ZIP](https://files2.makehumancommunity.org/asset_packs/shirts01/shirts01_cc0.zip) (listed at 23 MB) and [dress02 CC-BY ZIP](https://files2.makehumancommunity.org/asset_packs/dress02/dress02_cc-by.zip) (listed at 145 MB). Check each extracted asset's license metadata before bundling it: retain the correct CC-BY version, author, attribution, license link and modification notice. Do not assume every MakeHuman community asset is CC0; only the selected tops are listed as CC0.

Source evidence: [official tops listing](https://github.com/makehumancommunity/makehuman-static-website/blob/master/content/Assets/AssetPacks/shirts01.md), [official dresses listing](https://github.com/makehumancommunity/makehuman-static-website/blob/master/content/Assets/AssetPacks/dress02.md), and [core asset license explanation](https://github.com/makehumancommunity/makehuman-static-website/blob/master/content/about/license.md). Garment thumbnails were inspected directly from that official repository. The download archives themselves were not retrieved or validated here.

## Integration needed

1. Fit a chosen garment to an adult human-proportioned base character in MakeHuman/MPFB or Blender. Avoid the rejected cartoon Knight proportions.
2. Verify the particular garment's license metadata, and add credits for CC-BY garments.
3. Combine with class props, weathered materials, and boots/armor where appropriate; avoid assuming an outfit changes stats or gender-specific capabilities.
4. Export a rigged FBX, optimize textures and mesh count for mobile, then test all battle animations for clipping.
5. Create a matching Unity prefab: `Resources/Heroes/<class>_Woman.prefab` or `Resources/Heroes/Named/<hero-id>.prefab`. Assign an Animator using the Idle/Attack/Cast/Dodge/Parry/Hit/Death states and the `HeroVisual` component.

The roster includes adult women in every class. Current gameplay still uses neutral placeholders until matching female meshes are supplied. The shortlist is asset research, not a claim that the outfits are playable, rigged, commercially bundled, or validated on iPhone.
