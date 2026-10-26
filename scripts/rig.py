"""Tank Dino: Tripo FBX -> straightened mesh + armature. Body = heat weights; gear pieces ride their own bones
rigidly (helmet/teeth on head, propeller spins, radio + flag in the left hand, backpack + biplane wings on a pack bone,
two tanks as feet with turrets and recoiling barrels). Saves rig.blend."""
import sys; sys.path.insert(0, "/private/tmp/claude-501/-Users-darshilbansal/dff3e1f5-ed21-426d-8fff-8d00ad6b92c5/scratchpad/dino")
from common import *
from mathutils import Vector

o = load(); P = parts(o); o.name = o.data.name = "TankDino"
co = np.array([v.co[:] for v in o.data.vertices])
C = lambda idx: co[idx].mean(0)

# ---- classify the gear pieces
body = P[0]; role = {}
for k, idx in enumerate(P[1:], 1):
    c, lo, hi = C(idx), co[idx].min(0), co[idx].max(0); span = hi - lo
    if hi[2] < 0.2:
        side = "R" if c[0] < 0.05 else "L"
        role[k] = ("barrel_" if c[2] > 0.14 else "turret_" if c[2] > 0.115 else "tank_") + side
    elif lo[2] > 0.89 and span[0] > 0.4: role[k] = "prop"
    elif c[0] > 0.25 and c[2] > 0.55: role[k] = "flag" if (lo[2] > 0.93 and len(idx) == 12) else "radio"
    elif span[0] > 0.3 and 0.4 < c[2] < 0.65: role[k] = "pack"                     # biplane wings
    elif len(idx) == 318 or (len(idx) == 14 and c[2] > 0.7): role[k] = "pack"     # backpack + its buckles
    elif c[1] > 0.1 and c[2] < 0.45: role[k] = "tail"                              # tail spikes
    elif hi[2] < 0.4 and lo[2] > 0.28: role[k] = "hips"                            # belt pouches
    elif len(idx) == 387: role[k] = "~neck"                                        # rope round the neck
    elif len(idx) in (182, 38): role[k] = "~armL"                                  # strap loop round the left shoulder
    elif c[2] > 0.58: role[k] = "head"                                             # helmet, crest, goggle, eyes, teeth
    else: role[k] = "chest"
for k in sorted(role): print("PART", k, len(P[k]), C(P[k]).round(3), role[k])

def tip_pair(idx, near):
    """barrel: hinge = vert nearest the turret, muzzle = vert farthest from it"""
    c = co[idx]; d = np.linalg.norm(c - near, axis=1); return c[d.argmin()], c[d.argmax()]

B = {}
def bone(n, h, t, p=None): B[n] = (np.array(h, float), np.array(t, float), p)
bone("root", [0, 0, 0], [0, 0, 0.1])
bone("hips", [0.03, -0.16, 0.22], [0.03, -0.16, 0.38], "root")
bone("spine", B["hips"][1], [0.03, -0.16, 0.50], "hips")
bone("chest", B["spine"][1], [0.02, -0.15, 0.60], "spine")
bone("neck", B["chest"][1], [0.02, -0.14, 0.67], "chest")
bone("head", B["neck"][1], [0.02, -0.14, 0.86], "neck")
bone("upperarm_L", [0.13, -0.16, 0.60], [0.25, -0.14, 0.545], "chest")
bone("forearm_L", B["upperarm_L"][1], [0.33, -0.19, 0.60], "upperarm_L")
bone("hand_L", B["forearm_L"][1], [0.39, -0.25, 0.645], "forearm_L")
bone("upperarm_R", [-0.10, -0.17, 0.58], [-0.24, -0.22, 0.49], "chest")
bone("forearm_R", B["upperarm_R"][1], [-0.19, -0.25, 0.41], "upperarm_R")
bone("hand_R", B["forearm_R"][1], [-0.15, -0.27, 0.38], "forearm_R")
bone("thigh_R", [-0.08, -0.19, 0.30], [-0.09, -0.22, 0.14], "hips")
bone("thigh_L", [0.15, -0.18, 0.30], [0.17, -0.20, 0.14], "hips")
TAIL = [[0.0, 0.0, 0.34], [-0.05, 0.14, 0.26], [-0.075, 0.264, 0.231], [-0.15, 0.37, 0.225], [-0.215, 0.47, 0.26]]
for i in range(4): bone(f"tail{i + 1}", TAIL[i], TAIL[i + 1], "hips" if i == 0 else f"tail{i}")
for s in "RL":
    tk = np.concatenate([P[k] for k, r in role.items() if r.endswith("_" + s)])
    tc = co[tk].mean(0); bone("tank_" + s, [tc[0], tc[1], 0], [tc[0], tc[1], 0.08], "root")
    tu = np.concatenate([P[k] for k, r in role.items() if r == "turret_" + s]); uc = co[tu].mean(0)
    bone("turret_" + s, [uc[0], uc[1], 0.1], [uc[0], uc[1], 0.17], "tank_" + s)
    ba = np.concatenate([P[k] for k, r in role.items() if r == "barrel_" + s]); h, t = tip_pair(ba, uc)
    bone("barrel_" + s, h, t, "turret_" + s)
