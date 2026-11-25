# Arma los tráilers verticales (1080x1920, 30 fps) a partir de las tomas grabadas en Unity (menú 23).
# Uso (en la carpeta D:\Proyectos):  python3 editar_tiktok.py
import os, subprocess, shutil, sys

BASE = os.path.dirname(os.path.abspath(__file__))
TOMAS = os.path.join(BASE, "Trailer_tomas")
OV = os.path.join(BASE, "Trailer_tomas", "_overlays")
TMP = os.path.join(BASE, "Trailer_tomas", "_tmp")
SAL = os.path.join(BASE, "Trailers_TikTok")
MUSICA = os.path.join(BASE, "Sitio_assets", "tema.mp3")
FPS = 30
SOLO = sys.argv[1] if len(sys.argv) > 1 else "todos"   # 1, 2, 3 o todos
REHACER = "--rehacer" in sys.argv
os.makedirs(TMP, exist_ok=True); os.makedirs(SAL, exist_ok=True)

def ff(*args):
    r = subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", *args])
    if r.returncode: sys.exit("ffmpeg falló: " + " ".join(args))

GRADO = ("scale=720:1280:flags=neighbor,"
         "curves=all=0/0 0.35/0.18 0.7/0.55 1/0.92,eq=contrast=1.12:saturation=0.85,"
         "colorbalance=bs=0.05:bm=0.03:rh=0.02,"
         "rgbashift=rh=-4:bh=4,noise=alls=14:allf=t,format=yuv420p")

def seg(nombre, toma, desde, cuadros, texto=None, texto_desde=0.0, lento=1.0):
    """Un pedazo de toma con el estilo VHS y (opcional) un texto encima."""
    out = os.path.join(TMP, nombre + ".mp4")
    if os.path.exists(out) and not REHACER: return out, cuadros * lento / FPS
    entrada = ["-framerate", str(FPS / lento), "-start_number", str(desde), "-i", os.path.join(TOMAS, toma, "%05d.jpg")]
    dur = cuadros * lento / FPS
    if texto:
        filtro = (f"[0:v]{GRADO},fps={FPS}[v];[1:v]format=rgba[t];"
                  f"[v][t]overlay=0:0:enable='gte(t,{texto_desde})'[o]")
        ff(*entrada, "-loop", "1", "-i", os.path.join(OV, texto + ".png"), "-filter_complex", filtro,
           "-map", "[o]", "-t", f"{dur:.3f}", "-r", str(FPS), "-c:v", "libx264", "-preset", "veryfast", "-crf", "18", out)
    else:
        ff(*entrada, "-vf", f"{GRADO},fps={FPS}", "-t", f"{dur:.3f}", "-r", str(FPS),
           "-c:v", "libx264", "-preset", "veryfast", "-crf", "18", out)
    return out, dur

def estatica(nombre, cuadros=6):
    out = os.path.join(TMP, nombre + ".mp4")
    if os.path.exists(out) and not REHACER: return out, cuadros / FPS
    ff("-f", "lavfi", "-i", f"color=c=gray:s=720x1280:r={FPS}", "-vf",
       "noise=alls=70:allf=t,eq=contrast=1.5,rgbashift=rh=-10:bh=10,format=yuv420p",
       "-frames:v", str(cuadros), "-c:v", "libx264", "-preset", "veryfast", "-crf", "20", out)
    return out, cuadros / FPS

def tarjeta(nombre, png, seg_):
    out = os.path.join(TMP, nombre + ".mp4")
    if os.path.exists(out) and not REHACER: return out, seg_
    ff("-loop", "1", "-i", os.path.join(OV, png + ".png"), "-vf",
       f"format=yuv420p,noise=alls=10:allf=t,rgbashift=rh=-3:bh=3", "-t", str(seg_), "-r", str(FPS),
       "-c:v", "libx264", "-preset", "veryfast", "-crf", "18", out)
    return out, seg_

