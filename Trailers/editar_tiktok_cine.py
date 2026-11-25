# Tráilers verticales CON las cinemáticas (diálogos reales del juego) + tomas de juego.
# Uso (en D:\Proyectos):  python3 editar_tiktok_cine.py 4|5 [piezas]
import os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

BASE = os.path.dirname(os.path.abspath(__file__))
TOMAS = os.path.join(BASE, "Trailer_tomas")
OV = os.path.join(TOMAS, "_overlays")
TMP = os.path.join(TOMAS, "_tmp_cine")
SAL = os.path.join(BASE, "Trailers_TikTok")
CINE = os.path.join(BASE, "Sitio_assets")
MUSICA = os.path.join(CINE, "tema.mp3")
FUENTE = os.path.expanduser("~/mnt/MTY LIGH NIGHT WEB/node_modules/@fontsource/vt323/files/vt323-latin-400-normal.woff")
if not os.path.exists(FUENTE):
    FUENTE = os.path.join(BASE, "vt323.woff")
W, H, FPS = 720, 1280, 30
os.makedirs(TMP, exist_ok=True); os.makedirs(SAL, exist_ok=True)
SOLO = sys.argv[1] if len(sys.argv) > 1 else "4"
PIEZAS = "piezas" in sys.argv

def ff(*a):
    r = subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", *a])
    if r.returncode: sys.exit("ffmpeg falló: " + " ".join(a)[:400])

def fnt(t): return ImageFont.truetype(FUENTE, t)

def envolver(texto, fo, ancho):
    pal, lineas, l = texto.split(), [], ""
    for p in pal:
        prueba = (l + " " + p).strip()
        if fo.getlength(prueba) > ancho and l: lineas.append(l); l = p
        else: l = prueba
    if l: lineas.append(l)
    return lineas

def png_texto(nombre, quien, texto, titulo=None):
    """Subtítulo abajo del video (y título opcional arriba), como en el juego."""
    ruta = os.path.join(TMP, nombre + ".png")
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    def s(xy, t, fo, fill, anchor="ma"):
        x, y = xy
        d.text((x + 2, y + 2), t, font=fo, fill=(0, 0, 0, 220), anchor=anchor)
        d.text((x, y), t, font=fo, fill=fill, anchor=anchor)
    if titulo:
        fo = fnt(70); y = 215
        for l in envolver(titulo, fo, W - 80):
            d.text((W/2 - 3, y), l, font=fo, fill=(220, 40, 30, 150), anchor="ma")
            d.text((W/2 + 3, y), l, font=fo, fill=(80, 150, 255, 120), anchor="ma")
            s((W/2, y), l, fo, (245, 238, 220, 255)); y += 66
    if texto:
        y = 880
        if quien:
            s((W/2, y), quien, fnt(40), (226, 60, 48, 255)); y += 44
        fo = fnt(46)
        for l in envolver(texto, fo, W - 90):
            s((W/2, y), l, fo, (245, 238, 220, 255)); y += 44
    im.save(ruta)
    return ruta

GRADO_TOMA = ("scale=720:1280:flags=neighbor,curves=all=0/0 0.35/0.18 0.7/0.55 1/0.92,"
              "eq=contrast=1.12:saturation=0.85,rgbashift=rh=-4:bh=4,noise=alls=14:allf=t,format=yuv420p")
AUDIO = ["-c:a", "aac", "-b:a", "160k", "-ar", "44100", "-ac", "2"]
VID = ["-c:v", "libx264", "-preset", "veryfast", "-crf", "20", "-r", str(FPS), "-pix_fmt", "yuv420p"]

def cine(nombre, archivo, desde, hasta, quien="", texto="", titulo=None):
    """Pedazo de una cinemática: video 16:9 al centro sobre fondo desenfocado + subtítulo. Conserva su audio."""
    out = os.path.join(TMP, nombre + ".mp4"); dur = hasta - desde
    if os.path.exists(out): return out, dur
    sub = png_texto(nombre + "_sub", quien, texto, titulo)
    filtro = ("[0:v]split[a][b];"
              "[a]scale=-2:1280,crop=720:1280,boxblur=20:2,eq=brightness=-0.25:saturation=0.6[fondo];"
              "[b]scale=720:-2:flags=lanczos,eq=contrast=1.1:saturation=0.85,rgbashift=rh=-2:bh=2,noise=alls=10:allf=t[fr];"
              "[fondo][fr]overlay=0:(H-h)/2-40[c];[1:v]format=rgba[s];[c][s]overlay=0:0,format=yuv420p[v];"
              "[0:a]aresample=44100,pan=stereo|c0=c0|c1=c1,afade=in:d=0.08,afade=out:st=%.2f:d=0.12[au]" % (dur - 0.12))
    ff("-ss", f"{desde}", "-t", f"{dur}", "-i", os.path.join(CINE, archivo), "-loop", "1", "-i", sub,
       "-filter_complex", filtro, "-map", "[v]", "-map", "[au]", "-t", f"{dur}", *VID, *AUDIO, out)
    return out, dur

