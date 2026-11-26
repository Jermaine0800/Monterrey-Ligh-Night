import { useState } from "react";
import { M, PERSONAJES, LUGARES } from "../data.js";
import { useRevelar } from "../efectos.js";
import { Titulo } from "./Videos.jsx";

function Tarjeta({ p }) {
  const [abierta, setAbierta] = useState(false);
  const [ref, cls] = useRevelar();
  // inclinación 3D siguiendo el ratón (como una foto que tienes en la mano)
  const mover = (e) => {
    const r = e.currentTarget.getBoundingClientRect();
    const x = (e.clientX - r.left) / r.width - 0.5, y = (e.clientY - r.top) / r.height - 0.5;
    e.currentTarget.style.setProperty("--rx", (-y * 10).toFixed(2) + "deg");
    e.currentTarget.style.setProperty("--ry", (x * 12).toFixed(2) + "deg");
  };
  const soltar = (e) => { e.currentTarget.style.setProperty("--rx", "0deg"); e.currentTarget.style.setProperty("--ry", "0deg"); };
  return (
    <article ref={ref} className={`tarjeta ${p.monstruo ? "monstruo " : ""}${abierta ? "abierta " : ""}${cls}`}
      onClick={() => setAbierta(!abierta)} onMouseMove={mover} onMouseLeave={soltar}
      tabIndex={0} onKeyDown={(e) => e.key === "Enter" && setAbierta(!abierta)}>
      <img src={M("img/" + p.img + ".jpg")} alt={p.nombre} loading="lazy" />
      <div className="tarjeta-texto">
        <p className="mono rojo">{p.exp}</p>
        <h4>{p.nombre}</h4>
        <p>{p.texto}</p>
        {p.hace && (
          <ul className="hace">{p.hace.map((h) => <li key={h}>{h}</li>)}</ul>
        )}
        {p.sobrevive && <p className="sobrevive mono">CÓMO SOBREVIVIR: {p.sobrevive}</p>}
      </div>
    </article>
  );
}

export function Personajes() {
  return (
    <section id="personajes" className="seccion">
      <Titulo num="04">EXPEDIENTES</Titulo>
      <p className="intro">La cuadrilla, el desconocido... y lo que vive en el cerro. Pasa el cursor (o toca) para abrir cada expediente.</p>
      <div className="tarjetas">{PERSONAJES.map((p) => <Tarjeta key={p.nombre} p={p} />)}</div>
    </section>
  );
}

function Lugar({ l }) {
  const [ref, cls] = useRevelar();
  const [pin, img, nombre, texto] = l;
  return (
    <div ref={ref} className={"lugar " + cls}>
      <span className="pin"><span>{pin}</span></span>
      <img src={M("img/" + img + ".jpg")} alt={nombre} loading="lazy" />
      <h4>{nombre}</h4>
      <p>{texto}</p>
    </div>
  );
}

export function Lugares() {
  return (
    <section id="lugares" className="seccion">
      <Titulo num="05">LAS CINCO LLAVES</Titulo>
      <p className="intro">Cada lugar guarda una llave, dos notas (pistas de ayer + instrucciones de alguien que ya estuvo en el bucle), un escondite y algo con qué defenderte.</p>
      <div className="lugares">{LUGARES.map((l) => <Lugar key={l[1]} l={l} />)}</div>
    </section>
  );
}
