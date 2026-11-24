import bpy, math, random, os, traceback
from mathutils import Vector, Matrix, Euler

PROY = r"D:\Proyectos\Monterrey Ligh Night"
A = PROY + r"\Assets"
SALIDA = r"D:\Proyectos\Cinematica_portal"
LOG = PROY + r"\Blender\cinematica_portal_log.txt"
RENDER = globals().get("RENDER", False)
PRUEBA = globals().get("PRUEBA", [])
FIN = 720
random.seed(55)
def log(*a):
    with open(LOG, "a", encoding="utf-8") as f: f.write(" ".join(str(x) for x in a) + "\n")
open(LOG, "w", encoding="utf-8").write("inicio\n")

# Final: "La puerta del bucle". La reja con las 5 llaves se abre, sale la luz; Mario y Chuy te llaman desde adentro;
# volteas: La Sonriente y El Asomado se quedan mirando entre los árboles; cruzas → blanco.
vieja = bpy.data.scenes.get("Cinematica_Portal")
if vieja:
    for o in list(vieja.objects): bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.scenes.remove(vieja)
esc = bpy.data.scenes.new("Cinematica_Portal")
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
w = bpy.data.worlds.new("Noche_Portal"); esc.world = w; w.use_nodes = True
bgn = w.node_tree.nodes.get("Background"); bgn.inputs[0].default_value = (0.008, 0.011, 0.024, 1)
col = bpy.data.collections.new("Portal"); esc.collection.children.link(col)
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
        x = random.uniform(-45, 45); y = random.uniform(-30, 45)
        if (x * x + (y - 8) ** 2) < 14 ** 2: continue
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


# ------------------------------------------------------------------ la puerta del bucle (y = 14)
try:
    caja("PilarI", (-2.2, 14, 1.9), (0.8, 0.8, 3.8), M_PIEDRA)
    caja("PilarD", (2.2, 14, 1.9), (0.8, 0.8, 3.8), M_PIEDRA)
    caja("Dintel", (0, 14, 3.95), (5.2, 0.8, 0.4), M_PIEDRA)
    hojas = []
    for lado in (-1, 1):
        bis = bpy.data.objects.new("Bisagra%d" % lado, None); link(bis); bis.location = (lado * 1.8, 13.95, 0)
        for i in range(7):
            b = caja("Barrote", (lado * 1.8 - lado * (0.13 + i * 0.26), 13.95, 1.75), (0.05, 0.05, 3.4), M_FIERRO); b.parent = bis
            b.location = (-lado * (0.13 + i * 0.26), 0, 1.75)
        for z in (0.4, 3.2):
            h = caja("Travesano", (0, 0, 0), (1.8, 0.07, 0.07), M_FIERRO); h.parent = bis; h.location = (-lado * 0.9, 0, z)
        hojas.append((bis, lado))
    # Las 5 cerraduras con las llaves: se encienden una por una
    cerraduras = []
    for i in range(5):
        m = mat_tex("Cerradura%d" % i, None, color=(1.0, 0.78, 0.4), emis=0.0)
        c = caja("Cerradura%d" % i, (-0.9 + i * 0.45, 13.85, 1.9), (0.16, 0.08, 0.22), m)
        bis_c = hojas[0][0] if (-0.9 + i * 0.45) < 0 else hojas[1][0]
        bpy.context.view_layer.update(); mw = c.matrix_world.copy(); c.parent = bis_c; c.matrix_world = mw
        p = m.node_tree.nodes.get("Principled BSDF")
        p.inputs["Emission Color"].default_value = (1.0, 0.75, 0.35, 1)
        for f, e in ((1, 0.0), (35 + i * 22, 0.0), (40 + i * 22, 25.0), (150, 25.0), (170, 0.0)):
            p.inputs["Emission Strength"].default_value = e; p.inputs["Emission Strength"].keyframe_insert("default_value", frame=f)
        lz = bpy.data.lights.new("LuzCerr%d" % i, "POINT"); lz.color = (1, 0.75, 0.4); lz.shadow_soft_size = 0.05
        lo = bpy.data.objects.new("LuzCerr%d" % i, lz); link(lo); lo.location = (-0.9 + i * 0.45, 13.5, 1.9)
        for f, e in ((1, 0), (35 + i * 22, 0), (40 + i * 22, 40), (150, 40), (170, 0)):
            lz.energy = e; lz.keyframe_insert("energy", frame=f)
    # La reja se abre (165 → 230)
    for bis, lado in hojas:
        llaves(bis, [(1, (0, 0, 0)), (165, (0, 0, 0)), (230, (0, 0, math.radians(-105 * lado)))], prop="rotation_euler", interp="BEZIER")
    # El portal: un plano de luz con ruido que gira, detrás de la reja
    mp = bpy.data.materials.new("PortalLuz"); mp.use_nodes = True
    n = mp.node_tree.nodes; l = mp.node_tree.links; n.clear()
    o = n.new("ShaderNodeOutputMaterial"); em = n.new("ShaderNodeEmission")
    tc = n.new("ShaderNodeTexCoord"); mpg = n.new("ShaderNodeMapping"); nz = n.new("ShaderNodeTexNoise"); nz.inputs["Scale"].default_value = 3.5; nz.inputs["Detail"].default_value = 6
    rp = n.new("ShaderNodeValToRGB"); rp.color_ramp.elements[0].color = (0.12, 0.01, 0.02, 1); rp.color_ramp.elements[1].color = (1.0, 0.95, 0.85, 1)
    l.new(tc.outputs["Generated"], mpg.inputs[0]); l.new(mpg.outputs[0], nz.inputs[0]); l.new(nz.outputs["Fac"], rp.inputs[0]); l.new(rp.outputs[0], em.inputs[0]); l.new(em.outputs[0], o.inputs["Surface"])
    for f, z in ((1, 0.0), (720, 8.0)):
        mpg.inputs["Rotation"].default_value[2] = z; mpg.inputs["Rotation"].keyframe_insert("default_value", index=2, frame=f)
    for f, e in ((1, 0.0), (170, 0.0), (215, 0.9), (560, 1.2), (640, 3.0), (700, 25.0)):
        em.inputs["Strength"].default_value = e; em.inputs["Strength"].keyframe_insert("default_value", frame=f)
    plano("Portal", (0, 21.0, 3.0), (9.0, 7.0), mp, rot=(math.radians(90), 0, 0))
    luzp = bpy.data.lights.new("LuzPortal", "AREA"); luzp.shape = "RECTANGLE"; luzp.size = 3.4; luzp.size_y = 3.6; luzp.color = (1.0, 0.85, 0.75)
    lpo = bpy.data.objects.new("LuzPortal", luzp); link(lpo); lpo.location = (0, 20.5, 2.2); lpo.rotation_euler = (math.radians(-90), 0, 0)
    for f, e in ((1, 0), (170, 0), (215, 600), (560, 900), (640, 2500), (700, 12000)):
        luzp.energy = e; luzp.keyframe_insert("energy", frame=f)
    log("puerta ok")
