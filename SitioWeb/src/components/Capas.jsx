import { useEffect, useRef, useState } from "react";

// Ruido VHS, líneas, viñeta, barra de glitch y HUD de videocámara
export default function Capas() {
  const lienzo = useRef(null);
  const barra = useRef(null);
  const ceniza = useRef(null);
  const [cinta, setCinta] = useState("00:00:00");
  const [hora, setHora] = useState("23:47:00");

  // ruido animado (lienzo chiquito escalado: casi no gasta)
  useEffect(() => {
    const c = lienzo.current, ctx = c.getContext("2d");
    const w = 200, h = 112; c.width = w; c.height = h;
    const img = ctx.createImageData(w, h);
    let raf, n = 0, vivo = true;
    const reducir = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const pinta = () => {
      if (!vivo) return;
      if (n++ % 3 === 0 && !document.hidden) {
        const d = img.data;
        for (let i = 0; i < d.length; i += 4) { const v = (Math.random() * 255) | 0; d[i] = d[i + 1] = d[i + 2] = v; d[i + 3] = 255; }
        ctx.putImageData(img, 0, 0);
      }
      if (!reducir) raf = requestAnimationFrame(pinta);
    };
    pinta();
    return () => { vivo = false; cancelAnimationFrame(raf); };
  }, []);

  // ceniza flotando (partículas muy ligeras)
  useEffect(() => {
    const c = ceniza.current, ctx = c.getContext("2d");
    const reducir = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    let w, h, raf;
    const ajustar = () => { w = c.width = window.innerWidth; h = c.height = window.innerHeight; };
    ajustar(); window.addEventListener("resize", ajustar);
    const ps = Array.from({ length: window.innerWidth < 700 ? 25 : 55 }, () => ({ x: Math.random() * w, y: Math.random() * h, r: Math.random() * 1.8 + 0.4, v: Math.random() * 0.35 + 0.1, o: Math.random() * 0.5 + 0.15, f: Math.random() * 6 }));
    const pinta = () => {
      ctx.clearRect(0, 0, w, h);
      for (const p of ps) {
        p.y -= p.v; p.x += Math.sin((p.y + p.f * 50) / 60) * 0.3;
        if (p.y < -5) { p.y = h + 5; p.x = Math.random() * w; }
        ctx.fillStyle = `rgba(217,207,184,${p.o})`;
        ctx.fillRect(p.x, p.y, p.r, p.r);
      }
      if (!reducir && !document.hidden) raf = requestAnimationFrame(pinta); else raf = setTimeout(pinta, 500);
    };
    pinta();
    return () => { cancelAnimationFrame(raf); clearTimeout(raf); window.removeEventListener("resize", ajustar); };
  }, []);

  // contador de la cinta: avanza con el scroll (como un VHS)
  useEffect(() => {
    const dos = (x) => String(x).padStart(2, "0");
    const f = () => {
      const max = document.documentElement.scrollHeight - window.innerHeight;
      const s = Math.round((max > 0 ? window.scrollY / max : 0) * 5400);   // 90 minutos de cinta
      setCinta(`${dos(Math.floor(s / 3600))}:${dos(Math.floor(s / 60) % 60)}:${dos(s % 60)}`);
    };
    window.addEventListener("scroll", f, { passive: true }); f();
    return () => window.removeEventListener("scroll", f);
  }, []);

  // reloj de la cinta: empieza el 27/11/1998 a las 23:47
  useEffect(() => {
    const inicio = new Date(1998, 10, 27, 23, 47, 0).getTime(), t0 = Date.now();
    const dos = (x) => String(x).padStart(2, "0");
    const id = setInterval(() => {
      const t = new Date(inicio + (Date.now() - t0));
      setHora(`${dos(t.getHours())}:${dos(t.getMinutes())}:${dos(t.getSeconds())}`);
    }, 1000);
    return () => clearInterval(id);
  }, []);

  // glitch: cuando alguien lo pide y de vez en cuando solo
  useEffect(() => {
    const disparar = () => {
      const b = barra.current; if (!b) return;
      b.classList.remove("activa"); void b.offsetWidth; b.classList.add("activa");
      document.querySelectorAll(".glitch").forEach((t) => { t.classList.add("activo"); setTimeout(() => t.classList.remove("activo"), 480); });
    };
    window.addEventListener("mln-glitch", disparar);
    let id;
    const siguiente = () => { id = setTimeout(() => { disparar(); siguiente(); }, 4000 + Math.random() * 7000); };
    siguiente();
    return () => { window.removeEventListener("mln-glitch", disparar); clearTimeout(id); };
  }, []);

  return (
    <>
      <canvas ref={lienzo} className="ruido" aria-hidden="true" />
      <canvas ref={ceniza} className="ceniza" aria-hidden="true" />
      <div className="scanlines" aria-hidden="true" />
      <div className="vineta" aria-hidden="true" />
      <div ref={barra} className="glitch-barra" aria-hidden="true" />
      <div className="hud" aria-hidden="true">
        <div className="hud-rec"><span className="punto" />REC</div>
        <div className="hud-sp">SP <span className="bat">BAT ▮▮▮▯</span></div>
        <div className="hud-fecha"><span>{hora}</span><br />NOV. 27 1998</div>
        <div className="hud-cinta">▶ PLAY {cinta} · CINTA RECUPERADA // SECTOR LA PASTORA</div>
      </div>
    </>
  );
}
