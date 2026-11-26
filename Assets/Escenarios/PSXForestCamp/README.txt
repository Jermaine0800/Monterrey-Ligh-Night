PSX Forest Camp Starter Pack v1.0
by Nickben12

Free PS1 style forest campsite: 12 low-poly models (tent, campfire, pine tree, picnic table, bear trap...) with tiny dithered textures. Ready for Unity, Unreal and Godot.

CONTENTS (12 models)
  Nature (4)
    Ground_Forest_2m_01         10 tris    2.00 x  2.00 x  0.05 m   256 px
    Tree_Pine_01               304 tris    2.43 x  2.43 x  3.36 m   256 px
    Log_Seat_01                 56 tris    1.40 x  0.35 x  0.52 m   256 px
    Stump_Axe_01               176 tris    0.90 x  0.77 x  0.83 m   256 px
  Camp (4)
    Tent_01                    190 tris    1.74 x  3.53 x  1.30 m   256 px
    Campfire_01                426 tris    0.94 x  0.96 x  0.43 m   256 px
    Picnic_Table_01            228 tris    1.80 x  1.52 x  0.79 m   256 px
    Firewood_Pile_01           216 tris    0.62 x  0.65 x  0.39 m   256 px
  Props (4)
    Lantern_01                 198 tris    0.18 x  0.19 x  0.36 m   128 px
    Sign_Trail_01               66 tris    1.26 x  0.52 x  1.88 m   256 px
    Cooler_01                  149 tris    0.69 x  0.41 x  0.38 m   128 px
    Bear_Trap_01               320 tris    1.09 x  0.33 x  0.14 m   128 px

MORE
  More PS1 horror assets: the PSX Abandoned House Pack (45 models) https://nickben12.itch.io/psx-abandoned-house-pack and more free packs on my page, follow to get the next ones: https://nickben12.itch.io

FOLDERS
  FBX/                  one .fbx per model (Unity, Unreal, most engines)
  GLB/                  one .glb per model, texture embedded, nearest filtering (Godot, three.js...)
  OBJ/                  one .obj + .mtl per model
  Textures/             <Model>_Albedo.png: one tiny texture per model (64 to 256 px,
                        32-colour palette, dithered, dirt and shading painted in)
  Source/               Blender source file (.blend): game meshes + the high-poly sources
  Previews/             a preview image of every model

TECHNICAL NOTES
  - Real-world scale: 1 unit = 1 metre.
  - Pivot at the bottom centre of every model, front facing -Y in Blender
    (-Z in Unity / Godot after import).
  - Ground tiles: 2 x 2 m with their top at height 0, they tile on a 2 m grid.
  - The lantern, flames and embers are painted bright: add an emissive material or a
    point light in your engine to make them glow.
  - Every model: one mesh, one material, one albedo texture. No normal or PBR maps:
    the PS1 look comes from the low-res, dithered textures.

GETTING THE PS1 LOOK
  - Texture filtering: Point / Nearest, no mipmaps, no compression.
  - Unlit or simple vertex-lit materials, low render resolution (e.g. 320x240 upscaled),
    optional vertex snapping and affine texture mapping shaders (engine side).

IMPORT TIPS
  Unity   : drag the FBX and Textures folders into Assets, set the textures to
            Filter Mode: Point, Compression: None, Generate Mip Maps: off.
  Unreal  : import the FBX files with their textures, set Texture Group to '2D Pixels'.
  Godot 4 : drop the GLB files into your project (nearest filtering is already set).

LICENSE
  See LICENSE.txt. Free for personal and commercial projects. Thank you for downloading!
