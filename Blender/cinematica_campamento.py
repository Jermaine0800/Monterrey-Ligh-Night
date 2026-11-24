import bpy, math, random, os, traceback
from mathutils import Vector, Matrix, Euler

PROY = r"D:\Proyectos\Monterrey Ligh Night"
A = PROY + r"\Assets"
SALIDA = r"D:\Proyectos\Cinematica_campamento"
LOG = PROY + r"\Blender\cinematica_campamento_log.txt"
RENDER = globals().get("RENDER", False)
PRUEBA = globals().get("PRUEBA", [])
FIN = 1728
random.seed(55)
def log(*a):
    with open(LOG, "a", encoding="utf-8") as f: f.write(" ".join(str(x) for x in a) + "\n")
open(LOG, "w", encoding="utf-8").write("inicio\n")

# Final: "La puerta del bucle". La reja con las 5 llaves se abre, sale la luz; Mario y Chuy te llaman desde adentro;
# volteas: La Sonriente y El Asomado se quedan mirando entre los árboles; cruzas → blanco.
vieja = bpy.data.scenes.get("Cinematica_Campamento")
if vieja:
    for o in list(vieja.objects): bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.scenes.remove(vieja)
esc = bpy.data.scenes.new("Cinematica_Campamento")
bpy.context.window.scene = esc
esc.frame_start, esc.frame_end = 1, FIN
esc.render.fps = 24
esc.render.resolution_x, esc.render.resolution_y = 640, 360
try: esc.render.engine = "BLENDER_EEVEE"
except Exception: esc.render.engine = "BLENDER_EEVEE_NEXT"
for k, v in (("taa_render_samples", 12), ("use_shadows", True)):
    try: setattr(esc.eevee, k, v)
    except Exception: pass
try: esc.render.use_motion_blur = True; esc.render.motion_blur_shutter = 0.35
except Exception: pass
esc.view_settings.view_transform = "Standard"
esc.view_settings.exposure = 0.5
esc.render.image_settings.file_format = "PNG"
esc.render.filepath = SALIDA + "\\f_"
w = bpy.data.worlds.new("Tarde_Campamento"); esc.world = w; w.use_nodes = True
bgn = w.node_tree.nodes.get("Background"); bgn.inputs[0].default_value = (0.008, 0.011, 0.024, 1)
col = bpy.data.collections.new("Campamento"); esc.collection.children.link(col)
def link(o):
    for c in list(o.users_collection): c.objects.unlink(o)
    col.objects.link(o); return o

def img(ruta):
    try: return bpy.data.images.load(ruta, check_existing=True)
    except Exception as e: log("sin imagen", ruta, e); return None

def mat_tex(nombre, ruta=None, color=(0.5, 0.5, 0.5), tinte=(1, 1, 1), vcol=False, rough=0.95, emis=0.0, alfa=False, escala=None):
    m = bpy.data.materials.new(nombre); m.use_nodes = True
    n = m.node_tree.nodes; l = m.node_tree.links
    p = n.get("Principled BSDF")
    p.inputs["Roughness"].default_value = rough
    p.inputs["Base Color"].default_value = (*color, 1)
    salida = None
    if ruta:
        t = n.new("ShaderNodeTexImage"); t.image = img(ruta); t.interpolation = "Closest"
        if escala:
            tc = n.new("ShaderNodeTexCoord"); mp = n.new("ShaderNodeMapping")
            mp.inputs["Scale"].default_value = escala
            l.new(tc.outputs["UV"], mp.inputs[0]); l.new(mp.outputs[0], t.inputs[0])
        mix = n.new("ShaderNodeMix"); mix.data_type = "RGBA"; mix.blend_type = "MULTIPLY"; mix.inputs["Factor"].default_value = 1.0
        l.new(t.outputs["Color"], mix.inputs[6]); mix.inputs[7].default_value = (*tinte, 1)
        salida = mix.outputs[2]
        if vcol:
            ca = n.new("ShaderNodeVertexColor")
            mix2 = n.new("ShaderNodeMix"); mix2.data_type = "RGBA"; mix2.blend_type = "MULTIPLY"; mix2.inputs["Factor"].default_value = 1.0
            l.new(salida, mix2.inputs[6]); l.new(ca.outputs["Color"], mix2.inputs[7]); salida = mix2.outputs[2]
        l.new(salida, p.inputs["Base Color"])
        if alfa:
            l.new(t.outputs["Alpha"], p.inputs["Alpha"])
            try: m.blend_method = "CLIP"
            except Exception: pass
    if emis > 0:
        p.inputs["Emission Strength"].default_value = emis
        if salida is not None: l.new(salida, p.inputs["Emission Color"])
        else: p.inputs["Emission Color"].default_value = (*color, 1)
    return m

