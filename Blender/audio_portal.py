# Sonido de la cinemática "Línea K": ambiente, pasos, lámparas, sustos, golpe de la cámara y estática.
# Uso: python3 audio_cinematica.py <carpeta_voces> <salida.wav>
import sys, os, wave, numpy as np, subprocess
SR = 48000
DUR = 720 / 24 + 0.2
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

# ---- Final: la puerta del bucle
viento = LP(ruido(DUR), 0.004, 2) * 6 * (0.6 + 0.4 * np.sin(np.linspace(0, 12, t2i(DUR))))
add(viento, 0, 0.45, -0.1); add(viento[::-1].copy(), 0, 0.35, 0.2)
def paso(fuerte=1.0):
    p = ruido(0.09) * env(t2i(0.09), 0.002, 0.07); return LP(p, 0.25) * fuerte
for tt in np.arange(0.3, 6.0, 0.55): add(paso(), tt, 0.4, rng.uniform(-0.3, 0.3))
# Cerraduras: clic metálico + campanita
for i, f in enumerate((40, 62, 84, 106, 128)):
    t0 = f / 24; tt = np.arange(t2i(1.6)) / SR
    clic = ruido(0.03) * env(t2i(0.03), 0.001, 0.025)
    camp = np.sin(2 * np.pi * (660 + i * 110) * tt) * np.exp(-tt * 3) * 0.5
    add(clic, t0, 0.8); add(camp, t0, 0.5)
# Coro grave que sube
tt = np.arange(t2i(24)) / SR
coro = sum(np.sin(2 * np.pi * f * tt * (1 + 0.003 * np.sin(2 * np.pi * 4.5 * tt + f))) for f in (110, 130.8, 164.8, 220)) / 4
add(coro * np.linspace(0, 1, len(tt)) ** 1.5 * env(len(tt), 2, 0.5), 5.5, 0.35)
# La reja rechina (165 → 230)
tt = np.arange(t2i(2.8)) / SR
f = 300 + 500 * np.abs(np.sin(2 * np.pi * 0.9 * tt))
rech = np.sin(2 * np.pi * np.cumsum(f) / SR) * (0.4 + 0.6 * LP(rng.standard_normal(len(tt)), 0.01) * 8)
add(np.tanh(rech * 2) * env(len(tt), 0.05, 0.6), 165 / 24, 0.35)
# Zumbido del portal
tt = np.arange(t2i(22)) / SR
hum = (np.sin(2 * np.pi * 55 * tt) * 0.6 + np.sin(2 * np.pi * 1760 * tt + 3 * np.sin(2 * np.pi * 0.3 * tt)) * 0.08)
add(hum * env(len(tt), 3, 0.3), 7.5, 0.4)
# Volteas (490): golpe + gruñidos
add(LP(ruido(0.5), 0.02) * 10 * env(t2i(0.5), 0.002, 0.45), 20.4, 0.8)
tt = np.arange(t2i(2.6)) / SR
gru = np.tanh(3 * (np.sin(2 * np.pi * 52 * tt) * (0.5 + 0.5 * np.sin(2 * np.pi * 2.7 * tt)) + 0.4 * LP(rng.standard_normal(len(tt)), 0.05) * 4))
add(gru * env(len(tt), 0.2, 1.0), 20.9, 0.45, 0.3)
# Se hunden (560 → 600): tono que cae
tt = np.arange(t2i(2.0)) / SR
cae = np.sin(2 * np.pi * np.cumsum(np.linspace(500, 60, len(tt))) / SR) * env(len(tt), 0.05, 0.6)
add(np.tanh(cae * 2), 23.3, 0.35)
# Cruzas: ruido blanco y un tono que sube hasta cortarse
tt = np.arange(t2i(4.2)) / SR
sube = np.sin(2 * np.pi * np.cumsum(np.linspace(200, 3200, len(tt))) / SR) * np.linspace(0, 1, len(tt)) ** 2
add(sube * 0.4 + ruido(4.2) * np.linspace(0, 1, len(tt)) ** 3 * 0.6, 26.0, 0.8)

VOCES = {"p3": (12.6, 1.0, 0.0), "m5": (14.2, 0.9, -0.2), "c4": (22.6, 1.07, 0.3)}
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
