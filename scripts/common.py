import bpy, math, bmesh, numpy as np
W = "/private/tmp/claude-501/-Users-darshilbansal/dff3e1f5-ed21-426d-8fff-8d00ad6b92c5/scratchpad/dino/"
YAW = 20.6   # source stance is turned: line the two tanks up on X so the belly faces -Y
def load():
    """Tripo FBX -> mesh facing -Y, tank bottoms on z=0, centred"""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=W + "src.fbx")
    o = [x for x in bpy.data.objects if x.type == "MESH"][0]
    o.rotation_euler.z -= math.radians(90 + YAW); bpy.context.view_layer.update()
    me = o.data; M = o.matrix_world.copy()
    for v in me.vertices: v.co = M @ v.co
    o.matrix_world = M.Identity(4)
    co = np.array([v.co[:] for v in me.vertices])
    sh = np.array([-(co[:, 0].min() + co[:, 0].max()) / 2, -(co[:, 1].min() + co[:, 1].max()) / 2, -co[:, 2].min()])
    for v, c in zip(me.vertices, co + sh): v.co = c
    me.update()
    return o
def parts(o):
    bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table()
    lab = [-1] * len(bm.verts); comps = []
    for v in bm.verts:
        if lab[v.index] >= 0: continue
        st = [v]; comp = []; lab[v.index] = len(comps)
        while st:
            x = st.pop(); comp.append(x.index)
            for e in x.link_edges:
                y = e.other_vert(x)
                if lab[y.index] < 0: lab[y.index] = len(comps); st.append(y)
        comps.append(comp)
    return [np.array(c) for c in sorted(comps, key=len, reverse=True)]
