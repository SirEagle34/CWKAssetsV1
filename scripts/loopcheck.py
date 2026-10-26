import bpy, numpy as np
ao = bpy.data.objects["DinoRig"]
def snap(act, f):
    ao.animation_data.action = None; ao.pose.apply_pose_from_action(act, evaluation_time=f); bpy.context.view_layer.update()
    return np.array([(ao.matrix_world @ pb.tail)[:] for pb in ao.pose.bones if pb.name not in ("prop",)] +
                    [(ao.matrix_world @ (pb.matrix @ __import__("mathutils").Vector((0.05, 0, 0)).to_4d()).to_3d())[:] for pb in ao.pose.bones if pb.name == "prop"])
ref = snap(bpy.data.actions["Idle"], 0)
for a in bpy.data.actions:
    L = int(a.frame_range[1])
    print(f"LOOP {a.name:7s} L{L:3d} start {np.abs(snap(a, 0) - ref).max()*1000:6.2f} mm  end {np.abs(snap(a, L) - ref).max()*1000:6.2f} mm")
