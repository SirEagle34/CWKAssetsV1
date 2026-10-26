"""preview: game look (unlit texture + outline), cream backdrop, soft contact shadow; every take in order"""
import sys; W = "/private/tmp/claude-501/-Users-darshilbansal/dff3e1f5-ed21-426d-8fff-8d00ad6b92c5/scratchpad/dino/"
import bpy, math, os
from mathutils import Vector
ORDER = ["Idle", "Intro", "Fidget", "Atk", "Lunge", "Charge", "Shoot", "HR"]
o = bpy.data.objects["TankDino"]; ao = bpy.data.objects["DinoRig"]; nt = o.data.materials[0].node_tree
tex = [n for n in nt.nodes if n.type == "TEX_IMAGE"][0]; out = [n for n in nt.nodes if n.type == "OUTPUT_MATERIAL"][0]
em = nt.nodes.new("ShaderNodeEmission"); nt.links.new(tex.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], out.inputs[0])
sc = bpy.context.scene; sc.render.engine = "BLENDER_EEVEE"; sc.eevee.taa_render_samples = 16
sc.render.resolution_x = sc.render.resolution_y = 720; sc.view_settings.view_transform = "Standard"
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.93, 0.87, 0.72, 1)
# floor that only catches a shadow (shadow catcher look via a dark, alpha-ish plane would need Cycles; use a soft blob)
bpy.ops.mesh.primitive_circle_add(vertices=48, radius=0.38, fill_type="TRIFAN", location=(0.03, -0.2, 0.001)); blob = bpy.context.object
bm_ = bpy.data.materials.new("blob"); bm_.use_nodes = True; bm_.blend_method = "BLEND" if hasattr(bm_, "blend_method") else None
n2 = bm_.node_tree; n2.nodes.clear()
tc = n2.nodes.new("ShaderNodeTexCoord"); gr = n2.nodes.new("ShaderNodeTexGradient"); gr.gradient_type = "SPHERICAL"
mp = n2.nodes.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value = (1.3, 1.3, 1)
tr = n2.nodes.new("ShaderNodeBsdfTransparent"); e2 = n2.nodes.new("ShaderNodeEmission"); e2.inputs[0].default_value = (0.55, 0.48, 0.36, 1)
mx = n2.nodes.new("ShaderNodeMixShader"); o2 = n2.nodes.new("ShaderNodeOutputMaterial")
n2.links.new(tc.outputs["Object"], mp.inputs[0]); n2.links.new(mp.outputs[0], gr.inputs[0])
n2.links.new(gr.outputs["Fac"], mx.inputs[0]); n2.links.new(tr.outputs[0], mx.inputs[1]); n2.links.new(e2.outputs[0], mx.inputs[2]); n2.links.new(mx.outputs[0], o2.inputs[0])
blob.data.materials.append(bm_); blob.scale = (1, 0.75, 1)
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam; cam.data.lens = 50
def aim(az, el=14, d=3.2, t=(0, -0.15, 0.55)):
    a = math.radians(az); e = math.radians(el); t = Vector(t)
    cam.location = t + Vector((d * math.cos(e) * math.sin(a), -d * math.cos(e) * math.cos(a), d * math.sin(e)))
    cam.rotation_euler = (t - cam.location).to_track_quat("-Z", "Y").to_euler()
os.makedirs(W + "frames", exist_ok=True)
for name in ORDER:
    act = bpy.data.actions[name]; L = int(act.frame_range[1])
    frames = [f % L for f in range(2 * L)] if name == "Idle" else list(range(L + 1))
    for i, f in enumerate(frames):
        ao.animation_data.action = None; ao.pose.apply_pose_from_action(act, evaluation_time=f); bpy.context.view_layer.update()
        root = ao.pose.bones["root"].location.z if False else (ao.matrix_world @ ao.pose.bones["root"].head).z
        blob.scale = (1, 0.75, 1); blob.hide_render = False
        s = max(0.35, 1 - root * 0.7); blob.scale = (s, 0.75 * s, 1)       # shadow shrinks as he flies in
        if name == "Intro": aim(25, d=3.8, t=(0, -0.15, 0.85))
        elif name in ("Atk", "Lunge"): aim(65)
        else: aim(25 + (i * 0.5 if name == "Idle" else 0))
        sc.render.filepath = f"{W}frames/{name}_{i:04d}.png"; bpy.ops.render.render(write_still=True)
    print("RENDERED", name, len(frames), flush=True)
