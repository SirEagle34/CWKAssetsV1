"""Native CWK look: the game shader is Mobile/Unlit, so the light lives in the texture. Keeps Tripo's painted albedo,
adds a hard 2-tone cel shade from one upper-left key light, and a thin dark inverted-hull outline that shares the
material (its UVs sit on a painted swatch). Saves native.blend."""
import bpy, math, bmesh, numpy as np
W = "/private/tmp/claude-501/-Users-darshilbansal/dff3e1f5-ed21-426d-8fff-8d00ad6b92c5/scratchpad/dino/"
RES = 2048; LINE = np.array([28, 34, 30], np.float32) / 255; SHADE = np.array([0.74, 0.76, 0.86], np.float32)  # cool shade tone
o = bpy.data.objects["TankDino"]; ao = bpy.data.objects["DinoRig"]; mat = o.data.materials[0]; nt = mat.node_tree
texnode = [n for n in nt.nodes if n.type == "TEX_IMAGE"][0]
src = bpy.data.images.load(W + "TankDino_Albedo.png"); texnode.image = src
alb = np.array(src.pixels[:], np.float32).reshape(RES, RES, 4)[..., :3]

# ---- key light bake (rest pose, no cast shadows: they stripe the wings), alpha marks the covered texels
ao.data.pose_position = "REST"; bpy.context.view_layer.update()
sc = bpy.context.scene; sc.render.engine = "CYCLES"; sc.cycles.samples = 32; sc.cycles.device = "CPU"
sun = bpy.data.objects.new("key", bpy.data.lights.new("key", "SUN")); sc.collection.objects.link(sun)
sun.data.energy = 3; sun.data.angle = math.radians(3); sun.rotation_euler = (math.radians(45), 0, math.radians(-50))
img = bpy.data.images.new("bl", RES, RES, alpha=True, float_buffer=True); img.generated_color = (0, 0, 0, 0)
node = nt.nodes.new("ShaderNodeTexImage"); node.image = img
for x in nt.nodes: x.select = False
node.select = True; nt.nodes.active = node
for x in bpy.data.objects: x.select_set(False)
o.select_set(True); bpy.context.view_layer.objects.active = o; o.visible_shadow = False
sc.render.bake.margin = 8
bpy.ops.object.bake(type="DIFFUSE", pass_filter={"DIRECT"}, margin=8)
bl = np.array(img.pixels[:], np.float32).reshape(RES, RES, 4)
light, covered = bl[..., :3].mean(-1), bl[..., 3] > 0
nt.nodes.remove(node); bpy.data.objects.remove(sun); ao.data.pose_position = "POSE"; sc.render.engine = "BLENDER_EEVEE"

def blur(a, r):
    for ax in (0, 1):
        c = np.cumsum(np.pad(a, [(r + 1, r) if i == ax else (0, 0) for i in range(a.ndim)], mode="edge"), axis=ax)
        a = (np.take(c, range(2 * r + 1, c.shape[ax]), axis=ax) - np.take(c, range(0, c.shape[ax] - 2 * r - 1), axis=ax)) / (2 * r + 1)
    return a
lt = blur(light, 2); thr = 0.2 * np.percentile(lt[covered], 99)
lit = np.clip((lt - thr) / 0.01 + 0.5, 0, 1)[..., None]           # hard cel edge
col = alb * (lit + (1 - lit) * SHADE)
print("LIT share", float(lit[covered].mean()))

# ---- outline swatch on a free block of the atlas
free = ~(blur(covered.astype(np.float32), 2) > 0)
ii = np.pad(free.astype(np.int32).cumsum(0).cumsum(1), ((1, 0), (1, 0))); need = 12
okb = (ii[need:, need:] - ii[:-need, need:] - ii[need:, :-need] + ii[:-need, :-need]) == need * need
by, bx = [int(v) for v in np.argwhere(okb)[len(np.argwhere(okb)) // 2]]
col[by:by + need, bx:bx + need] = LINE
SW_UV = ((bx + need / 2) / RES, (by + need / 2) / RES); print("SWATCH", bx, by)
out = bpy.data.images.new("TankDino_Native", RES, RES)
out.pixels[:] = np.concatenate([np.clip(col, 0, 1), np.ones((RES, RES, 1), np.float32)], -1).ravel()
out.filepath_raw = W + "TankDino_Native.png"; out.file_format = "PNG"; out.save(); texnode.image = out

# ---- outline shell: smoothed-normal offset, flipped, same weights; none on the thin flag / propeller blades
sh = o.copy(); sh.data = o.data.copy(); sh.name = sh.data.name = "TankDino_Outline"; sc.collection.objects.link(sh)
bm = bmesh.new(); bm.from_mesh(sh.data); bm.verts.ensure_lookup_table()
dl = bm.verts.layers.deform.verify(); skip = {sh.vertex_groups[n].index for n in ("flag", "prop")}
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v[dl] and max(v[dl].items(), key=lambda kv: kv[1])[0] in skip], context="VERTS")
# smooth normals across coincident verts so split seams don't crack the hull
key = {}
for v in bm.verts: key.setdefault(tuple(np.round(v.co[:], 4)), []).append(v)
for vs in key.values():
    n = sum((v.normal for v in vs), vs[0].normal * 0).normalized()
    for v in vs: v.co += n * 0.0045
bmesh.ops.reverse_faces(bm, faces=bm.faces)
uv = bm.loops.layers.uv.active
for f in bm.faces:
    for l in f.loops: l[uv].uv = SW_UV
bm.to_mesh(sh.data); bm.free()
print("OUTLINE verts", len(sh.data.vertices))
mat.use_backface_culling = True
bpy.ops.wm.save_as_mainfile(filepath=W + "native.blend")