except Exception:
    log("ERROR puerta", traceback.format_exc())

# Luna
sol = bpy.data.lights.new("Luna", "SUN"); sol.energy = 0.35; sol.color = (0.55, 0.65, 1.0)
so = bpy.data.objects.new("Luna", sol); link(so); so.rotation_euler = (math.radians(55), 0, math.radians(30))

# ------------------------------------------------------------------ Mario y Chuy dentro de la luz; los monstruos atrás
PSX = A + r"\Characters\Protagonista\PSXCreator"; ATLAS = PSX + r"\atlas.png"
try:
    mario, arm_m = personaje("Mario", PSX + r"\man_2.fbx", [PSX + r"\man_jacket.fbx", PSX + r"\man_jeans.fbx", PSX + r"\man_boots.fbx", PSX + r"\man_cap.fbx"], ATLAS, (0.62, 0.5, 0.38), 1.78, True)
    chuy, arm_c = personaje("Chuy", PSX + r"\man_2.fbx", [PSX + r"\man_tee.fbx", PSX + r"\man_jeans.fbx", PSX + r"\man_boots.fbx", PSX + r"\man_hair_short.fbx"], ATLAS, (0.45, 0.52, 0.62), 1.72, True)
    for p in (mario, chuy): p.rotation_euler.z = math.pi      # miran hacia la cámara
    llaves(mario, [(1, (-0.6, 60, -50)), (300, (-0.7, 16.2, 0))], interp="CONSTANT")
    llaves(chuy, [(1, (0.7, 60, -50)), (300, (0.9, 16.8, 0))], interp="CONSTANT")
    animar(arm_m, quien="Mario", segmentos=[("idle", 1, 330), ("wave", 330, 470), ("idle", 470, 720)])
    animar(arm_c, quien="Chuy", segmentos=[("idle", 1, 720)])
    sonr, arm_s = personaje("La_Sonriente", A + r"\Characters\Monstruos\LowbitLab\hooded_figure.fbx", [], A + r"\Characters\Monstruos\LowbitLab\hooded_figure_atlas.png", (0.3, 0.3, 0.33), 1.95, False)
    mc = mat_tex("CaraSonrienteP", A + r"\Characters\Monstruos\CaraSonriente.png", alfa=True, emis=0.6, tinte=(0.75, 0.75, 0.72))
    cara = plano("Cara", (0, 0, 0), (0.34, 0.34), mc, rot=(math.radians(90), 0, 0)); cara.parent = sonr; cara.location = (0, 0.17, 1.72)
    asom, arm_a = personaje("El_Asomado", A + r"\Characters\Monstruos\DesertCryptids\DC_Mimic.fbx", [], A + r"\Characters\Monstruos\DesertCryptids\DC_Mimic_baked.png", (0.85, 0.82, 0.78), 2.35, False)
    sonr.location = (-2.6, 3.5, 0); asom.location = (3.4, 2.0, 0)     # detrás de ti, entre los árboles
    sonr.rotation_euler.z = 0; asom.rotation_euler.z = 0             # miran hacia la puerta (+Y)
    lr = bpy.data.lights.new("LuzRoja", "POINT"); lr.energy = 120; lr.color = (1.0, 0.15, 0.1)
    lro = bpy.data.objects.new("LuzRoja", lr); link(lro); lro.location = (0.4, 1.5, 3.0)
    animar(arm_s, quien="La_Sonriente", segmentos=[("idle", 1, 720)])
    animar(arm_a, quien="El_Asomado", segmentos=[("idle", 1, 720)])
    # Se desvanecen hundiéndose cuando vuelves a ver la puerta
    llaves(sonr, [(1, (-2.6, 3.5, 0)), (560, (-2.6, 3.5, 0)), (600, (-2.6, 3.5, -2.4))])
    llaves(asom, [(1, (3.4, 2.0, 0)), (560, (3.4, 2.0, 0)), (600, (3.4, 2.0, -2.8))])
    log("personajes ok")
