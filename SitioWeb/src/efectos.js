// Utilidades compartidas
import { useEffect, useRef, useState } from "react";

export function glitch() { window.dispatchEvent(new Event("mln-glitch")); }

// Aparece al hacer scroll
export function useRevelar() {
  const ref = useRef(null);
  const [visto, setVisto] = useState(false);
  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    if (!("IntersectionObserver" in window)) { setVisto(true); return; }
    const io = new IntersectionObserver(([e]) => { if (e.isIntersecting) { setVisto(true); io.disconnect(); } }, { threshold: 0.12 });
    io.observe(el);
    return () => io.disconnect();
  }, []);
  return [ref, visto ? "revelar visto" : "revelar"];
}

export function usaCelular() {
  const [cel, setCel] = useState(() => typeof window !== "undefined" && window.matchMedia("(max-width: 860px), (pointer: coarse)").matches);
  useEffect(() => {
    const mq = window.matchMedia("(max-width: 860px), (pointer: coarse)");
    const f = () => setCel(mq.matches);
    mq.addEventListener("change", f);
    return () => mq.removeEventListener("change", f);
  }, []);
  return cel;
}

// Solo deja pasar ligas http(s) o rutas propias (evita "javascript:" si alguien edita la config)
export function ligaSegura(url) {
  if (!url) return "";
  if (url.startsWith("/")) return url;
  try { const u = new URL(url); return u.protocol === "https:" ? u.href : ""; } catch { return ""; }
}
