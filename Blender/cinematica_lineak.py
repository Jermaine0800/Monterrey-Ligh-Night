# Cinemática de inicio: "LÍNEA K" (found footage VHS, 1998)
# Se corre dentro de Blender 5.x:  exec(open(r"D:\Proyectos\Monterrey Ligh Night\Blender\cinematica_lineak.py", encoding="utf-8").read())
# Arma la escena en una escena NUEVA llamada "Cinematica_LineaK" (no toca lo demás), la guarda en un .blend
# y, si RENDER = True, renderiza los cuadros PNG en D:\Proyectos\Cinematica_frames\
import bpy, math, random, os, traceback
from mathutils import Vector, Matrix, Euler

PROY = r"D:\Proyectos\Monterrey Ligh Night"
A = PROY + r"\Assets"
SALIDA = r"D:\Proyectos\Cinematica_frames"
LOG = PROY + r"\Blender\cinematica_log.txt"
RENDER = globals().get("RENDER", False)
PRUEBA = globals().get("PRUEBA", [])         # cuadros sueltos para revisar, p. ej. [100, 520, 615, 760, 860]
FIN = 885
random.seed(1127)

def log(*a):
    with open(LOG, "a", encoding="utf-8") as f:
        f.write(" ".join(str(x) for x in a) + "\n")

open(LOG, "w", encoding="utf-8").write("inicio\n")

