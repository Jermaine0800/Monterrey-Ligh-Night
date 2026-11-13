DESERT CRYPTIDS - 3 Rigged & Animated PSX Horror Creatures
by Hearthlane - https://hearthlanecreators.itch.io

Three low-poly PS1-style desert horror creatures, rigged, with 5 animations each.

CREATURES (triangle counts)
  DC_Chupacabra   Spined, mangy desert predator with a banded back and open wounds - 5,241 tris, 34 bones
  DC_RotCoyote    Rotting coyote pack enemy, fur sloughing off, smaller and faster - 4,389 tris, 34 bones
  DC_Mimic        Tall, pale long-armed thing wearing a coyote hide - 6,562 tris, 30 bones

ANIMATIONS (24 fps, in every GLB and FBX)
  Idle, Walk, Run, Attack, Death
  Walk and Run loop. Idle loops. Attack and Death play once.
  GLB: one animation clip per name. FBX: one take per name (shown as "<Rig>|Idle" etc.).

WHICH FOLDER SHOULD I USE?
  Models/PSX_VertexColor/   The PS1 way: one 256 px pixel-art texture per creature with the shading
                            stored in vertex colors. Use with a PSX/retro shader, Godot
                            (StandardMaterial3D: Vertex Color > Use as Albedo) or Blender.
  Models/Standard_Baked/    Same creatures with the shading baked into a 512 px texture (still
                            15-bit color, still pixel-crisp). Use with Unity's or Unreal's standard
                            materials, or anything that ignores vertex colors.
  Textures/                 PSX_Original = the pixel-art sheets; Baked = the baked sheets.
                            (FBX files look for their textures in the .fbm folder next to them.)
  Blender/                  Source .blend: one scene per creature, rig + mesh + all 5 animations as
                            NLA tracks (Idle is active; un-mute another track to preview it).
  Previews/                 Renders of the creatures.

SPECS
  Units: metres. Up axis: +Y in GLB/FBX. Pivot (root bone) on the ground between the feet.
  Facing: Blender -Y, which is +Z in the GLB (glTF's standard "front"). In Godot, rotate 180 degrees
  if your character script treats -Z as forward.
  Textures are snapped to 15-bit color, like the PS1. Set texture filtering to Nearest / Point:
    Godot: Import tab > Filter off.  Unity: Filter Mode: Point.  Unreal: Filter Nearest, 2D Pixels group.
  Eyes use a small emissive material. Turn on bloom/glow to make them shine in the dark.
  Polygon counts are higher than a real 1990s PS1 enemy (those were roughly 300-1,500 triangles);
  they're sized for modern "PSX-style" games. Add a Decimate step if you need them lighter.

LICENSE
  See LICENSE.txt. Short version: use them in commercial and non-commercial projects, modify
  freely, don't resell or redistribute the model files themselves. Credit appreciated.

AI DISCLOSURE
  These creatures were made with AI-assisted procedural modeling, rigging, animation and texture
  painting in Blender (the AI wrote and refined the code while checking renders).

Thanks for downloading! - Hearthlane
