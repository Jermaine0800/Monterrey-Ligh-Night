PSX Graveyard Starter Pack v1.0
by Nickben12

Free PS1 style graveyard: 12 low-poly models (crypt, gravestones, iron fence and gate, coffin, dead tree...) with tiny dithered textures. Ready for Unity, Unreal and Godot.

CONTENTS (12 models)
  Ground & Fence (4)
    Ground_Dirt_2m_01           10 tris    2.00 x  2.00 x  0.05 m   256 px
    Fence_Iron_2m_01           242 tris    2.05 x  0.20 x  1.70 m   256 px
    Fence_Gate_01              500 tris    2.50 x  0.68 x  2.21 m   256 px
    Crypt_01                   140 tris    2.20 x  2.75 x  2.90 m   256 px
  Graves (4)
    Gravestone_Rounded_01       44 tris    0.76 x  0.32 x  0.95 m   256 px
    Gravestone_Cross_01        140 tris    0.69 x  0.40 x  1.30 m   256 px
    Grave_Mound_01             108 tris    0.87 x  2.16 x  0.95 m   256 px
    Coffin_01                  136 tris    0.71 x  2.09 x  0.44 m   256 px
  Props (4)
    Lamp_Post_01               112 tris    0.36 x  0.36 x  2.59 m   256 px
    Tree_Dead_01               628 tris    0.95 x  0.96 x  3.29 m   256 px
    Candles_Skull_01           376 tris    0.40 x  0.38 x  0.40 m   128 px
    Shovel_Dirt_01             146 tris    0.88 x  0.64 x  1.19 m   256 px

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
  - Ground tiles: 2 x 2 m with their top at height 0. Fence segments are 2 m long and
    centred: place them every 2 m along X, each one brings the post at its left end.
  - The gate is 2.4 m wide between its pillar centres +-1 m (opening 1.6 m).
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