def caja(nombre, loc, tam, m, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = bpy.context.active_object; o.name = nombre; o.scale = tam
    o.data.materials.append(m); return link(o)

def plano(nombre, loc, tam, m, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_plane_add(size=1, location=loc, rotation=rot)
    o = bpy.context.active_object; o.name = nombre; o.scale = (tam[0], tam[1], 1)
    o.data.materials.append(m); return link(o)


M_SUELO = mat_tex("SueloP", A + r"\Escenarios\PSXForestCamp\Textures\Ground_Forest_2m_01_Albedo.png", tinte=(0.55, 0.55, 0.5), escala=(60, 60, 1))
M_PIEDRA = mat_tex("PiedraPilar", A + r"\Escenarios\Texturas\Concreto.png", tinte=(0.45, 0.45, 0.47), escala=(2, 4, 1))
M_FIERRO = mat_tex("Fierro", None, color=(0.08, 0.07, 0.06), rough=0.4)
plano("Suelo", (0, 20, 0), (200, 200), M_SUELO)

def importar_fbx(ruta):
    antes = set(bpy.data.objects)
    try:
        bpy.ops.import_scene.fbx(filepath=ruta, use_anim=True, automatic_bone_orientation=False, ignore_leaf_bones=True)
    except Exception as e:
        log("import_scene.fbx falló, probando wm.fbx_import:", e)
        bpy.ops.wm.fbx_import(filepath=ruta)
    nuevos = [o for o in bpy.data.objects if o not in antes]
    for o in nuevos: link(o)
    return nuevos

def raices(objs):
    return [o for o in objs if o.parent is None or o.parent not in objs]

def caja_mundo(objs):
    bpy.context.view_layer.update()
    pts = []
    for o in objs:
        if o.type == "MESH":
            pts += [o.matrix_world @ Vector(c) for c in o.bound_box]
    if not pts: return Vector((0, 0, 0)), Vector((0, 0, 1))
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx

try:
    pino = importar_fbx(A + r"\Escenarios\PSXForestCamp\FBX\Tree_Pine_01.fbx")
    mp = mat_tex("Pino", A + r"\Escenarios\PSXForestCamp\Textures\Tree_Pine_01_Albedo.png", tinte=(0.6, 0.65, 0.6), alfa=False)
    mallas = [o for o in pino if o.type == "MESH"]
    for o in mallas:
        o.data.materials.clear(); o.data.materials.append(mp)
    piv = bpy.data.objects.new("PinoBase", None); link(piv)
    for r in raices(pino): r.parent = piv
    mn, mx = caja_mundo(pino)
    alto = max(0.01, mx.z - mn.z)
    piv.scale = (9 / alto,) * 3
    bpy.context.view_layer.update()
    puestos = 0
    for i in range(400):
        x = random.uniform(-45, 45); y = random.uniform(-35, 45)
        if (x * x + (y - 2) ** 2) < 12 ** 2 or (abs(x) < 2.5 and y < -2): continue
        if puestos >= 170: break
        for m0 in mallas:
            c = m0.copy(); c.parent = None
            c.matrix_world = Matrix.Translation((x, y, 0)) @ Matrix.Rotation(random.uniform(0, 6.28), 4, "Z") @ Matrix.Scale(random.uniform(0.8, 1.4), 4) @ m0.matrix_world
            col.objects.link(c)
        puestos += 1
    piv.location.z = -200   # el original se esconde
    log("pinos", puestos, "mallas", [(m.name, tuple(round(v,2) for v in m.dimensions), tuple(round(v,1) for v in m.matrix_world.translation)) for m in mallas], "alto", alto)
except Exception:
    log("ERROR pinos", traceback.format_exc())

def accion(nombre):
    nombre = nombre.lower()
    for a in bpy.data.actions:
        b = a.name.split("|")[-1].split(".")[0].lower()
        if b == nombre: return a
    for a in bpy.data.actions:
        if nombre in a.name.lower(): return a
    log("no hay accion", nombre, [a.name for a in bpy.data.actions][:30])
    return None

ACCIONES = {}
def personaje(nombre, ruta_cuerpo, piezas, tex, tinte, alto, vcol):
    antes_a = set(bpy.data.actions)
    objs = importar_fbx(ruta_cuerpo)
    ACCIONES[nombre] = {a.name.split("|")[-1].split(".")[0].lower(): a for a in bpy.data.actions if a not in antes_a}
    log("acciones", nombre, list(ACCIONES[nombre].keys()))
    arm = next((o for o in objs if o.type == "ARMATURE"), None)
    m = mat_tex("Mat_" + nombre, tex, tinte=tinte, vcol=vcol)
    for o in objs:
        if o.type == "MESH":
            o.data.materials.clear(); o.data.materials.append(m)
    # Ropa: cada pieza trae su propio esqueleto igual; se pasa al del cuerpo
    for p in piezas:
        po = importar_fbx(p)
        for o in po:
            if o.type == "MESH":
                o.data.materials.clear(); o.data.materials.append(m)
                mw = o.matrix_world.copy(); o.parent = arm; o.matrix_world = mw
                for md in o.modifiers:
                    if md.type == "ARMATURE": md.object = arm
        mallas_p = [o for o in po if o.type == "MESH"]
        for o in po:
            if o.type in ("ARMATURE", "EMPTY") and o is not arm:
                bpy.data.objects.remove(o, do_unlink=True)
        objs += mallas_p
    raiz = bpy.data.objects.new(nombre, None); link(raiz)
    ajuste = bpy.data.objects.new(nombre + "_ajuste", None); link(ajuste); ajuste.parent = raiz
    for r in raices(objs):
        r.parent = ajuste
    if arm and arm.animation_data: arm.animation_data.action = None
    mn, mx = caja_mundo(objs)
    h = max(0.01, mx.z - mn.z)
    ajuste.scale = (alto / h,) * 3
    ajuste.rotation_euler.z = math.pi      # los modelos miran a -Y; así "raiz" mira a +Y
    bpy.context.view_layer.update()
    mn, mx = caja_mundo(objs)
    ajuste.location.z -= mn.z
    log("personaje", nombre, "alto", round(h, 3), "arm", arm.name if arm else None)
    return raiz, arm

def animar(arm, segmentos, quien=None):
    """segmentos: [(accion, inicio, fin)] — cada uno en su pista NLA, encima del anterior."""
    if arm is None: return
    ad = arm.animation_data_create(); ad.action = None
    for i, (nom, a0, a1) in enumerate(segmentos):
        act = ACCIONES.get(quien, {}).get(nom) if quien else None
        if act is None: act = accion(nom)
        if act is None: continue
        tr = ad.nla_tracks.new(); tr.name = "%02d_%s" % (i, nom)
        st = tr.strips.new(nom, int(a0), act)
        try:
            if getattr(act, "slots", None) and len(act.slots) and hasattr(st, "action_slot"): st.action_slot = act.slots[0]
        except Exception as e: log("slot", e)
        largo = max(1.0, st.action_frame_end - st.action_frame_start)
        st.repeat = max(0.05, (a1 - a0) / largo)
        st.extrapolation = "HOLD" if i == 0 else "HOLD_FORWARD"
        st.blend_in = 0 if i == 0 else 3; st.use_auto_blend = False

def llaves(o, puntos, prop="location", interp="LINEAR"):
    for f, v in puntos:
        setattr(o, prop, v); o.keyframe_insert(data_path=prop, frame=f)
    if o.animation_data and o.animation_data.action:
        try:
            for fc in o.animation_data.action.fcurves:
                for k in fc.keyframe_points: k.interpolation = interp
        except Exception:
            pass

def interp_const(o):
    try:
        for fc in o.animation_data.action.fcurves:
            for k in fc.keyframe_points: k.interpolation = "CONSTANT"
    except Exception:
        # acciones por capas (Blender 4.4+)
        try:
            for ly in o.animation_data.action.layers:
                for sp in ly.strips:
                    for cb in sp.channelbags:
                        for fc in cb.fcurves:
                            for k in fc.keyframe_points: k.interpolation = "CONSTANT"
        except Exception as e: log("interp", e)

def fcurvas(o):
    a = o.animation_data.action
    try:
        return list(a.fcurves)
    except Exception:
        res = []
        for ly in a.layers:
            for sp in ly.strips:
                for cb in sp.channelbags: res += list(cb.fcurves)
        return res



# ------------------------------------------------------------------ TARDE EN EL CAMPAMENTO (prólogo)
# Hernán Camarena (tú, el practicante, con la videocámara) llega al campamento base de la cuadrilla.
# Mario (jefe) y Chuy lo reciben junto a la fogata; Chuy cuenta la leyenda de la Línea K;
# el Asomado se deja ver entre los árboles (solo la cámara lo ve) y se hace de noche.
F = 24
def s2f(t): return int(round(t * F)) + 1
CAMP = A + r"\Escenarios\PSXForestCamp"
def prop(nombre, pos, giro=0.0, escala=1.0):
    objs = importar_fbx(CAMP + r"\FBX\%s.fbx" % nombre)
    m = mat_tex("Camp_" + nombre, CAMP + r"\Textures\%s_Albedo.png" % nombre, tinte=(0.9, 0.85, 0.8))
    for o in objs:
        if o.type == "MESH": o.data.materials.clear(); o.data.materials.append(m)
    piv = bpy.data.objects.new("Prop_" + nombre, None); link(piv)
    for r in raices(objs): r.parent = piv
    piv.location = pos; piv.rotation_euler.z = math.radians(giro); piv.scale = (escala,) * 3
    return piv
try:
    prop("Campfire_01", (0, 2, 0))
    prop("Log_Seat_01", (-1.7, 2.6, 0), 80)
    prop("Log_Seat_01", (1.8, 2.3, 0), -75)
    prop("Tent_01", (-4.5, 6.5, 0), 200)
    prop("Tent_01", (4.2, 7.2, 0), 160)
    prop("Picnic_Table_01", (5.2, 0.5, 0), 90)
    prop("Cooler_01", (2.6, 4.6, 0), 30)
    prop("Firewood_Pile_01", (-3.2, 3.4, 0), 10)
    prop("Lantern_01", (5.2, 0.5, 0.78))
    prop("Sign_Trail_01", (-1.8, -6.5, 0), 20)
    prop("Stump_Axe_01", (-5.5, 0.5, 0), 0)
    log("props ok")
except Exception:
    log("ERROR props", traceback.format_exc())

# Fogata (parpadea) y el sol que se mete
fl = bpy.data.lights.new("Fogata", "POINT"); fl.color = (1.0, 0.55, 0.22); fl.shadow_soft_size = 0.3
flo = bpy.data.objects.new("Fogata", fl); link(flo); flo.location = (0, 2, 0.6)
rnd = random.Random(3)
for f in range(1, 1729, 4):
    base_e = 90 if f < s2f(62) else 90 + (f - s2f(62)) * 0.35
    fl.energy = base_e * rnd.uniform(0.75, 1.15); fl.keyframe_insert("energy", frame=f)
sol = bpy.data.lights.new("Sol", "SUN"); so = bpy.data.objects.new("Sol", sol); link(so)
so.rotation_euler = (math.radians(78), 0, math.radians(-60))
for t, e, c in ((0, 3.0, (1.0, 0.62, 0.35)), (40, 2.2, (1.0, 0.5, 0.3)), (62, 1.2, (0.9, 0.4, 0.3)), (68, 0.15, (0.5, 0.55, 0.9)), (72, 0.12, (0.5, 0.55, 0.9))):
    sol.energy = e; sol.color = c; sol.keyframe_insert("energy", frame=s2f(t)); sol.keyframe_insert("color", frame=s2f(t))
for t, c in ((0, (0.55, 0.28, 0.14, 1)), (40, (0.42, 0.2, 0.12, 1)), (62, (0.2, 0.1, 0.1, 1)), (68, (0.012, 0.014, 0.03, 1)), (72, (0.008, 0.011, 0.024, 1))):
    bgn.inputs[0].default_value = c; bgn.inputs[0].keyframe_insert("default_value", frame=s2f(t))

# ------------------------------------------------------------------ Mario y Chuy (y el Asomado entre los árboles)
PSX = A + r"\Characters\Protagonista\PSXCreator"; ATLAS = PSX + r"\atlas.png"
try:
    mario, arm_m = personaje("Mario", PSX + r"\man_2.fbx", [PSX + r"\man_jacket.fbx", PSX + r"\man_jeans.fbx", PSX + r"\man_boots.fbx", PSX + r"\man_cap.fbx"], ATLAS, (0.62, 0.5, 0.38), 1.78, True)
    chuy, arm_c = personaje("Chuy", PSX + r"\man_2.fbx", [PSX + r"\man_tee.fbx", PSX + r"\man_jeans.fbx", PSX + r"\man_boots.fbx", PSX + r"\man_hair_short.fbx"], ATLAS, (0.45, 0.52, 0.62), 1.72, True)
    # Mario: sentado en el tronco, se para a saludarte y se queda de pie
    llaves(mario, [(1, (-1.75, 2.6, 0)), (s2f(10.2), (-1.75, 2.6, 0)), (s2f(10.6), (-1.6, 1.4, 0)), (s2f(56.6), (-1.6, 1.4, 0)), (s2f(57.4), (-1.5, 1.4, 0))])
    llaves(mario, [(1, (0, 0, math.radians(-90))), (s2f(10.2), (0, 0, math.radians(-90))), (s2f(10.8), (0, 0, math.radians(200))),
                   (s2f(56.6), (0, 0, math.radians(200))), (s2f(57.6), (0, 0, math.radians(300))), (s2f(61.6), (0, 0, math.radians(300))), (s2f(62.4), (0, 0, math.radians(200)))], prop="rotation_euler")
    animar(arm_m, quien="Mario", segmentos=[("sit", 1, s2f(10.3)), ("wave", s2f(10.3), s2f(12.6)), ("talk", s2f(12.6), s2f(15.3)), ("idle", s2f(15.3), s2f(20.3)),
           ("talk", s2f(20.3), s2f(24.5)), ("idle", s2f(24.5), s2f(32.3)), ("talk", s2f(32.3), s2f(36.0)), ("idle", s2f(36.0), s2f(46.0)),
           ("talk", s2f(46.0), s2f(52.0)), ("idle", s2f(52.0), s2f(61.8)), ("talk", s2f(61.8), s2f(66.5)), ("idle", s2f(66.5), 1800)])
    # Chuy: sentado del otro lado de la fogata todo el tiempo
    llaves(chuy, [(1, (1.85, 2.3, 0))])
    llaves(chuy, [(1, (0, 0, math.radians(95))), (s2f(27.4), (0, 0, math.radians(95))), (s2f(28.2), (0, 0, math.radians(150))),
                  (s2f(56.6), (0, 0, math.radians(150))), (s2f(57.4), (0, 0, math.radians(321))), (s2f(61.6), (0, 0, math.radians(321))), (s2f(62.4), (0, 0, math.radians(120)))], prop="rotation_euler")
    animar(arm_c, quien="Chuy", segmentos=[("sit", 1, 1800)])
    # Linternas: se prenden cuando oscurece
    for p, retraso in ((mario, 66.0), (chuy, 67.0)):
        l = bpy.data.lights.new("Linterna", "SPOT"); l.spot_size = math.radians(38); l.spot_blend = 0.35; l.color = (1, 0.92, 0.75)
        o = bpy.data.objects.new("Linterna_" + p.name, l); link(o); o.parent = p; o.location = (0.3, 0.35, 1.0 if p is chuy else 1.25)
        o.rotation_euler = (math.radians(80), 0, 0)
        for t, e in ((0, 0), (retraso, 0), (retraso + 0.1, 450)):
            l.energy = e; l.keyframe_insert("energy", frame=s2f(t))
    asom, arm_a = personaje("El_Asomado", A + r"\Characters\Monstruos\DesertCryptids\DC_Mimic.fbx", [], A + r"\Characters\Monstruos\DesertCryptids\DC_Mimic_baked.png", (0.85, 0.82, 0.78), 2.35, False)
    llaves(asom, [(1, (0, 200, -60)), (s2f(57.6), (7.95, 9.55, 0)), (s2f(60.6), (0, 200, -60))], interp="CONSTANT")
    interp_const(asom)
    asom.rotation_euler = (0, math.radians(-18), math.radians(145))     # asomado de lado detrás del tronco
    animar(arm_a, quien="El_Asomado", segmentos=[("idle", 1, 1800)])
    # el tronco detrás del que se asoma
    tronco = caja("TroncoAsomado", (7.6, 10.0, 3.0), (0.7, 0.7, 6.0), mat_tex("Corteza", None, color=(0.12, 0.08, 0.05)))
    log("personajes ok")
except Exception:
    log("ERROR personajes", traceback.format_exc())

# ------------------------------------------------------------------ cámara: los ojos de Hernán (videocámara en mano)
cd = bpy.data.cameras.new("VideocamaraC"); cd.lens = 26; cd.clip_start = 0.05
cam = bpy.data.objects.new("VideocamaraC", cd); link(cam); esc.camera = cam
def mirar(t, loc, objetivo, roll=0.0):
    d = Vector(objetivo) - Vector(loc)
    yaw = math.degrees(math.atan2(-d.x, d.y)); pitch = math.degrees(math.atan2(d.z, math.hypot(d.x, d.y)))
    mtx = Matrix.Rotation(math.radians(yaw), 4, "Z") @ Matrix.Rotation(math.radians(90 + pitch), 4, "X") @ Matrix.Rotation(math.radians(roll), 4, "Z")
    cam.location = loc; cam.rotation_euler = mtx.to_euler("XYZ", cam.rotation_euler)
    cam.keyframe_insert("location", frame=s2f(t)); cam.keyframe_insert("rotation_euler", frame=s2f(t))
P = (0.2, -1.8, 1.62)         # donde se para Hernán
MARIO_C = (-1.6, 1.4, 1.62); MARIO_S = (-1.75, 2.6, 1.0); CHUY = (1.85, 2.3, 1.0); FUEGO = (0, 2, 0.4); ARBOL = (7.8, 9.7, 1.9)
mirar(0, (0.0, -13.0, 1.62), (0, 2, 1.2))
mirar(4.0, (0.1, -8.0, 1.62), (0, 2, 1.0))
mirar(9.5, P, MARIO_S)
mirar(10.6, P, (-1.6, 1.4, 1.5))
mirar(15.4, P, MARIO_C)
mirar(16.0, P, CHUY)
mirar(20.0, P, CHUY)
mirar(20.6, P, MARIO_C)
mirar(24.6, (0.2, -1.7, 1.5), (0.2, -0.6, 0.6), 4)      # mira la videocámara en sus manos
mirar(27.2, P, MARIO_C)
mirar(27.9, P, CHUY)
mirar(32.0, (0.35, -1.2, 1.55), CHUY)                     # se acerca un poco
mirar(32.6, (0.35, -1.2, 1.55), MARIO_C)
mirar(36.0, (0.35, -1.2, 1.55), MARIO_C)
mirar(36.6, (0.35, -1.2, 1.55), CHUY)
mirar(43.6, (0.35, -1.2, 1.55), FUEGO)
mirar(46.2, (0.35, -1.2, 1.55), MARIO_C)
mirar(52.2, (0.35, -1.2, 1.55), MARIO_C)
mirar(52.8, (0.35, -1.2, 1.55), CHUY)
mirar(56.6, (0.35, -1.2, 1.55), CHUY)
mirar(57.6, (0.35, -1.2, 1.6), ARBOL)                    # un grito lejos... todos voltean
mirar(60.4, (0.4, -1.1, 1.6), ARBOL, -3)
mirar(61.4, (0.35, -1.2, 1.58), MARIO_C)
mirar(66.0, (0.35, -1.2, 1.58), MARIO_C)
mirar(70.0, (0.3, -1.4, 1.58), (0.0, 30.0, 6.0))         # el cielo se oscurece
mirar(72.0, (0.3, -1.4, 1.58), (0.0, 30.0, 8.0))
for fc in fcurvas(cam):
    n1 = fc.modifiers.new("NOISE"); n1.scale = 16; n1.strength = 0.022 if "rotation" in fc.data_path else 0.018
for t, ln in ((0, 26), (57.4, 26), (58.6, 48), (60.6, 48), (61.4, 26)):
    cd.lens = ln; cd.keyframe_insert("lens", frame=s2f(t))
lr = bpy.data.lights.new("LuzAsomado", "POINT"); lr.energy = 0; lr.color = (0.7, 0.8, 1.0)
lro = bpy.data.objects.new("LuzAsomado", lr); link(lro); lro.location = (6.8, 8.6, 2.4)
for t, e in ((0, 0), (57.5, 0), (57.6, 60), (60.5, 60), (60.6, 0)):
    lr.energy = e; lr.keyframe_insert("energy", frame=s2f(t))
lc = bpy.data.lights.new("LuzCamC", "SPOT"); lc.energy = 0; lc.spot_size = math.radians(55)
lco = bpy.data.objects.new("LuzCamC", lc); link(lco); lco.parent = cam
for t, e in ((0, 0), (66.5, 0), (66.6, 25)):
    lc.energy = e; lc.keyframe_insert("energy", frame=s2f(t))
# fundido a negro al final
for t, e in ((0, 0.6), (70.5, 0.6), (71.8, -6.0)):
    esc.view_settings.exposure = e; esc.view_settings.keyframe_insert("exposure", frame=s2f(t))
log("camara ok")

try: bpy.ops.wm.save_as_mainfile(filepath=PROY + r"\Blender\Cinematica_Campamento.blend", copy=True)
except Exception as e: log("no blend", e)
os.makedirs(SALIDA, exist_ok=True)
for f in PRUEBA:
    esc.frame_set(f); esc.render.filepath = SALIDA + "\\prueba_%04d.png" % f
    bpy.ops.render.render(write_still=True)
esc.render.filepath = SALIDA + "\\f_"
if RENDER:
    if globals().get("RANGO"): esc.frame_start, esc.frame_end = RANGO
    bpy.ops.render.render(animation=True); log("render listo")
log("fin")
