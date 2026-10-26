"""CornSamurai: create a simple CWK-ready armature from the currently loaded Blender model.

Expected: import the CornSamurai mesh(es) first. The character is assumed to be upright
(Z up), with left/right along X and the front facing -Y. Sword is intentionally excluded;
it can be added later as a separate object parented to hand_L or hand_R.
"""
import bpy
from mathutils import Vector

# ---- Settings
ARMATURE_NAME = "CornSamuraiRig"
MESH_NAME_HINT = "CornSamurai"  # meshes whose names contain this text are preferred
USE_AUTOMATIC_WEIGHTS = True

# Find mesh objects. Prefer the CornSamurai-named meshes, otherwise use selected meshes.
selected_meshes = [o for o in bpy.context.selected_objects if o.type == "MESH"]
named_meshes = [
    o for o in bpy.context.scene.objects
    if o.type == "MESH" and MESH_NAME_HINT.lower() in o.name.lower()
]
meshes = named_meshes or selected_meshes
if not meshes:
    raise RuntimeError(
        "CornSamurai rig: no mesh found. Import/select the character mesh(es), then run this script."
    )

# Use world-space bounds so the script works with multiple mesh parts.
world_points = []
for obj in meshes:
    world_points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
min_v = Vector((min(p.x for p in world_points),
                min(p.y for p in world_points),
                min(p.z for p in world_points)))
max_v = Vector((max(p.x for p in world_points),
                max(p.y for p in world_points),
                max(p.z for p in world_points)))
size = max_v - min_v
if min(size.x, size.z) <= 1e-6:
    raise RuntimeError("CornSamurai rig: mesh bounds are invalid; check model scale/orientation.")

cx = (min_v.x + max_v.x) * 0.5
cy = (min_v.y + max_v.y) * 0.5
z0, z1 = min_v.z, max_v.z
H = z1 - z0
W = max_v.x - min_v.x
D = max_v.y - min_v.y

# Proportional landmarks; adjust these after checking the actual model in Blender.
def P(x, z, y=None):
    return (cx + x * W, cy if y is None else cy + y * D, z0 + z * H)

# Remove an existing generated armature only; leave meshes/materials untouched.
old = bpy.data.objects.get(ARMATURE_NAME)
if old:
    bpy.data.objects.remove(old, do_unlink=True)

arm_data = bpy.data.armatures.new(ARMATURE_NAME)
arm = bpy.data.objects.new(ARMATURE_NAME, arm_data)
bpy.context.collection.objects.link(arm)
arm.show_in_front = True
arm.data.display_type = "OCTAHEDRAL"
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
for obj in bpy.context.selected_objects:
    if obj != arm:
        obj.select_set(False)

# Bone endpoints in world-like model coordinates. The proportions are a starting
# template, not a substitute for fitting joints to the final CornSamurai geometry.
bones = {
    "root":       (P(0, 0.00), P(0, 0.06), None),
    "body":       (P(0, 0.08), P(0, 0.43), "root"),
    "spine":      (P(0, 0.38), P(0, 0.66), "body"),
    "neck":       (P(0, 0.65), P(0, 0.75), "spine"),
    "head":       (P(0, 0.73), P(0, 0.94), "neck"),
    "upperarm_L": (P(0.10, 0.61), P(0.27, 0.55), "spine"),
    "forearm_L":  (P(0.27, 0.55), P(0.34, 0.43), "upperarm_L"),
    "hand_L":     (P(0.34, 0.43), P(0.39, 0.41), "forearm_L"),
    "upperarm_R": (P(-0.10, 0.61), P(-0.27, 0.55), "spine"),
    "forearm_R":  (P(-0.27, 0.55), P(-0.34, 0.43), "upperarm_R"),
    "hand_R":     (P(-0.34, 0.43), P(-0.39, 0.41), "forearm_R"),
    "leg_L":      (P(0.12, 0.16), P(0.13, 0.02), "body"),
    "leg_R":      (P(-0.12, 0.16), P(-0.13, 0.02), "body"),
}

bpy.ops.object.mode_set(mode="EDIT")
edit_bones = arm.data.edit_bones
for name, (head, tail, parent_name) in bones.items():
    b = edit_bones.new(name)
    b.head = head
    b.tail = tail
    if parent_name:
        b.parent = edit_bones[parent_name]
        b.use_connect = False
bpy.ops.object.mode_set(mode="OBJECT")

# Parent each mesh to the armature and generate weights. If automatic weighting
# fails for a disconnected/very low-poly part, the armature is still created;
# that mesh can be weighted manually in Blender.
if USE_AUTOMATIC_WEIGHTS:
    for obj in meshes:
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        arm.select_set(True)
        bpy.context.view_layer.objects.active = arm
        try:
            bpy.ops.object.parent_set(type="ARMATURE_AUTO")
        except RuntimeError as exc:
            print("WARNING: automatic weights failed for", obj.name, "-", exc)
            obj.parent = arm
else:
    for obj in meshes:
        obj.parent = arm

bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
print("CornSamurai rig created:", [b.name for b in arm.data.bones])
print("Model bounds (X, Y, Z):", tuple(round(v, 4) for v in size))
print("Review bone placement and test pose mode before exporting. Sword is intentionally not included.")
