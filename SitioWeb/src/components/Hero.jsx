import { useEffect, useRef } from "react";
import { M } from "../data.js";

// Portada con efecto de linterna: la luz sigue al ratón o al dedo
export default function Hero() {
  const sec = useRef(null), osc = useRef(null);

  useEffect(() => {
    const s = sec.current, o = osc.current;
    const mover = (x, y) => {
      const r = s.getBoundingClientRect();
      o.style.setProperty("--x", x - r.left + "px");
      o.style.setProperty("--y", y - r.top + "px");
    };
    const raton = (e) => mover(e.clientX, e.clientY);
    const dedo = (e) => { const t = e.touches[0]; if (t) mover(t.clientX, t.clientY); };
    s.addEventListener("mousemove", raton);
    s.addEventListener("touchmove", dedo, { passive: true });
    // la linterna falla de vez en cuando
    const id = setInterval(() => {
      if (Math.random() > 0.35) return;
      o.classList.add("falla");
      setTimeout(() => o.classList.remove("falla"), 260);
    }, 4200);
    return () => { s.removeEventListener("mousemove", raton); s.removeEventListener("touchmove", dedo); clearInterval(id); };
  }, []);

  return (
    <section id="inicio" className="hero" ref={sec}>
      <div className="hero-fondo" style={{ backgroundImage: `url(${M("img/portada.jpg")})` }} />
      <div className="hero-oscuridad" ref={osc} />
      <div className="hero-texto">
        <p className="mono rojo parpadea">⚠ CINTA DAÑADA — REPRODUCIR CON DISCRECIÓN</p>
        <h1 className="titulo glitch" data-text="MONTERREY LIGHT NIGHT">MONTERREY LIGHT NIGHT</h1>
        <p className="lema">Siete entraron a la <b>Línea K</b>. Ninguno salió.<br />Esta noche, tú tampoco.</p>
        <div className="botones">
          <a href="#trailer" className="btn">▶ VER TRÁILER</a>
          <a href="#jugar" className="btn btn-rojo">DESCARGAR GRATIS</a>
        </div>
        <p className="mono tenue etiquetas">Survival horror · Estética PS1 / VHS · Windows y Android (.zip) · iPhone en el navegador · Control Xbox, teclado o táctil</p>
      </div>
      <a href="#trailer" className="bajar mono">▼ SIGUE GRABANDO</a>
    </section>
  );
}