bone("pack", [0.0, 0.05, 0.60], [0.0, 0.05, 0.75], "chest")
pc = C(np.concatenate([P[k] for k, r in role.items() if r == "prop"]))
bone("prop", [pc[0], pc[1], 0.90], [pc[0], pc[1], 0.96], "head")
bone("radio", [0.31, -0.22, 0.60], [0.31, -0.22, 0.83], "hand_L")
fl = co[np.concatenate([P[k] for k, r in role.items() if r == "flag"])]
bone("flag", [fl[:, 0].min(), fl[:, 1].mean(), fl[:, 2].mean()], [fl[:, 0].max(), fl[:, 1].mean(), fl[:, 2].mean()], "radio")

arm = bpy.data.armatures.new("DinoRig"); ao = bpy.data.objects.new("DinoRig", arm)
bpy.context.scene.collection.objects.link(ao); bpy.context.view_layer.objects.active = ao
bpy.ops.object.mode_set(mode="EDIT")
for n, (h, t, p) in B.items():
    eb = arm.edit_bones.new(n); eb.head = Vector(h); eb.tail = Vector(t)
for n, (h, t, p) in B.items():
    if p: arm.edit_bones[n].parent = arm.edit_bones[p]
bpy.ops.object.mode_set(mode="OBJECT"); arm.display_type = "STICK"
BN = list(B); print("BONES", len(BN))

# ---- body: heat weights on a body-only copy (vertex order kept), body bones only
BODY_BONES = ["hips", "spine", "chest", "neck", "head", "upperarm_L", "forearm_L", "hand_L", "upperarm_R", "forearm_R",
              "hand_R", "thigh_R", "thigh_L", "tail1", "tail2", "tail3", "tail4"]
tmp = o.copy(); tmp.data = o.data.copy(); bpy.context.scene.collection.objects.link(tmp)
bm = bmesh.new(); bm.from_mesh(tmp.data); bm.verts.ensure_lookup_table()
keep = set(body.tolist()); bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.index not in keep], context="VERTS")
bm.to_mesh(tmp.data); bm.free()
for b in arm.bones: b.use_deform = b.name in BODY_BONES
for x in bpy.data.objects: x.select_set(False)
tmp.select_set(True); ao.select_set(True); bpy.context.view_layer.objects.active = ao
bpy.ops.object.parent_set(type="ARMATURE_AUTO")
for b in arm.bones: b.use_deform = True
W8 = np.zeros((len(co), len(BN))); bidx = np.sort(body)
for v in tmp.data.vertices:
    for g in v.groups:
        n = tmp.vertex_groups[g.group].name
        if n in BN: W8[bidx[v.index], BN.index(n)] = g.weight
miss = (W8[body].sum(1) == 0).sum(); print("HEAT unweighted body verts", miss)
bpy.data.objects.remove(tmp)

