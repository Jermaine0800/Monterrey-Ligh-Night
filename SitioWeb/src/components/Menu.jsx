import { useEffect, useState } from "react";
import { MENU } from "../data.js";

export default function Menu({ sonando, alternar }) {
  const [abierto, setAbierto] = useState(false);
  const [solido, setSolido] = useState(false);

  useEffect(() => {
    const f = () => setSolido(window.scrollY > 60);
    f();
    window.addEventListener("scroll", f, { passive: true });
    return () => window.removeEventListener("scroll", f);
  }, []);

  return (
    <nav className={"menu" + (solido ? " solido" : "")} aria-label="Secciones">
      <a href="#inicio" className="logo">MLN</a>
      <button className="menu-boton" aria-label="Abrir menú" aria-expanded={abierto} onClick={() => setAbierto(!abierto)}>
        {abierto ? "✕" : "☰"}
      </button>
      <ul className={abierto ? "abierto" : ""}>
        {MENU.map(([id, txt]) => (
          <li key={id}><a href={"#" + id} className={id === "jugar" ? "rojo" : ""} onClick={() => setAbierto(false)}>{txt}</a></li>
        ))}
      </ul>
      <button className={"btn-musica" + (sonando ? " on" : "")} onClick={alternar} aria-pressed={sonando} title="Música">
        ♪ {sonando ? "ON" : "OFF"}
      </button>
    </nav>
  );
}
