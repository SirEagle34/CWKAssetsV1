"""8 CWK takes for the Tank Dino (30 fps). Every take starts and ends on Idle frame 0.
pose = looping idle (harmonics) + eased keyed offsets with per-part lag; the propeller always spins (whole turns per
take so it loops), the flag always flutters, the tail trails the body. Saves anim.blend."""
import sys; W = "/private/tmp/claude-501/-Users-darshilbansal/dff3e1f5-ed21-426d-8fff-8d00ad6b92c5/scratchpad/dino/"; sys.path.insert(0, W)
import bpy, math
from posekit import *
from mathutils import Vector, Quaternion

FPS = 30; IDLE = 60
# channels: rx ry rz = whole creature incl. tanks (ry forward = -), hy hz = body over the tanks, lean = torso forward,
# hlean/hturn/htilt = head, lift/swing/fore/wrist = left arm (radio), tur = both turrets, recR/recL = barrel recoil 0..1,
# spin = extra propeller speed (x base), flag = extra flutter, shake = buzz
CLIPS = {
    "Idle": (60, []),
    # helicopter drop-in: hangs off the propeller, lands on the tanks with a squash, looks round
    "Intro": (66, [(0, dict(rz=1.1, hz=0.05, spin=3, lean=-6, lift=25, flag=1)), (26, dict(rz=0.06, spin=3, lean=-4, lift=15, flag=1)),
                   (32, dict(hz=-0.06, lean=10, hlean=8, spin=2, lift=-8, tailup=-8)), (40, dict(hz=0.01, lean=-3, hlean=-6, spin=1)),
                   (50, dict(hturn=-14, htilt=4, tur=-20)), (66, {})]),
    # radio check: brings the radio to his mouth, talks, waves it, back
    "Fidget": (84, [(0, {}), (16, dict(swing=40, lift=-6, fore=34, hturn=22, hlean=6, lean=4, flag=0.5)),
                    (24, dict(swing=40, lift=-6, fore=34, hturn=22, hlean=2, lean=4, talk=1)),
                    (52, dict(swing=40, lift=-6, fore=34, hturn=22, hlean=2, lean=4, talk=1)),
                    (62, dict(lift=40, swing=-5, wrist=-20, hturn=6, hlean=-8, tur=25, flag=1.5)),
                    (70, dict(lift=40, swing=-5, wrist=20, hturn=6, hlean=-8, tur=25, flag=1.5)), (84, {})]),
    # chomp: rears back, snaps forward over the tanks
    "Atk": (45, [(0, {}), (12, dict(hy=0.06, hz=0.02, lean=-16, hlean=-22, lift=20, tailup=8, tur=0)),
                 (19, dict(ry=-0.10, hy=-0.07, lean=24, hlean=24, lift=-10, swing=20, tailup=-10, shake=0.5)),
                 (27, dict(ry=-0.08, hy=-0.05, lean=18, hlean=14, lift=-6, swing=12)), (45, {})]),
    # tanks charge forward, body leans into it
    "Lunge": (40, [(0, {}), (8, dict(ry=0.05, lean=-8, hlean=-6, tailup=4)), (16, dict(ry=-0.30, lean=20, hlean=10, lift=-14, swing=30, tailup=-10, flag=2)),
                   (24, dict(ry=-0.30, lean=12, hlean=4, flag=1)), (40, {})]),
    # calls in support: radio high, propeller goes wild, lifts off the tanks a little, buzzes
    "Charge": (60, [(0, {}), (14, dict(lift=95, swing=-10, fore=-20, hlean=-14, hturn=18, lean=-6, spin=3, hz=0.05, flag=2)),
                    (44, dict(lift=100, swing=-10, fore=-25, hlean=-16, hturn=18, lean=-6, spin=4, hz=0.07, shake=1, flag=2.5)), (60, {})]),
    # both tank cannons fire: turrets swing forward, R then L recoil, tank kick
    "Shoot": (45, [(0, {}), (8, dict(tur=1, lean=-4, hlean=-6, lift=10)), (12, dict(tur=1, recR=1, ry=0.015, lean=-6, hlean=-10, lift=14)),
                   (18, dict(tur=1, recL=1, ry=0.03, lean=-8, hlean=-12, lift=16)), (26, dict(tur=1, ry=0.01, lean=-2)), (45, {})]),
    "HR": (24, [(0, {}), (4, dict(hy=0.06, ry=0.03, lean=-18, hlean=-24, htilt=10, lift=30, tailup=12, shake=1, flag=2)),
                (10, dict(hy=0.02, lean=-6, hlean=-8, lift=8)), (24, {})]),
}
AIM = {"R": 0.0, "L": 0.0}           # tur=1 in Shoot means 'point the guns forward' (filled after the rig is read)

def ease(x): return x * x * x * (x * (6 * x - 15) + 10)
def chan(keys, f):
    for i in range(len(keys) - 1):
        f0, a = keys[i]; f1, b = keys[i + 1]
        if f0 <= f <= f1:
            u = ease((f - f0) / (f1 - f0)); return {c: a.get(c, 0) * (1 - u) + b.get(c, 0) * u for c in set(a) | set(b)}
    return {}
CUR_L = [0]
def lagged(keys, f, lag):
    """trailing parts run `lag` frames behind, closing the gap over the last 10 frames so every take ends on idle"""
    return chan(keys, max(0, f - lag * min(1, (CUR_L[0] - f) / 10)))