def armar(titulo, piezas, archivo):
    """Une las piezas, pone HUD + líneas + música + golpes de sonido en cada estática."""
    lista = os.path.join(TMP, archivo + "_lista.txt")
    t, golpes = 0.0, []
    with open(lista, "w") as f:
        for p, d in piezas:
            f.write(f"file '{p}'\n")
            if os.path.basename(p).startswith("st"): golpes.append(t)
            t += d
    total = t
    union = os.path.join(TMP, archivo + "_union.mp4")
    ff("-f", "concat", "-safe", "0", "-i", lista, "-c", "copy", union)
    cierre_t = total - piezas[-1][1]
    # audio: música (de la parte intensa) + golpes graves en cada corte de estática + golpe final
    sfx = []
    for i, g in enumerate(golpes + [cierre_t]):
        sfx.append(f"[{4+i}:a]adelay={int(g*1000)}|{int(g*1000)}[g{i}]")
    entradas_sfx = []
    for _ in golpes + [cierre_t]:
        entradas_sfx += ["-f", "lavfi", "-t", "1.4", "-i",
                         "aevalsrc='0.9*sin(2*PI*(55-25*t)*t)*exp(-3.2*t)+0.25*(random(0)*2-1)*exp(-14*t)':s=44100:c=stereo"]
    mezcla = "".join(f"[g{i}]" for i in range(len(golpes) + 1))
    filtro = (
        "[1:v]format=rgba[h1];[2:v]format=rgba[h2];[3:v]format=rgba[capa];"
        "[0:v][capa]overlay=0:0[a];"
        f"[a][h1]overlay=0:0:enable='lt(t,{cierre_t:.2f})*lt(mod(t,1),0.5)'[b];"
        f"[b][h2]overlay=0:0:enable='lt(t,{cierre_t:.2f})*gte(mod(t,1),0.5)'[v];"
        + ";".join(sfx) + ";"
        f"[{4+len(golpes)+1}:a]atrim=start=25,asetpts=PTS-STARTPTS,volume=0.75,afade=in:d=0.6,afade=out:st={total-1.5:.2f}:d=1.5[m];"
        f"[m]{mezcla}amix=inputs={len(golpes)+2}:normalize=0,alimiter=limit=0.95[au]"
    )
    final = os.path.join(SAL, archivo + ".mp4")
    ff("-i", union, "-loop", "1", "-i", os.path.join(OV, "hud_a.png"), "-loop", "1", "-i", os.path.join(OV, "hud_b.png"),
       "-loop", "1", "-i", os.path.join(OV, "capas.png"), *entradas_sfx, "-i", MUSICA,
       "-filter_complex", filtro, "-map", "[v]", "-map", "[au]", "-t", f"{total:.3f}",
       "-c:v", "libx264", "-profile:v", "high", "-preset", "veryfast", "-crf", "21", "-pix_fmt", "yuv420p",
       "-color_primaries", "bt709", "-color_trc", "bt709", "-colorspace", "bt709",
       "-c:a", "aac", "-b:a", "160k", "-movflags", "+faststart", final)
    # póster para la página
    ff("-ss", "1.2", "-i", final, "-frames:v", "1", "-vf", "scale=540:960", "-q:v", "3", os.path.join(SAL, archivo + ".jpg"))
    print(f"{titulo}: {total:.1f} s → {final}")

def t1():
    P = []
    P.append(seg("a1", "31_sonriente_cara", 50, 25))
    P.append(estatica("st_a1"))
    P.append(seg("a2", "30_linterna", 0, 70, "t_soloSeMueve", 0.4))
    P.append(seg("a3", "30_linterna", 70, 80))
    P.append(estatica("st_a2"))
    P.append(seg("a4", "20_tunel", 0, 45, lento=2.0))
    P.append(estatica("st_a3", 4))
    P.append(seg("a5", "31_sonriente_cara", 0, 45, "t_noLeRegreses", 0.1))
    P.append(seg("a6", "31_sonriente_cara", 45, 30))
    P.append(estatica("st_a4", 8))
    P.append(tarjeta("a7", "t_noVoltees", 1.6))
    P.append(tarjeta("a8", "cierre", 3.2))
    armar("NO VOLTEES", P, "tiktok_1_no_voltees")

def t2():
    P = []
    P.append(seg("b1", "41_asomado_corre", 25, 30))
    P.append(estatica("st_b1"))
    P.append(seg("b2", "01_bosque", 0, 100, "t_cinta", 0.3))
    P.append(seg("b3", "40_asomado", 0, 90, "t_siLoVes", 1.2))
    P.append(estatica("st_b2", 4))
    P.append(seg("b4", "42_asomado_camina", 0, 90, "t_siLoIgnoras", 0.5))
    P.append(seg("b5", "41_asomado_corre", 0, 55, "t_corre", 1.2))
    P.append(estatica("st_b3", 8))
    P.append(tarjeta("b6", "cierre", 3.2))
    armar("EL ASOMADO", P, "tiktok_2_el_asomado")

def t3():
    P = []
    P.append(seg("c1", "02_vigia", 0, 120, "t_sigueVigia", 1.8))
    P.append(seg("c2", "03_vigia_rie", 0, 60))
    P.append(estatica("st_c1"))
    for i in range(5):
        P.append(seg(f"c3{i}", f"1{i}_lugar", 30, 60, f"t_l{i+1}", 0.1))
        if i < 4: P.append(estatica(f"st_c3{i}", 3))
    P.append(seg("c4", "20_tunel", 0, 45, "t_unBucle", 0.1))
    P.append(estatica("st_c2"))
    P.append(seg("c5", "50_portal", 0, 180, "t_sal", 4.0))
    P.append(estatica("st_c3", 8))
    P.append(tarjeta("c6", "cierre", 3.2))
    armar("5 LLAVES", P, "tiktok_3_cinco_llaves")

# Uso: python3 editar_tiktok.py [1|2|3|todos] [piezas|final] [--rehacer]
FASE = sys.argv[2] if len(sys.argv) > 2 and not sys.argv[2].startswith("--") else "todo"
if FASE == "piezas":
    _armar = armar
    armar = lambda *a, **k: print("piezas listas")
for n, f in (("1", t1), ("2", t2), ("3", t3)):
    if SOLO in (n, "todos"): f()
