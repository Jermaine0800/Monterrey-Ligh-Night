import { useEffect, useState } from "react";
import { TEXTO_ENTRADA } from "../data.js";
import { glitch } from "../efectos.js";

// Pantalla negra de "cinta recuperada" (hace falta un clic para poder poner audio)
export default function Entrada({ alEntrar }) {
  const [n, setN] = useState(0);
  const [saliendo, setSaliendo] = useState(false);

  useEffect(() => {
    if (n >= TEXTO_ENTRADA.length) return;
    const id = setTimeout(() => setN(n + 1), TEXTO_ENTRADA[n] === "\n" ? 260 : 32);
    return () => clearTimeout(id);
  }, [n]);

  const entrar = (conMusica) => {
    if (saliendo) return;
    setN(TEXTO_ENTRADA.length);
    setSaliendo(true);
    glitch();
    setTimeout(() => alEntrar(conMusica), 1100);
  };

  useEffect(() => {
    const tecla = (e) => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); entrar(true); } };
    window.addEventListener("keydown", tecla);
    return () => window.removeEventListener("keydown", tecla);
  });

  return (
    <section className={"entrada" + (saliendo ? " fuera" : "")} aria-label="Entrada">
      <div className="entrada-caja">
        <p className="mono tenue">EVIDENCIA # NL-98-1127</p>
        <p className="maquina">{TEXTO_ENTRADA.slice(0, n)}<span className="cursor-maquina">█</span></p>
        <button className="btn-play" onClick={() => entrar(true)} autoFocus>PLAY ▶</button>
        <button className="btn-link" onClick={() => entrar(false)}>entrar sin sonido</button>
      </div>
    </section>
  );
}
