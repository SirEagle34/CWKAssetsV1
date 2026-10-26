"""World-axis pose helpers shared by test and animation scripts."""
import bpy, math
from mathutils import Matrix, Quaternion, Vector

def rig():
    return bpy.data.objects["DinoRig"]

def wq(pb, axis, deg):
    """local quaternion for a rotation of `deg` about a rig-space axis, in this bone's rest frame"""
    M = pb.bone.matrix_local.to_3x3()
    R = Quaternion(Vector(axis).normalized(), math.radians(deg)).to_matrix()
    return (M.inverted() @ R @ M).to_quaternion()

def setrot(name, *rots):
    """rots: (axis, deg) pairs, composed in order"""
    pb = rig().pose.bones[name]; pb.rotation_mode = "QUATERNION"
    q = Quaternion()
    for axis, deg in rots: q = wq(pb, axis, deg) @ q
    pb.rotation_quaternion = q
    return pb

def setloc(name, vec):
    """translation in rig space"""
    pb = rig().pose.bones[name]
    pb.location = pb.bone.matrix_local.to_3x3().inverted() @ Vector(vec)
    return pb

def reset():
    for pb in rig().pose.bones:
        pb.rotation_mode = "QUATERNION"; pb.rotation_quaternion = Quaternion(); pb.location = (0, 0, 0); pb.scale = (1, 1, 1)

# side conventions (front = -Y): R side is -X
X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)
def sgn(side): return 1 if side == "R" else -1
def abduct(side, deg):      # arm out to the side (+)
    return ((0, sgn(side), 0), deg)
def flex(deg):              # arm/leg forward (+)
    return ((-1, 0, 0), deg)
def lean(deg):              # torso/head forward (+)
    return ((1, 0, 0), deg)
def turn(deg):              # look to his left (+) = toward +X
    return ((0, 0, 1), deg)
def tilt(deg):              # head roll toward his left shoulder (+)
    return ((0, 1, 0), deg)
