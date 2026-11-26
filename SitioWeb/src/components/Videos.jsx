import { useRef, useState } from "react";
import { M, CINTAS, CLIPS, LINEA } from "../data.js";
import { glitch, useRevelar } from "../efectos.js";

export function Titulo({ num, children }) {
  const [ref, cls] = useRevelar();
  return <h2 ref={ref} className={"h2 titulo-sec " + cls}><span className="num">{num}</span> <span className="glitch" data-text={typeof children === "string" ? children : ""}>{children}</span></h2>;
}

export function Trailer() {
  const [ref, cls] = useRevelar();
  return (
    <section id="trailer" className="seccion">
      <Titulo num="01">TRÁILER</Titulo>
      <div ref={ref} className={"tele " + cls}>
        <div className="tele-pantalla">
          <video src={M("video/TrailerMonterrey.mp4")} poster={M("img/cine_asomado.jpg")} controls preload="none" playsInline />
        </div>
        <div className="tele-marca mono">TRINITRON-98 · CH 03</div>
      </div>
    </section>
  );
}

// Un clip vertical dentro de un "celular"
function Clip({ c }) {
  const v = useRef(null);
  const [tocando, setTocando] = useState(false);
  const [ref, cls] = useRevelar();
  const alternar = () => {
    const el = v.current;
    if (el.paused) { el.muted = false; el.play().catch(() => {}); } else el.pause();
  };
  return (
    <figure ref={ref} className={"clip " + cls}>
      <div className="cel">
        <video ref={v} src={M("video/" + c.src)} poster={M("img/" + c.poster)} preload="none" playsInline loop
          onPlay={() => setTocando(true)} onPause={() => setTocando(false)} onClick={alternar} />
        {!tocando && <button className="clip-play" onClick={alternar} aria-label={"Reproducir " + c.titulo}>▶</button>}
        <div className="cel-hud mono"><span className="punto" />REC</div>
      </div>
      <figcaption><b className="mono">{c.titulo}</b><span>{c.texto}</span></figcaption>
    </figure>
  );
}

export function Clips() {
  return (
    <section id="clips" className="seccion">
      <Titulo num="02">CLIPS PARA TU FEED</Titulo>
      <p className="intro">Tráilers verticales para TikTok, Reels y Shorts. Tócalos para reproducir. Si los compartes, etiqueta <b>#MonterreyLightNight</b>.</p>
      <div className="clips">{CLIPS.map((c) => <Clip key={c.src} c={c} />)}</div>
    </section>
  );
}

export function Historia() {
  const [cinta, setCinta] = useState(CINTAS[0]);
  const [desbloqueada, setDesbloqueada] = useState(false);
  const video = useRef(null);
  const [r1, c1] = useRevelar();
  const [r2, c2] = useRevelar();

  const elegir = (c) => {
    if (c.spoiler && !desbloqueada) {
      if (!window.confirm("Esta cinta es del FINAL del juego. ¿Seguro que quieres verla? (spoiler)")) return;
      setDesbloqueada(true);
    }
    setCinta(c);
    glitch();
    setTimeout(() => video.current && video.current.play().catch(() => {}), 50);
  };

  return (
    <section id="historia" className="seccion">
      <Titulo num="03">LA HISTORIA</Titulo>
      <div className="dos-col">
        <div ref={r1} className={"documento " + c1}>
          <p className="sello">CONFIDENCIAL</p>
          <p className="doc-titulo">INFORME — LÍNEA K</p>
          <p>27 de noviembre de 1998. Una cuadrilla de inspección sube a las faldas del <b>Cerro de la Silla</b> para revisar la <b>Línea K</b>: un túnel de metro que nunca se terminó.</p>
          <p>El practicante <b>Hernán Camarena</b> lleva la videocámara. El jefe de cuadrilla, <b>Mario</b>, quiere todo grabado. <b>Chuy</b>, el que carga todo, insiste en contar una historia junto a la fogata: hace siete años otra cuadrilla bajó al túnel. Encontraron <b>cinco llaves</b>... y a ninguno de ellos.</p>
          <p>Esa noche, en la Línea K, algo sonríe en la oscuridad. La cámara cae. La señal se pierde.</p>
          <p>Horas después, Hernán despierta en el piso del claro. Es otra vez 27 de noviembre. Es otra vez la misma noche. Un desconocido que se hace llamar <b>El Vigía</b> le ofrece salir del bucle... si junta las cinco llaves.</p>
          <p className="firma">— Encontrada por la policía local semanas después. Se recomienda discreción.</p>
        </div>
        <ol ref={r2} className={"linea-tiempo " + c2}>
          {LINEA.map(([h, t, d, img, alt]) => (
            <li key={h}><span className="hora">{h}</span><b>{t}</b> {d}<img src={M("img/" + img + ".jpg")} alt={alt} loading="lazy" /></li>
          ))}
        </ol>
      </div>

      <h3 className="h3">LAS CINTAS</h3>
      <div className="cintas" role="tablist">
        {CINTAS.map((c) => (
          <button key={c.id} role="tab" aria-selected={cinta.id === c.id}
            className={"cinta" + (cinta.id === c.id ? " activa" : "") + (c.spoiler && !desbloqueada ? " bloqueada" : "")}
            onClick={() => elegir(c)}>{c.spoiler && desbloqueada ? "CINTA 3 · EL PORTAL" : c.nombre}</button>
        ))}
      </div>
      <div className="tele chica">
        <div className="tele-pantalla">
          <video key={cinta.src} ref={video} src={M("video/" + cinta.src)} poster={M("img/" + cinta.poster + ".jpg")} controls preload="none" playsInline />
        </div>
      </div>
    </section>
  );
}
