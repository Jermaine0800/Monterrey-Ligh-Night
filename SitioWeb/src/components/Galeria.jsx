import { useEffect, useState } from "react";
import { M, FOTOS } from "../data.js";
import { glitch, useRevelar } from "../efectos.js";
import { Titulo } from "./Videos.jsx";

function Foto({ f, abrir }) {
  const [ref, cls] = useRevelar();
  const [rota, setRota] = useState(false);
  if (rota) return null;
  return (
    <figure ref={ref} className={cls} onClick={() => abrir(f)}>
      <img src={M("img/" + f[0] + ".jpg")} alt={f[1]} loading="lazy" onError={() => setRota(true)} />
      <figcaption>{f[1]}</figcaption>
    </figure>
  );
}

export default function Galeria() {
  const [i, setI] = useState(-1);
  const abrir = (f) => { setI(FOTOS.indexOf(f)); glitch(); };

  useEffect(() => {
    if (i < 0) return;
    const tecla = (e) => {
      if (e.key === "Escape") setI(-1);
      if (e.key === "ArrowRight") setI((i + 1) % FOTOS.length);
      if (e.key === "ArrowLeft") setI((i - 1 + FOTOS.length) % FOTOS.length);
    };
    window.addEventListener("keydown", tecla);
    return () => window.removeEventListener("keydown", tecla);
  }, [i]);

  return (
    <section id="galeria" className="seccion">
      <Titulo num="07">GALERÍA</Titulo>
      <div className="galeria">{FOTOS.map((f) => <Foto key={f[0]} f={f} abrir={abrir} />)}</div>
      {i >= 0 && (
        <div className="visor" role="dialog" aria-label={FOTOS[i][1]} onClick={() => setI(-1)}>
          <img src={M("img/" + FOTOS[i][0] + ".jpg")} alt={FOTOS[i][1]} />
          <p className="mono">{FOTOS[i][1]} · {i + 1}/{FOTOS.length} · toca para cerrar</p>
        </div>
      )}
    </section>
  );
}