def segdist(pts, h, t):
    d = t - h; u = np.clip((pts - h) @ d / max(d @ d, 1e-9), 0, 1)
    return np.linalg.norm(pts - (h + u[:, None] * d), axis=1)
def assign(idx, allowed, power=4):
    cols = [BN.index(b) for b in allowed]
    D = np.stack([segdist(co[idx], B[b][0], B[b][1]) for b in allowed], 1) + 0.004
    w = 1 / D ** power; W8[np.ix_(idx, [c for c in cols])] = w / w.sum(1, keepdims=True)
# heat failures (if any) fall back to distance weights
bad = body[W8[body].sum(1) == 0]
if len(bad): assign(bad, BODY_BONES)
# right hand is fused to the hip (akimbo): the arm is a handle between chest and hips, it rides the torso
for src, dst in (("upperarm_R", "chest"), ("forearm_R", "spine"), ("hand_R", "hips")):
    W8[:, BN.index(dst)] += W8[:, BN.index(src)]; W8[:, BN.index(src)] = 0
# feet stand in the tank hatches: the bottom of each leg rides its tank
fz = co[body][:, 2]; foot = body[(fz < 0.2) & (co[body][:, 1] < 0.05)]
for i in foot:
    s = "R" if co[i, 0] < 0.03 else "L"; t = np.clip((0.2 - co[i, 2]) / 0.06, 0, 1)
    W8[i] *= 1 - t; W8[i, BN.index("tank_" + s)] += t

from mathutils.kdtree import KDTree
kd = KDTree(len(body))
for i in body: kd.insert(co[i], int(i))
kd.balance()
def skin_like_body(idx):
    """straps, pouches, teeth: copy the weights of the skin underneath (4 nearest body verts)"""
    for i in idx:
        nb = kd.find_n(co[i], 4); w = np.array([1 / (d + 0.003) for _, _, d in nb])
        W8[i] = (W8[[j for _, j, _ in nb]] * w[:, None]).sum(0) / w.sum()
for k, r in role.items():
    idx = P[k]
    if r in ("~neck", "~armL", "chest") or (r == "head" and C(idx)[2] < 0.7): skin_like_body(idx)
    elif r == "tail":
        D = np.stack([segdist(co[idx], B[f"tail{i}"][0], B[f"tail{i}"][1]) for i in (1, 2, 3, 4)], 1).mean(0)
        W8[idx, BN.index(f"tail{D.argmin() + 1}")] = 1        # each spike rigid on its nearest tail bone
    else: W8[idx, BN.index(r)] = 1

top4 = np.argsort(-W8, 1)[:, 4:]; np.put_along_axis(W8, top4, 0, 1)
W8[W8 < 0.01] = 0; W8 /= W8.sum(1, keepdims=True)
assert np.allclose(W8.sum(1), 1), "unweighted verts"
for g in list(o.vertex_groups): o.vertex_groups.remove(g)
for j, n in enumerate(BN):
    vg = o.vertex_groups.new(name=n)
    for i in np.nonzero(W8[:, j])[0]: vg.add([int(i)], float(W8[i, j]), "REPLACE")
bpy.context.view_layer.objects.active = ao; bpy.ops.object.mode_set(mode="EDIT")
for n in ("hand_R", "forearm_R", "upperarm_R"): ao.data.edit_bones.remove(ao.data.edit_bones[n])
bpy.ops.object.mode_set(mode="OBJECT")
for n in ("hand_R", "forearm_R", "upperarm_R"): o.vertex_groups.remove(o.vertex_groups[n])
o.parent = ao; mod = o.modifiers.new("Armature", "ARMATURE"); mod.object = ao

im = [n.image for n in o.data.materials[0].node_tree.nodes if n.type == "TEX_IMAGE"][0]
im.scale(2048, 2048); im.name = "TankDino_Albedo"
im.filepath_raw = W + "TankDino_Albedo.png"; im.file_format = "PNG"; im.save()
o.data.materials[0].name = "TankDino"
print("VERTS", len(o.data.vertices), "FACES", len(o.data.polygons))
bpy.ops.wm.save_as_mainfile(filepath=W + "rig.blend")