def prop_angles(L, keys):
    """cumulative propeller angle: base 1.5 turns/s scaled by (1 + spin), rounded to whole turns over the take"""
    sp = [1 + chan(keys, f).get("spin", 0) for f in range(L + 1)]
    cum = [0.0]
    for f in range(1, L + 1): cum.append(cum[-1] + (sp[f - 1] + sp[f]) / 2)
    turns = max(1, round(cum[-1] * 1.5 / FPS)); return [360 * turns * c / cum[-1] for c in cum]

def pose(f, L, keys, prop):
    n = max(1, round(L / IDLE)) * IDLE
    w = 2 * math.pi * (f * n / L) / IDLE
    s = lambda k, ph: math.sin(k * w + ph)
    C = chan(keys, f); g = lambda c, src=C: src.get(c, 0.0)
    shake = g("shake") * math.sin(f * 2.7) * 1.5
    reset()
    setloc("root", (g("rx"), g("ry"), g("rz")))
    setloc("hips", (0, g("hy"), 0.008 * s(1, 0) + g("hz")))                       # breathing bob over the tanks
    setrot("hips", turn(2 * s(1, 0.4)), tilt(1.2 * s(1, 1.0)))
    setrot("spine", lean(1.5 * s(1, 0.6) + g("lean") * 0.45 + shake * 0.5))
    setrot("chest", lean(1.5 * s(1, 0.9) + g("lean") * 0.55), turn(-1.5 * s(1, 0.7)))
    H = lagged(keys, f, 2)
    setrot("neck", lean(g("hlean", H) * 0.4))
    talk = g("talk") * (4 * math.sin(f * 1.1) + 3 * math.sin(f * 1.9))
    setrot("head", lean(2 * s(1, 1.3) + g("hlean", H) * 0.6 + talk + shake), turn(3 * s(1, 2.2) + g("hturn", H)), tilt(1.5 * s(1, 0.3) + g("htilt", H)))
    # left arm holds the radio up; forearm and hand trail
    A1, A2, A3 = lagged(keys, f, 1), lagged(keys, f, 2), lagged(keys, f, 3)
    setrot("upperarm_L", abduct("L", 3 * s(1, -0.6) + g("lift", A1)), ((0, 0, -1), g("swing", A1)))
    setrot("forearm_L", ((0, 0, -1), 4 * s(1, -1.0) + g("fore", A2)))
    # wrist undoes most of the arm swing so the radio stays upright with its antenna up
    setrot("hand_L", ((0, 0, 1), 0.8 * (g("fore", A2) + g("swing", A1))), tilt(0.9 * g("lift", A1) + g("wrist", A3) + 3 * s(1, -1.4)))
    # tail: lagged side-to-side wave, lifts with the body motion
    for i in (1, 2, 3, 4):
        T = lagged(keys, f, 2 * i)
        setrot(f"tail{i}", turn((5 + i) * s(1, -0.7 * i)), lean(-(T.get("tailup", 0)) * (0.5 + 0.25 * i) + 2 * s(2, -0.9 * i)))
    for sd in "RL":
        aim = g("tur") * AIM[sd] if CUR_N[0] == "Shoot" else g("tur") * (1 if sd == "R" else -1)
        setrot("turret_" + sd, turn(aim + 4 * s(1, 0.5 if sd == "R" else 2.6)))
        bb = rig().data.bones["barrel_" + sd]; ax = (bb.tail_local - bb.head_local).normalized()
        setloc("barrel_" + sd, tuple(-ax * 0.045 * g("rec" + sd)))
    setrot("prop", turn(prop[f]))
    fl = 1 + g("flag")
    setrot("flag", turn(fl * (12 * s(5, 0) + 6 * s(12, 1))))      # whole cycles per loop
    # the pack and its wings shiver a touch with the propeller
    setrot("pack", lean(0.6 * s(2, 0.3) + 0.3 * shake))

CUR_N = [""]
ao = rig(); ao.animation_data_create()
# Shoot: point each barrel straight ahead (-Y) from where it rests
for sd in "RL":
    b = ao.data.bones["barrel_" + sd]; d = b.tail_local - b.head_local
    AIM[sd] = (-90 - math.degrees(math.atan2(d.y, d.x)) + 180) % 360 - 180
print("AIM", AIM)
for a in list(bpy.data.actions): bpy.data.actions.remove(a)
bpy.context.scene.render.fps = FPS; bpy.context.scene.render.fps_base = 1
for name, (L, keys) in CLIPS.items():
    act = bpy.data.actions.new(name); act.use_fake_user = True; ao.animation_data.action = act
    CUR_L[0] = L; CUR_N[0] = name; prop = prop_angles(L, keys); prev = {}
    for f in range(L + 1):
        pose(f, L, keys, prop)
        for pb in ao.pose.bones:
            q = pb.rotation_quaternion
            if pb.name in prev and q.dot(prev[pb.name]) < 0: pb.rotation_quaternion = -q
            prev[pb.name] = pb.rotation_quaternion.copy()
            pb.keyframe_insert("rotation_quaternion", frame=f); pb.keyframe_insert("location", frame=f)
    print("CLIP", name, L)
ao.animation_data.action = bpy.data.actions["Idle"]
bpy.context.scene.frame_start = 0; bpy.context.scene.frame_end = IDLE - 1
bpy.ops.wm.save_as_mainfile(filepath=W + "anim.blend")