def toma(nombre, carpeta, desde, cuadros, texto=None, texto_desde=0.0, lento=1.0):
    """Toma de juego a pantalla completa, con golpe grave al inicio."""
    out = os.path.join(TMP, nombre + ".mp4"); dur = cuadros * lento / FPS
    if os.path.exists(out): return out, dur
    entradas = ["-framerate", str(FPS / lento), "-start_number", str(desde), "-i", os.path.join(TOMAS, carpeta, "%05d.jpg")]
    sonido = ["-f", "lavfi", "-t", f"{dur}", "-i",
              "aevalsrc='0.8*sin(2*PI*(55-25*t)*t)*exp(-3*t)+0.2*(random(0)*2-1)*exp(-12*t)':s=44100:c=stereo"]
    if texto:
        filtro = f"[0:v]{GRADO_TOMA},fps={FPS}[v0];[2:v]format=rgba[t];[v0][t]overlay=0:0:enable='gte(t,{texto_desde})'[v]"
        ff(*entradas, *sonido, "-loop", "1", "-i", os.path.join(OV, texto + ".png"), "-filter_complex", filtro,
           "-map", "[v]", "-map", "1:a", "-t", f"{dur}", *VID, *AUDIO, out)
    else:
        ff(*entradas, *sonido, "-filter_complex", f"[0:v]{GRADO_TOMA},fps={FPS}[v]", "-map", "[v]", "-map", "1:a",
           "-t", f"{dur}", *VID, *AUDIO, out)
    return out, dur

def estatica(nombre, cuadros=6):
    out = os.path.join(TMP, nombre + ".mp4"); dur = cuadros / FPS
    if os.path.exists(out): return out, dur
    ff("-f", "lavfi", "-i", f"color=c=gray:s={W}x{H}:r={FPS}", "-f", "lavfi", "-i", "anoisesrc=c=white:a=0.35:r=44100",
       "-vf", "noise=alls=70:allf=t,eq=contrast=1.5,rgbashift=rh=-10:bh=10,format=yuv420p",
       "-af", "pan=stereo|c0=c0|c1=c0", "-t", f"{dur}", *VID, *AUDIO, out)
    return out, dur

def tarjeta(nombre, png, dur, golpe=True):
    out = os.path.join(TMP, nombre + ".mp4")
    if os.path.exists(out): return out, dur
    expr = "0.9*sin(2*PI*(50-22*t)*t)*exp(-2.2*t)" if golpe else "0"
    ff("-loop", "1", "-i", os.path.join(OV, png + ".png"), "-f", "lavfi", "-t", f"{dur}", "-i",
       f"aevalsrc='{expr}':s=44100:c=stereo", "-vf", "format=yuv420p,noise=alls=10:allf=t,rgbashift=rh=-3:bh=3",
       "-t", f"{dur}", *VID, *AUDIO, out)
    return out, dur