except Exception:
    log("ERROR personajes", traceback.format_exc())

# ------------------------------------------------------------------ cámara
cd = bpy.data.cameras.new("VideocamaraP"); cd.lens = 24; cd.clip_start = 0.05
cam = bpy.data.objects.new("VideocamaraP", cd); link(cam); esc.camera = cam
def ckey(f, loc, yaw=0, pitch=0, roll=0):
    mtx = Matrix.Rotation(math.radians(yaw), 4, "Z") @ Matrix.Rotation(math.radians(90 + pitch), 4, "X") @ Matrix.Rotation(math.radians(roll), 4, "Z")
    cam.location = loc; cam.rotation_euler = mtx.to_euler("XYZ", cam.rotation_euler)
    cam.keyframe_insert("location", frame=f); cam.keyframe_insert("rotation_euler", frame=f)
ckey(1, (0.3, 2.0, 1.6), 3, -2)
ckey(140, (0.1, 9.0, 1.6), 0, 2)
ckey(170, (0.0, 9.6, 1.6), 0, 0)
ckey(300, (0.0, 10.4, 1.6), 0, 1)
ckey(470, (0.0, 11.0, 1.6), 0, 0)
ckey(490, (0.0, 11.0, 1.6), 0, 0)
ckey(520, (0.1, 10.9, 1.6), 175, -3)     # volteas: ahí están, mirando
ckey(560, (0.1, 10.9, 1.6), 182, -2)
ckey(585, (0.0, 11.0, 1.6), 360, 0)      # regresas a la puerta
ckey(700, (0.0, 14.2, 1.6), 360, 2)      # cruzas
ckey(720, (0.0, 14.8, 1.6), 360, 4)
for fc in fcurvas(cam):
    n1 = fc.modifiers.new("NOISE"); n1.scale = 14; n1.strength = 0.03 if "rotation" in fc.data_path else 0.025
lc = bpy.data.lights.new("LuzCamP", "SPOT"); lc.energy = 25; lc.spot_size = math.radians(55); lc.spot_blend = 0.6
lco = bpy.data.objects.new("LuzCamP", lc); link(lco); lco.parent = cam
# Blanco al final
for f, e in ((1, 0.5), (630, 0.5), (680, 3.0), (705, 8.0), (720, 10.0)):
    esc.view_settings.exposure = e; esc.view_settings.keyframe_insert("exposure", frame=f)
log("camara ok")

try:
    bpy.ops.wm.save_as_mainfile(filepath=PROY + r"\Blender\Cinematica_Portal.blend", copy=True)
except Exception as e: log("no blend", e)
os.makedirs(SALIDA, exist_ok=True)
for f in PRUEBA:
    esc.frame_set(f); esc.render.filepath = SALIDA + "\\prueba_%04d.png" % f
    bpy.ops.render.render(write_still=True)
esc.render.filepath = SALIDA + "\\f_"
if RENDER:
    bpy.ops.render.render(animation=True); log("render listo")
log("fin")
