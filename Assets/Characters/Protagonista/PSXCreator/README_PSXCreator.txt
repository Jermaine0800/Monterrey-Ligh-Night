PSX Character Creator Free
==========================

A man and a woman, 15 pieces to dress them in and a ready-made student, taken from PSX Character
Creator (two bodies in four skin tones, 118 pieces, fourteen characters, a Unity package):
https://heyheythere.itch.io/psx-character-creator
Each piece is rigged to its body's skeleton and tinted any colour. Low-poly, one 1024x1024
point-filtered texture atlas, the shading baked into vertex colours, real-world scale (1 unit = 1
metre).

What's in the zip
-----------------
glb/        one file per body, piece and character, with its animations (Godot, Blender, three.js)
fbx/        the same, the animations as takes (Unreal, Unity, most 3D apps)
obj/        the same, in the bind pose (any 3D app)
textures/   atlas.png, the one texture everything uses
icons/      an icon each, 256x256 PNG with transparency
props.json  everything: title, group, size, triangles, parts, animations, and what it is ("wear")
blender/    everything in one .blend, the atlas packed in
addons/     the Godot addon, psx_character_creator/: this folder is a Godot 4.3+ project that
            opens on its creator (.gdignore keeps Godot out of the other folders)

The bodies
----------
man_2 and woman_1: the man and the woman, cropped hair, plain underwear. Each is rigid parts that
turn at their joints, the way PS1 characters were made, rigged: one mesh on an armature, each part
weighted whole to its bone, bound in a T-pose with standard humanoid bone names (Hips,
Spine, Head, LeftArm, LeftForeArm, LeftHand, LeftUpLeg, LeftLeg, LeftFoot and the Right ones). Its origin is on the ground under it, and it faces
Blender's -Y (+Z in Godot and Unity). Each has ten animations:
  idle            a loop: standing at ease, looking about
  walk, run       loops; they play in place, for your code to move the character
  talk            a loop: talking with the hands, nodding
  wave            a loop: waving, the hand up by the head
  sit             a loop: sitting where it stands, on a seat about 0.4 m high
  cower           a loop: crouched, arms over the head, trembling
  attack          a punch, once
  hit             a flinch, once, back to where it started
  death           a fall, once, ending on the ground

The pieces
----------
man_hair_short, man_beard_moustache, man_cap, man_tee, man_jacket, man_jeans, man_boots for the man;
woman_hair_ponytail, woman_hair_bob, woman_sunglasses, woman_tank_top, woman_hoodie, woman_jeans,
woman_skirt, woman_trainers for the woman.
A piece is a skinned mesh on the same skeleton as its body, bound where the body is: put it on
the body's skeleton and it moves as the body does, whatever animation plays. A slot holds one
piece; clothes are layered to sit over each other (trousers under a top under a jacket). The
pieces are painted near-white: multiply your material's colour by the tint you want. Each
piece's "wear" in props.json has its slot, its body, its own colour and the swatches the
creators offer for it. A bone a piece doesn't cover holds a speck a few millimetres across,
inside the body, so every piece has every bone.

The character
-------------
student: the woman with her ponytail, hoodie, jeans and trainers on, one mesh with the ten
animations, for where you don't want to put her together yourself. Its "wear" in props.json is the
look she was made as.

Godot 4.3 and later
-------------------
Copy addons/psx_character_creator/ into your project's addons/ folder. character.gd puts a
character together: a Node3D wearing a body and pieces, by name.

    var Character = preload("res://addons/psx_character_creator/character.gd")
    var c = Character.new()
    add_child(c)
    c.dress({"body": "woman_1", "pieces": [["woman_hoodie", "5e3a6a"], ["woman_jeans", "2e5a8a"]]})
    c.put_on("woman_tank_top", Color("8a2424"))  # in place of the hoodie
    c.play("walk")
    var saved = c.look()                         # the same Dictionary back, for a save

Each body, piece and character is also a scene in props/. demo/gallery.tscn opens on the creator:
the body, the skin and each slot on the panel (a slot's name picks it for the colours), the
ready-made looks, Random, Save and Load (user://psx_character.json), the animations below; Gallery
shows each prop on its own (a piece worn on its body), arrows to browse, A for all of them.
The props use one material, props.tres, lit; a piece's colour is a copy of it with albedo_color
set. For the full PS1 look,
PSX Look comes with it, off as it starts.

Other engines and apps
----------------------
Use fbx/, glb/ or obj/ with textures/atlas.png. Set the texture's filtering to nearest/point and
turn off mipmaps for the crisp PS1 texels, and multiply it by the vertex colours (the baked
shading) and, for a piece, by its tint in your material. A piece goes on a body with your engine's
way of sharing a skeleton: in Unreal, a Skeletal Mesh Component per piece under the body's, its
Leader Pose Component set to the body's.

License: CC BY 4.0, see LICENSE.txt for the credit line. Made by heyheythere: https://heyheythere.itch.io