def armar(titulo, piezas, archivo):
    if PIEZAS: print("piezas listas"); return
    lista = os.path.join(TMP, archivo + "_lista.txt"); total = 0.0
    with open(lista, "w") as f:
        for p, d in piezas: f.write(f"file '{p}'\n"); total += d
    union = os.path.join(TMP, archivo + "_union.mp4")
    ff("-f", "concat", "-safe", "0", "-i", lista, "-c", "copy", union)
    cierre = total - piezas[-1][1]
    filtro = ("[1:v]format=rgba[h1];[2:v]format=rgba[h2];[3:v]format=rgba[capa];[0:v][capa]overlay=0:0[a];"
              f"[a][h1]overlay=0:0:enable='lt(t,{cierre:.2f})*lt(mod(t,1),0.5)'[b];"
              f"[b][h2]overlay=0:0:enable='lt(t,{cierre:.2f})*gte(mod(t,1),0.5)'[v];"
              f"[4:a]atrim=start=25,asetpts=PTS-STARTPTS,volume=0.22,afade=in:d=1,afade=out:st={total-1.5:.2f}:d=1.5[m];"
              "[0:a][m]amix=inputs=2:normalize=0,alimiter=limit=0.95[au]")
    final = os.path.join(SAL, archivo + ".mp4")
    ff("-i", union, "-loop", "1", "-i", os.path.join(OV, "hud_a.png"), "-loop", "1", "-i", os.path.join(OV, "hud_b.png"),
       "-loop", "1", "-i", os.path.join(OV, "capas.png"), "-i", MUSICA, "-filter_complex", filtro,
       "-map", "[v]", "-map", "[au]", "-t", f"{total:.3f}", "-c:v", "libx264", "-preset", "veryfast", "-crf", "21",
       "-pix_fmt", "yuv420p", "-color_primaries", "bt709", "-color_trc", "bt709", "-colorspace", "bt709",
       "-c:a", "aac", "-b:a", "160k", "-movflags", "+faststart", final)
    ff("-ss", "1.0", "-i", final, "-frames:v", "1", "-vf", "scale=540:960", "-q:v", "3", os.path.join(SAL, archivo + ".jpg"))
    print(f"{titulo}: {total:.1f} s → {final}")

CT, LK = "CampamentoTarde.mp4", "IntroLineaK.mp4"

if SOLO == "4":   # LA LEYENDA (la fogata) → los monstruos de verdad
    P = []
    P.append(cine("d1", CT, 30.0, 33.6, "CHUY", "Oye... ¿a poco no te dijo el supervisor lo que sale en la noche?", "LO QUE ME CONTARON EN MI PRIMER DÍA"))
    P.append(cine("d2", CT, 33.7, 38.3, "MARIO", "Ya, Chuy, no asustes al nuevo. Solo es un practicante."))
    P.append(cine("d3", CT, 38.4, 45.2, "CHUY", "Hace siete años bajó una cuadrilla a la Línea K. Encontraron cinco llaves... y a ninguno de ellos."))
    P.append(cine("d4", CT, 47.5, 50.4, "MARIO", "Son cuentos de la sierra. Que hay una que sonríe..."))
    P.append(estatica("d_st1", 4))
    P.append(toma("d5", "30_linterna", 96, 54))
    P.append(cine("d6", CT, 50.4, 53.0, "MARIO", "...y otro que se asoma entre los árboles."))
    P.append(estatica("d_st2", 4))
    P.append(toma("d7", "40_asomado", 40, 50))
    P.append(toma("d8", "41_asomado_corre", 20, 35))
    P.append(estatica("d_st3", 4))
    P.append(cine("d9", CT, 53.1, 56.7, "CHUY", "Si allá adentro alguien te sonríe... no le regreses la sonrisa."))
    P.append(toma("d10", "31_sonriente_cara", 20, 55))
    P.append(estatica("d_st4", 8))
    P.append(tarjeta("d11", "cierre", 3.4))
    armar("LA LEYENDA", P, "tiktok_4_la_leyenda")

if SOLO == "5":   # LÍNEA K (lo que grabó la cámara)
    P = []
    P.append(cine("e1", LK, 22.0, 25.1, "MARIO", "Hay alguien allá al fondo... ¿Hola?  —CHUY: ¡No es una persona!", "LO ÚLTIMO QUE GRABÓ LA CÁMARA"))
    P.append(estatica("e_st0", 5))
    P.append(cine("e2", LK, 4.0, 8.5, "MARIO", "Según los planos, la entrada de la Línea K está por aquí. Nunca la terminaron."))
    P.append(cine("e3", LK, 11.2, 16.7, "MARIO", "Mira. Expediente cero ocho. Es aquí.  —TÚ: Sigue grabando..."))
    P.append(cine("e4", LK, 19.5, 21.5, "CHUY", "Esperen... ¿oyeron eso?"))
    P.append(toma("e5", "20_tunel", 0, 30, lento=2.0))
    P.append(cine("e6", LK, 27.1, 29.5, "MARIO", "¡Corre! ¡CORRE!"))
    P.append(estatica("e_st1", 4))
    P.append(toma("e7", "31_sonriente_cara", 25, 50))
    P.append(estatica("e_st2", 6))
    P.append(cine("e8", LK, 31.5, 34.2, "TÚ", "No... no puedo moverme..."))
    P.append(tarjeta("e9", "t_sal", 1.6, golpe=False))
    P.append(tarjeta("e10", "cierre", 3.4))
    armar("LÍNEA K", P, "tiktok_5_linea_k")
