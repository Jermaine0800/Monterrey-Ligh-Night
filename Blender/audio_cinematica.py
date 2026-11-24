# Sonido de la cinemática "Línea K": ambiente, pasos, lámparas, sustos, golpe de la cámara y estática.
# Uso: python3 audio_cinematica.py <carpeta_voces> <salida.wav>
import sys, os, wave, numpy as np, subprocess
SR = 48000
DUR = 885 / 24 + 0.2
N = int(SR * DUR)
rng = np.random.default_rng(1127)
L = np.zeros(N); R = np.zeros(N)

def t2i(t): return int(t * SR)
def add(sig, t, vol=1.0, pan=0.0):
    i = t2i(t); j = min(N, i + len(sig))
    if j <= i: return
    s = sig[:j - i] * vol
    L[i:j] += s * (1 - max(0, pan)); R[i:j] += s * (1 + min(0, pan))
def LP(x, a, veces=1):
    """Paso bajo de un polo hecho con convolución FFT (rápido, sin scipy)."""
    n = int(min(len(x), max(16, 9 / a)))
    h = a * (1 - a) ** np.arange(n)
    for _ in range(veces):
        m = len(x) + n - 1; F = 1 << (m - 1).bit_length()
        x = np.fft.irfft(np.fft.rfft(x, F) * np.fft.rfft(h, F), F)[:len(x)]
    return x
def env(n, ataque, caida):
    e = np.ones(n); a = max(1, int(ataque * SR)); c = max(1, int(caida * SR))
    e[:a] = np.linspace(0, 1, a); e[-c:] *= np.linspace(1, 0, c); return e
def ruido(seg): return rng.standard_normal(int(seg * SR))

# 1) Viento del bosque (0 → 12.5 s) y zumbido del túnel (12.5 → fin)
viento = LP(ruido(13), 0.004, 2); viento *= 6 * (0.6 + 0.4 * np.sin(np.linspace(0, 9, len(viento))))
viento *= env(len(viento), 1.5, 2.5); add(viento, 0, 0.5, -0.1); add(viento[::-1].copy(), 0, 0.4, 0.2)
t = np.arange(t2i(25)) / SR
zumbido = (0.5 * np.sign(np.sin(2 * np.pi * 120 * t)) + 0.3 * np.sin(2 * np.pi * 60 * t)) * 0.03
zumbido = LP(zumbido, 0.08) * env(len(zumbido), 2, 0.05)
add(zumbido, 12.0, 0.7)
grave = LP(ruido(25), 0.0015, 2) * 18 * env(t2i(25), 3, 0.5)
add(grave, 12.3, 0.45)

# 2) Pasos: grava en el bosque, eco en el túnel
def paso(eco=False, fuerte=1.0):
    p = ruido(0.09) * env(t2i(0.09), 0.002, 0.07)
    p = LP(p, 0.25) * fuerte
    if eco:
        out = np.zeros(len(p) + t2i(0.5)); out[:len(p)] += p
        for d, g in ((0.11, 0.45), (0.23, 0.25), (0.37, 0.12)): out[t2i(d):t2i(d) + len(p)] += p * g
        return out
    return p
for k, tt in enumerate(np.arange(0.4, 19.6, 0.52)):
    eco = tt > 12.6
    add(paso(eco, 0.9), tt + rng.uniform(-0.03, 0.03), 0.5, rng.uniform(-0.4, 0.4))
    add(paso(eco, 0.7), tt + 0.26 + rng.uniform(-0.03, 0.03), 0.35, rng.uniform(-0.4, 0.4))
# Respiración del que graba
for tt in np.arange(1.0, 24, 3.2):
    r = LP(ruido(1.1), 0.05) * env(t2i(1.1), 0.4, 0.6) * 1.4; add(r, tt, 0.25)

# 3) Lámparas que truenan (al azar) y apagón 25.0 → 25.5
for tt in rng.uniform(12.6, 36, 14):
    c = ruido(0.05) * env(t2i(0.05), 0.001, 0.04); add(c, tt, 0.25, rng.uniform(-0.6, 0.6))
apagon = ruido(0.12) * env(t2i(0.12), 0.001, 0.1); add(apagon, 24.95, 0.9)
add(-zumbido[:t2i(0.6)] * 0, 25.0)  # silencio (el zumbido sigue debajo)

# 4) Revelación de La Sonriente (21.5 s): golpe de cuerdas disonante
def acorde(freqs, seg, aspero=0.0):
    tt = np.arange(t2i(seg)) / SR; s = np.zeros_like(tt)
    for f in freqs:
        vib = 1 + 0.004 * np.sin(2 * np.pi * 5.5 * tt + f)
        s += np.sin(2 * np.pi * f * tt * vib) + 0.4 * np.sin(4 * np.pi * f * tt * vib)
    s = s / len(freqs)
    if aspero: s = np.tanh(s * (1 + aspero * 6))
    return s
add(acorde([311, 329.6, 466, 493.9, 987], 3.2, 0.3) * env(t2i(3.2), 0.01, 2.6), 21.45, 0.32)
add(LP(ruido(3.0), 0.02) * 4 * env(t2i(3), 0.01, 2.8), 21.45, 0.3)
# Dron que sube hasta el apagón
tt = np.arange(t2i(4.5)) / SR
dron = np.sin(2 * np.pi * (40 + 25 * tt / 4.5) * tt) * 0.5 + 0.5 * np.sin(2 * np.pi * (80.5 + 50 * tt / 4.5) * tt)
add(dron * np.linspace(0, 1, len(dron)) ** 2, 20.5, 0.4)

