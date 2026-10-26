import bpy, os, shutil
W = "/private/tmp/claude-501/-Users-darshilbansal/dff3e1f5-ed21-426d-8fff-8d00ad6b92c5/scratchpad/dino/"
OUT = os.path.expanduser("~/Desktop/CWK/Creatures/Tank Dino/"); os.makedirs(OUT, exist_ok=True)
sc = bpy.context.scene; sc.render.fps = 30; sc.render.fps_base = 1
shutil.copy(W + "TankDino_Native.png", OUT + "TankDino.png")
im = bpy.data.images["TankDino_Native"]
if im.packed_file: im.unpack(method="REMOVE")
im.filepath = OUT + "TankDino.png"; im.reload()
ao = bpy.data.objects["DinoRig"]; ao.animation_data.action = bpy.data.actions["Idle"]
for o in bpy.data.objects: o.select_set(o.name in ("DinoRig", "TankDino", "TankDino_Outline"))
bpy.ops.export_scene.fbx(filepath=OUT + "TankDino.fbx", use_selection=True, path_mode="RELATIVE", embed_textures=False,
    bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0.0,
    add_leaf_bones=False, axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True,
    mesh_smooth_type="FACE")
bpy.ops.export_scene.gltf(filepath=OUT + "TankDino.glb", use_selection=True, export_format="GLB",
    export_animations=True, export_animation_mode="ACTIONS", export_skins=True)
im.pack()
bpy.ops.wm.save_as_mainfile(filepath=OUT + "TankDino.blend", copy=True)
os.makedirs(OUT + "scripts", exist_ok=True)
for f in ("common.py", "posekit.py", "rig.py", "anim.py", "native.py", "render.py", "export.py", "loopcheck.py"): shutil.copy(W + f, OUT + "scripts/" + f)
print("EXPORT OK", sorted(os.listdir(OUT)))
