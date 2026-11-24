# Sonido de la cinemática "Línea K": ambiente, pasos, lámparas, sustos, golpe de la cámara y estática.
# Uso: python3 audio_cinematica.py <carpeta_voces> <salida.wav>
import sys, os, wave, numpy as np, subprocess
SR = 48000
DUR = 1728 / 24 + 0.2
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


# ---- Prólogo: tarde en el campamento
def acorde(freqs, seg, aspero=0.0):
    tt = np.arange(t2i(seg)) / SR; s = np.zeros_like(tt)
    for f in freqs:
        vib = 1 + 0.004 * np.sin(2 * np.pi * 5.5 * tt + f)
        s += np.sin(2 * np.pi * f * tt * vib) + 0.4 * np.sin(4 * np.pi * f * tt * vib)
    s = s / len(freqs)
    if aspero: s = np.tanh(s * (1 + aspero * 6))
    return s
viento = LP(ruido(DUR), 0.004, 2) * 5 * (0.6 + 0.4 * np.sin(np.linspace(0, 10, t2i(DUR))))
add(viento, 0, 0.35, -0.1); add(viento[::-1].copy(), 0, 0.3, 0.2)
# Pájaros al atardecer (0-45 s) y grillos al anochecer (40-72 s)
for tt in rng.uniform(0, 45, 40):
    n = t2i(0.18); x = np.arange(n) / SR
    f0 = rng.uniform(2600, 4200); canto = np.sin(2 * np.pi * (f0 + 900 * np.sin(2 * np.pi * 18 * x)) * x) * env(n, 0.01, 0.12)
    add(canto, tt, 0.06 * (1 - tt / 50), rng.uniform(-0.8, 0.8))
tt = np.arange(t2i(32)) / SR
grillo = np.sin(2 * np.pi * 4300 * tt) * (np.sin(2 * np.pi * 28 * tt) > 0.6) * (np.sin(2 * np.pi * 0.9 * tt) > -0.2)
add(grillo * env(len(tt), 6, 1.5) * 0.05, 40, 1.0, 0.4); add(np.roll(grillo, 9000) * env(len(tt), 6, 1.5) * 0.04, 40, 1.0, -0.5)
# Fogata: crepitar
fuego = LP(ruido(DUR), 0.02) * 2.2
for tt in rng.uniform(0, DUR - 0.2, 260):
    c = ruido(0.02) * env(t2i(0.02), 0.001, 0.015); add(c, tt, rng.uniform(0.05, 0.25), rng.uniform(-0.2, 0.2))
add(fuego * env(t2i(DUR), 4, 2), 0, 0.25)
# Pasos al llegar (0-9 s)
for tt in np.arange(0.3, 9.2, 0.55):
    p = LP(ruido(0.09) * env(t2i(0.09), 0.002, 0.07), 0.25); add(p, tt, 0.35, rng.uniform(-0.2, 0.2))
# El grito lejano (56.8) y el que se asoma (57.6)
tt = np.arange(t2i(1.6)) / SR
f = np.linspace(900, 520, len(tt)) * (1 + 0.03 * np.sin(2 * np.pi * 9 * tt))
grito = np.tanh((np.sin(2 * np.pi * np.cumsum(f) / SR) + 0.3 * ruido(1.6)) * 2.5) * env(len(tt), 0.05, 0.9)
grito = LP(grito, 0.12)          # lejos
eco = np.zeros(len(grito) + t2i(1.2)); eco[:len(grito)] += grito
for d, g in ((0.35, 0.4), (0.7, 0.2)): eco[t2i(d):t2i(d) + len(grito)] += grito * g
add(eco, 56.8, 0.5, 0.5)
add(acorde([311, 329.6, 466, 493.9], 3.5, 0.3) * env(t2i(3.5), 0.02, 2.5), 57.6, 0.28)
add(LP(ruido(3.0), 0.02) * 4 * env(t2i(3), 0.01, 2.8), 57.6, 0.25)
# Anochece: dron grave que sube y corte
tt = np.arange(t2i(7)) / SR
dron = (np.sin(2 * np.pi * 45 * tt) + 0.5 * np.sin(2 * np.pi * 67.5 * tt)) * np.linspace(0, 1, len(tt)) ** 2
add(dron * env(len(tt), 0.5, 1.0), 65.0, 0.35)

VOCES = {}
VOCES = {"h1": (4.00, 1.00, 0.00), "m6": (11.20, 0.90, -0.30), "c5": (18.20, 1.07, 0.30), "m7": (22.10, 0.90, -0.30), "h2": (27.00, 1.00, 0.00), "c6": (30.00, 1.07, 0.30), "m8": (33.70, 0.90, -0.30), "c7": (38.40, 1.07, 0.30), "h3": (45.30, 1.00, 0.00), "m9": (47.50, 0.90, -0.30), "c8": (53.10, 1.07, 0.30), "m10": (61.80, 0.90, -0.30)}
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
    add(d, tt, 0.9, pan)

m = max(np.abs(L).max(), np.abs(R).max(), 1e-6)
L = np.tanh(L / m * 1.2) * 0.9; R = np.tanh(R / m * 1.2) * 0.9
salida = sys.argv[2] if len(sys.argv) > 2 else "audio.wav"
with wave.open(salida, "w") as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
    w.writeframes((np.stack([L, R], 1) * 32767).astype(np.int16).tobytes())
print("ok", salida, DUR)