# 5) Ataque (25.6 s): grito distorsionado + golpes + pánico
def grito(seg, f0, f1, aspero=2.5):
    tt = np.arange(t2i(seg)) / SR
    f = np.linspace(f0, f1, len(tt)) * (1 + 0.03 * np.sin(2 * np.pi * 9 * tt))
    s = np.sin(2 * np.pi * np.cumsum(f) / SR) + 0.6 * np.sin(2 * np.pi * np.cumsum(f * 2.01) / SR) + 0.3 * ruido(seg)
    return np.tanh(s * aspero) * env(len(tt), 0.02, seg * 0.5)
add(grito(1.6, 900, 520), 25.6, 0.5, 0.2)
add(grito(1.2, 380, 260, 4), 26.3, 0.45, 0.3)          # grito de Chuy
for tt in (25.65, 26.1, 26.5, 27.0):
    g = LP(ruido(0.25), 0.03) * 6 * env(t2i(0.25), 0.002, 0.2); add(g, tt, 0.7)
add(acorde([233, 247, 349, 370], 2.0, 1.0) * env(t2i(2.0), 0.005, 1.5), 25.6, 0.3)
for k, tt in enumerate(np.arange(27.1, 30.2, 0.21)):     # Mario corriendo
    add(paso(True, 1.2), tt, 0.6 * (1 - (tt - 27.1) / 3.5), -0.5)
for tt in np.arange(25.8, 30.0, 0.45):                   # respiración agitada
    r = LP(ruido(0.35), 0.08) * env(t2i(0.35), 0.05, 0.25) * 2; add(r, tt, 0.35)

# 6) Golpe a la cámara (30.2 s), caída, zumbido en los oídos
golpe = LP(ruido(0.4), 0.015) * 14 * env(t2i(0.4), 0.001, 0.35); add(golpe, 30.2, 1.0)
for tt, v in ((30.55, 0.8), (30.75, 0.5), (30.95, 0.3)):
    c = ruido(0.12) * env(t2i(0.12), 0.001, 0.1); c = LP(c, 0.35); add(c, tt, v)
tt = np.arange(t2i(5.5)) / SR
tinnitus = np.sin(2 * np.pi * 3800 * tt) * env(len(tt), 0.05, 5.0) * 0.08
add(tinnitus, 30.3, 1.0)
# 7) El Asomado se acerca: pasos pesados lentos + gruñido
for tt in np.arange(31.0, 35.2, 0.62):
    p = LP(ruido(0.18), 0.02) * 8 * env(t2i(0.18), 0.003, 0.15)
    add(p, tt, 0.25 + 0.6 * (tt - 31) / 4.2)
tt = np.arange(t2i(4.4)) / SR
gru = np.tanh(3 * (np.sin(2 * np.pi * 55 * tt) * (0.5 + 0.5 * np.sin(2 * np.pi * 3.1 * tt)) + 0.4 * LP(rng.standard_normal(len(tt)), 0.05) * 4))
add(gru * np.linspace(0.05, 1, len(tt)) * 0.35, 31.0)
add(grito(1.4, 1400, 300, 5), 35.25, 0.65)
add(acorde([180, 191, 270, 285], 1.6, 2.0) * env(t2i(1.6), 0.002, 1.2), 35.25, 0.35)
# 8) Estática final
est = ruido(0.9) * 0.5; add(est, DUR - 0.9, 0.6)

# Voces (Windows TTS) con filtro de videocámara y tono distinto por persona
VOCES = {"m1": (4.0, 0.9, -0.3), "c1": (8.8, 1.07, 0.3), "m2": (11.2, 0.9, -0.2), "p1": (14.3, 1.0, 0.0),
         "c2": (19.6, 1.07, 0.3), "m3": (22.0, 0.9, -0.2), "c3": (24.1, 1.1, 0.3), "m4": (27.2, 0.92, -0.4), "p2": (31.6, 0.97, 0.0)}
carpeta = sys.argv[1] if len(sys.argv) > 1 else ""
for vid, (tt, tono, pan) in VOCES.items():
    ruta = os.path.join(carpeta, vid + ".wav")
    if not os.path.exists(ruta): print("falta voz", vid); continue
    tmp = "/tmp/_voz_%s.wav" % vid
    with wave.open(ruta) as w0: sr_in = w0.getframerate()
    filtro = "asetrate=%d,aresample=%d,highpass=f=280,lowpass=f=3400,volume=1.6" % (int(sr_in * tono), SR)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", ruta, "-af", filtro, "-ac", "1", tmp], check=False)
    with wave.open(tmp) as w:
        d = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(float) / 32768
    if vid == "p2": d = LP(d, 0.3) * 0.8   # apenas un susurro, ya en el piso
    if vid in ("c3", "m4"): d = np.tanh(d * 2.2) * 0.8
    add(d, tt, 0.9, pan)

m = max(np.abs(L).max(), np.abs(R).max(), 1e-6)
L = np.tanh(L / m * 1.2) * 0.9; R = np.tanh(R / m * 1.2) * 0.9
salida = sys.argv[2] if len(sys.argv) > 2 else "audio.wav"
with wave.open(salida, "w") as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
    w.writeframes((np.stack([L, R], 1) * 32767).astype(np.int16).tobytes())
print("ok", salida, DUR)