# ------------------------------------------------------------------ escena nueva
vieja = bpy.data.scenes.get("Cinematica_LineaK")
if vieja:
    for o in list(vieja.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.scenes.remove(vieja)
esc = bpy.data.scenes.new("Cinematica_LineaK")
bpy.context.window.scene = esc
esc.frame_start, esc.frame_end = 1, FIN
esc.render.fps = 24
esc.render.resolution_x, esc.render.resolution_y = 640, 360
esc.render.resolution_percentage = 100
try:
    esc.render.engine = "BLENDER_EEVEE"
except Exception:
    esc.render.engine = "BLENDER_EEVEE_NEXT"
ee = esc.eevee
for k, v in (("taa_render_samples", 12), ("use_volumetric_shadows", False), ("volumetric_tile_size", "4"),
             ("volumetric_start", 0.1), ("volumetric_end", 70.0), ("use_shadows", True)):
    try: setattr(ee, k, v)
    except Exception: pass
try: esc.render.use_motion_blur = True; esc.render.motion_blur_shutter = 0.35
except Exception: pass
esc.view_settings.view_transform = "Standard"
esc.view_settings.look = "None"
esc.view_settings.exposure = 0.5
esc.render.image_settings.file_format = "PNG"
esc.render.filepath = SALIDA + "\\f_"
esc.render.film_transparent = False

# Mundo: noche azul con niebla
w = bpy.data.worlds.new("Noche_LineaK"); esc.world = w
w.use_nodes = True
nt = w.node_tree; nt.nodes.clear()
bg = nt.nodes.new("ShaderNodeBackground"); bg.inputs[0].default_value = (0.008, 0.011, 0.024, 1); bg.inputs[1].default_value = 1.0
vol = nt.nodes.new("ShaderNodeVolumePrincipled"); vol.inputs["Density"].default_value = 0.007
vol.inputs["Color"].default_value = (0.35, 0.4, 0.5, 1)
out = nt.nodes.new("ShaderNodeOutputWorld")
nt.links.new(bg.outputs[0], out.inputs["Surface"])
if globals().get("NIEBLA", False): nt.links.new(vol.outputs[0], out.inputs["Volume"])

col = bpy.data.collections.new("LineaK"); esc.collection.children.link(col)
def link(o):
    for c in list(o.users_collection): c.objects.unlink(o)
    col.objects.link(o)
    return o

# ------------------------------------------------------------------ materiales
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

M_SUELO = mat_tex("Suelo", A + r"\Escenarios\PSXForestCamp\Textures\Ground_Forest_2m_01_Albedo.png", tinte=(0.55, 0.55, 0.5), escala=(60, 60, 1))
M_CONC = mat_tex("Concreto", A + r"\Escenarios\Texturas\Concreto.png", tinte=(0.55, 0.55, 0.55), escala=(6, 6, 1))
M_ROCA = mat_tex("Roca", None, color=(0.09, 0.085, 0.08))
M_RIEL = mat_tex("Riel", None, color=(0.22, 0.18, 0.15), rough=0.5)
M_DURM = mat_tex("Durmiente", None, color=(0.12, 0.08, 0.05))
M_NEGRO = mat_tex("Fondo", None, color=(0.0, 0.0, 0.0))
M_LAMPARA = mat_tex("Lampara", None, color=(0.7, 0.8, 1.0), emis=6.0)

# ------------------------------------------------------------------ escenario
try:
    plano("Suelo", (0, 20, 0), (220, 220), M_SUELO)
    # Cerro con la boca del túnel en y = 40 (hueco de 5 x 4.2 m)
    caja("CerroIzq", (-23, 90, 7), (41, 100, 14), M_ROCA)
    caja("CerroDer", (23, 90, 7), (41, 100, 14), M_ROCA)
    caja("CerroArriba", (0, 90, 9.6), (5.2, 100, 8.8), M_ROCA)
    # Fachada de concreto con el letrero
    caja("MuroIzq", (-4.5, 39.8, 3.2), (4.0, 0.6, 6.4), M_CONC)
    caja("MuroDer", (4.5, 39.8, 3.2), (4.0, 0.6, 6.4), M_CONC)
    caja("MuroArriba", (0, 39.8, 5.3), (5.0, 0.6, 2.2), M_CONC)
    exp = mat_tex("Expediente08", A + r"\Escenarios\Texturas\PuertaExpediente08.png", alfa=True)
    plano("Letrero", (0, 39.45, 5.3), (3.6, 1.6), exp, rot=(math.radians(90), 0, math.radians(180)))
    marca = mat_tex("MarcaWatcher", A + r"\Escenarios\Texturas\MarcaWatcher.png", alfa=True, tinte=(1, 0.3, 0.25))
    plano("Marca", (-4.3, 39.45, 2.2), (1.4, 1.4), marca, rot=(math.radians(90), 0, math.radians(180)))
    # Túnel por dentro: y 40 → 132
    caja("TunelPiso", (0, 86, -0.05), (5.2, 92, 0.1), M_CONC)
    caja("TunelParedI", (-2.6, 86, 2.1), (0.2, 92, 4.2), M_CONC)
    caja("TunelParedD", (2.6, 86, 2.1), (0.2, 92, 4.2), M_CONC)
    caja("TunelTecho", (0, 86, 4.25), (5.4, 92, 0.2), M_CONC)
    caja("TunelFondo", (0, 132, 2.1), (5.2, 0.2, 4.2), M_NEGRO)
    for x in (-0.72, 0.72):
        caja("Riel", (x, 86, 0.08), (0.08, 92, 0.1), M_RIEL)
    base = caja("Durmiente", (0, 40.6, 0.03), (2.2, 0.22, 0.06), M_DURM)
    for i in range(1, 112):
        d = base.copy(); d.location.y = 40.6 + i * 0.8; col.objects.link(d)
    # Pintas en las paredes
    for i, (tx, y) in enumerate((("Pinta_NoVoltees", 60), ("Pinta_CuantasVeces", 74), ("Pinta_ElTeGuia", 88))):
        mp = mat_tex(tx, A + r"\Escenarios\Texturas\%s.png" % tx, alfa=True, tinte=(0.9, 0.2, 0.15))
        lado = -1 if i % 2 == 0 else 1
        plano(tx, (lado * 2.48, y, 1.9), (2.6, 1.3), mp, rot=(math.radians(90), 0, math.radians(-90 * lado)))
    # Vagón abandonado al fondo
    caja("Vagon", (0, 118, 1.6), (2.8, 11, 3.0), mat_tex("Vagon", None, color=(0.08, 0.12, 0.1), rough=0.6))
    # Polvo / niebla solo dentro del túnel
    mv = bpy.data.materials.new("NieblaTunel"); mv.use_nodes = True
    nv = mv.node_tree.nodes; nv.remove(nv.get("Principled BSDF"))
    pv = nv.new("ShaderNodeVolumePrincipled"); pv.inputs["Density"].default_value = 0.05; pv.inputs["Color"].default_value = (0.5, 0.55, 0.6, 1)
    mv.node_tree.links.new(pv.outputs[0], nv.get("Material Output").inputs["Volume"])
    caja("NieblaTunel", (0, 86, 2.1), (5.0, 92, 4.1), mv)
    log("escenario ok")
except Exception:
    log("ERROR escenario", traceback.format_exc())

# Árboles (pinos PSX) a los lados del sendero
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
        x = random.uniform(-45, 45); y = random.uniform(-25, 39)
        if abs(x) < 3.2 + (2.5 if y > 30 else 0): continue
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

# ------------------------------------------------------------------ personajes
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

PSX = A + r"\Characters\Protagonista\PSXCreator"
ATLAS = PSX + r"\atlas.png"
try:
    mario, arm_m = personaje("Mario", PSX + r"\man_2.fbx", [PSX + r"\man_jacket.fbx", PSX + r"\man_jeans.fbx", PSX + r"\man_boots.fbx", PSX + r"\man_cap.fbx"], ATLAS, (0.62, 0.5, 0.38), 1.78, True)
    chuy, arm_c = personaje("Chuy", PSX + r"\man_2.fbx", [PSX + r"\man_tee.fbx", PSX + r"\man_jeans.fbx", PSX + r"\man_boots.fbx", PSX + r"\man_hair_short.fbx"], ATLAS, (0.45, 0.52, 0.62), 1.72, True)
except Exception:
    log("ERROR senderistas", traceback.format_exc()); mario = chuy = arm_m = arm_c = None
try:
    sonr, arm_s = personaje("La_Sonriente", A + r"\Characters\Monstruos\LowbitLab\hooded_figure.fbx", [], A + r"\Characters\Monstruos\LowbitLab\hooded_figure_atlas.png", (0.3, 0.3, 0.33), 1.95, False)
    # Cara sonriente (como en el juego): una placa con la textura frente a la capucha
    mc = mat_tex("CaraSonriente", A + r"\Characters\Monstruos\CaraSonriente.png", alfa=True, emis=0.6, tinte=(0.75, 0.75, 0.72))
    cara = plano("Cara_Sonriente", (0, 0, 0), (0.34, 0.34), mc, rot=(math.radians(90), 0, 0))
    hueso = None
    if arm_s:
        for b in arm_s.data.bones:
            if "head" in b.name.lower(): hueso = b.name; break
    bpy.context.view_layer.update()
    if arm_s and hueso:
        pb = arm_s.pose.bones[hueso]
        mh = arm_s.matrix_world @ pb.matrix
        cabeza = mh.translation
        cara.location = cabeza + Vector((0, 0.14, 0.02))
        bpy.context.view_layer.update()
        cara.parent = arm_s; cara.parent_type = "BONE"; cara.parent_bone = hueso
        bpy.context.view_layer.update()
        cola = arm_s.matrix_world @ Matrix.Translation(pb.tail - pb.head) @ pb.matrix
        cara.matrix_parent_inverse = (arm_s.matrix_world @ pb.matrix @ Matrix.Translation((0, pb.length, 0))).inverted()
        cara.matrix_world = Matrix.Translation(cabeza + Vector((0, 0.14, 0.02))) @ Matrix.Rotation(math.radians(90), 4, "X")
    else:
        cara.parent = sonr; cara.location = (0, 0.16, 1.72)
    asom, arm_a = personaje("El_Asomado", A + r"\Characters\Monstruos\DesertCryptids\DC_Mimic.fbx", [], A + r"\Characters\Monstruos\DesertCryptids\DC_Mimic_baked.png", (0.85, 0.82, 0.78), 2.35, False)
except Exception:
    log("ERROR monstruos", traceback.format_exc()); sonr = asom = arm_s = arm_a = None

# ------------------------------------------------------------------ guion (24 fps)
# 1-300 bosque → boca del túnel · 300-470 adentro · 470-560 se detienen · 515 aparece La Sonriente
# 600-613 se va la luz · 613 La Sonriente sobre Chuy · 615 ataque · 650 Mario corre · 725 golpean la cámara (cae)
# 760-850 El Asomado se acerca a la cámara tirada · 850 ataque · 885 fin
try:
    if mario:
        llaves(mario, [(1, (-0.6, 8, 0)), (300, (-0.6, 40, 0)), (470, (-0.7, 55, 0)), (640, (-0.7, 55, 0)), (652, (-0.9, 54.5, 0)), (720, (-1.3, 38, 0))])
        llaves(mario, [(1, (0, 0, 0)), (640, (0, 0, 0)), (652, (0, 0, math.pi))], prop="rotation_euler")
        animar(arm_m, quien="Mario", segmentos=[("walk", 1, 470), ("talk", 470, 560), ("cower", 560, 640), ("run", 645, 760)])
    if chuy:
        llaves(chuy, [(1, (0.7, 6.5, 0)), (300, (0.7, 38.5, 0)), (470, (0.7, 53.5, 0))])
        llaves(chuy, [(1, (0, 0, 0)), (560, (0, 0, 0)), (575, (0, 0, math.radians(-25)))], prop="rotation_euler")
        animar(arm_c, quien="Chuy", segmentos=[("walk", 1, 470), ("idle", 470, 560), ("cower", 560, 630), ("hit", 630, 650), ("death", 650, 760)])
    if sonr:
        llaves(sonr, [(1, (0, 200, -60)), (515, (0.2, 78, 0)), (613, (0.7, 55.3, 0)), (700, (0.7, 55.3, 0)), (760, (0.9, 58, 0))])
        interp_const(sonr)
        sonr.rotation_euler.z = math.pi        # mira hacia la cámara (-Y)
        animar(arm_s, quien="La_Sonriente", segmentos=[("idle", 1, 612), ("attack", 613, 660), ("idle", 660, 700), ("walk", 700, 900)])
    if asom:
        llaves(asom, [(1, (0, 200, -60)), (740, (0.3, 72, 0)), (845, (0.15, 51.4, 0)), (885, (0.15, 51.3, 0))])
        f = fcurvas(asom)
        for fc in f:
            for k in fc.keyframe_points:
                if k.co.x <= 740.5: k.interpolation = "CONSTANT"
        asom.rotation_euler.z = math.pi
        animar(arm_a, quien="El_Asomado", segmentos=[("idle", 1, 740), ("walk", 740, 845), ("attack", 845, 900)])
    log("guion ok")
except Exception:
    log("ERROR guion", traceback.format_exc())

# Linternas de los senderistas
def linterna(padre, energia=450):
    l = bpy.data.lights.new("Linterna", "SPOT"); l.energy = energia; l.spot_size = math.radians(38); l.spot_blend = 0.35
    l.color = (1.0, 0.92, 0.75); l.volume_factor = 0.25
    try: l.use_shadow = True
    except Exception: pass
    o = bpy.data.objects.new("Linterna_" + padre.name, l); link(o)
    o.parent = padre; o.location = (0.3, 0.35, 1.25); o.rotation_euler = (math.radians(84), 0, math.radians(-4))
    return o, l

luces = []
for p in (mario, chuy):
    if p: luces.append(linterna(p))
# La linterna de Chuy se apaga cuando cae
if len(luces) > 1:
    lo, ld = luces[1]
    ld.energy = 450; ld.keyframe_insert("energy", frame=628)
    ld.energy = 0; ld.keyframe_insert("energy", frame=652)

# Luna y lámparas del túnel (parpadean y se apagan todas en 600-613)
sol = bpy.data.lights.new("Luna", "SUN"); sol.energy = 0.35; sol.color = (0.55, 0.65, 1.0)
so = bpy.data.objects.new("Luna", sol); link(so); so.rotation_euler = (math.radians(55), 0, math.radians(30))
lamparas = []
for i, y in enumerate(range(46, 130, 9)):
    ld = bpy.data.lights.new("Lampara%d" % i, "POINT"); ld.energy = 60; ld.color = (0.75, 0.85, 1.0); ld.shadow_soft_size = 0.1
    lo = bpy.data.objects.new("Lampara%d" % i, ld); link(lo); lo.location = (0, y, 3.9)
    caja("Tubo%d" % i, (0, y, 4.1), (0.12, 1.2, 0.05), M_LAMPARA)
    rnd = random.Random(i)
    for f in range(300, 885, 6):
        e = 60
        if rnd.random() < 0.22: e = rnd.choice((0, 8, 15))
        if 598 <= f <= 613: e = 0
        if f >= 725 and y < 70 and rnd.random() < 0.5: e = 0
        ld.energy = e; ld.keyframe_insert("energy", frame=f)
    lamparas.append(ld)
# Lámpara de las linternas: también se van en 600-613
for lo, ld in luces:
    for f, e in ((598, 450), (600, 0), (606, 200), (607, 0), (613, 0), (614, 450)):
        if lo.name.startswith("Linterna_Chuy") and f > 628: continue
        ld.energy = e; ld.keyframe_insert("energy", frame=f)

# ------------------------------------------------------------------ cámara (la videocámara del protagonista)
cd = bpy.data.cameras.new("Videocamara"); cd.lens = 24; cd.clip_start = 0.05; cd.clip_end = 300
cam = bpy.data.objects.new("Videocamara", cd); link(cam); esc.camera = cam
def ckey(f, loc, yaw=0, pitch=0, roll=0):
    mtx = Matrix.Rotation(math.radians(yaw), 4, "Z") @ Matrix.Rotation(math.radians(90 + pitch), 4, "X") @ Matrix.Rotation(math.radians(roll), 4, "Z")
    cam.location = loc; cam.rotation_euler = mtx.to_euler("XYZ", cam.rotation_euler)
    cam.keyframe_insert("location", frame=f); cam.keyframe_insert("rotation_euler", frame=f)
ckey(1, (0.1, 2.0, 1.62), 4, -4)
ckey(140, (0.0, 22.0, 1.6), -6, -2)
ckey(260, (0.2, 33.0, 1.62), 0, 6)          # mira el letrero
ckey(300, (0.1, 36.0, 1.6), 0, 3)
ckey(380, (0.0, 44.0, 1.58), 3, -3)
ckey(470, (0.1, 50.0, 1.6), 0, -2)
ckey(515, (0.1, 50.2, 1.6), 0, 0)
ckey(540, (0.1, 50.2, 1.6), 1, 1)           # zoom/encuadre hacia La Sonriente
ckey(600, (0.1, 50.0, 1.58), -2, 0)
ckey(640, (0.0, 49.6, 1.55), -8, -2, 4)
ckey(700, (-0.2, 49.2, 1.5), -25, -4, -6)   # voltea siguiendo a Mario que corre
ckey(722, (-0.1, 49.3, 1.55), 10, 2, 0)
ckey(728, (0.0, 49.5, 1.1), 30, -20, 35)    # ¡golpe! la cámara cae
ckey(734, (0.2, 49.8, 0.45), 15, -8, 70)
ckey(738, (0.3, 50.0, 0.22), 6, 2, 88)
ckey(742, (0.32, 50.05, 0.3), 4, 3, 84)
ckey(747, (0.33, 50.1, 0.18), 3, 2, 86)
ckey(885, (0.33, 50.1, 0.18), 3, 2, 86)
for fc in fcurvas(cam):
    for k in fc.keyframe_points: k.interpolation = "BEZIER"
    # Cámara en mano
    n1 = fc.modifiers.new("NOISE"); n1.scale = 14; n1.strength = 0.03 if "rotation" in fc.data_path else 0.03
    n1.use_restricted_range = True; n1.frame_start = 1; n1.frame_end = 724; n1.blend_out = 4
    n2 = fc.modifiers.new("NOISE"); n2.scale = 3; n2.strength = 0.18 if "rotation" in fc.data_path else 0.06
    n2.use_restricted_range = True; n2.frame_start = 598; n2.frame_end = 726; n2.blend_in = 6; n2.blend_out = 2
    n3 = fc.modifiers.new("NOISE"); n3.scale = 4; n3.strength = 0.08 if "rotation" in fc.data_path else 0.015
    n3.use_restricted_range = True; n3.frame_start = 846; n3.frame_end = 885; n3.blend_in = 3
# Luz de la videocámara (se queda prendida cuando cae)
lc = bpy.data.lights.new("LuzVideocamara", "SPOT"); lc.energy = 25; lc.volume_factor = 0.0; lc.spot_size = math.radians(55); lc.spot_blend = 0.6; lc.color = (0.85, 0.9, 1.0)
lco = bpy.data.objects.new("LuzVideocamara", lc); link(lco); lco.parent = cam; lco.location = (0.12, -0.05, 0)
log("camara ok")

# Guardar el .blend de la cinemática (archivo aparte; tu archivo original no se toca)
try:
    bpy.ops.wm.save_as_mainfile(filepath=PROY + r"\Blender\Cinematica_LineaK.blend", copy=True)
    log("blend guardado")
except Exception as e:
    log("no se guardó blend", e)

os.makedirs(SALIDA, exist_ok=True)
if PRUEBA:
    for f in PRUEBA:
        esc.frame_set(f)
        esc.render.filepath = SALIDA + "\\prueba_%04d.png" % f
        bpy.ops.render.render(write_still=True)
    esc.render.filepath = SALIDA + "\\f_"
    log("pruebas listas", PRUEBA)
if RENDER:
    bpy.ops.render.render(animation=True)
    log("render listo")
log("fin")
