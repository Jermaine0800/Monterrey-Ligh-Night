import { CONFIG } from "../config.js";
import { ligaSegura, useRevelar } from "../efectos.js";
import { Titulo } from "./Videos.jsx";

function Descarga({ icono, titulo, texto, liga, peso, pronto }) {
  const [ref, cls] = useRevelar();
  const url = ligaSegura(liga);
  const contenido = (
    <>
      <span className="opcion-icono">{icono}</span>
      <b>{titulo}</b>
      <span>{texto}</span>
      <span className="peso mono">{url ? "⬇ DESCARGAR · " + peso : pronto}</span>
    </>
  );
  return url
    ? <a ref={ref} className={"opcion " + cls} href={url} download>{contenido}</a>
    : <div ref={ref} className={"opcion apagada " + cls}>{contenido}</div>;
}

export default function Jugar() {
  return (
    <section id="jugar" className="seccion jugar">
      <Titulo num="08">DESCARGAR Y JUGAR</Titulo>
      <p className="intro">Gratis. Descarga el .zip, descomprímelo y juega sin internet en tu computadora.</p>
      <div className="opciones-jugar">
        <Descarga icono="💾" titulo="WINDOWS" liga={CONFIG.descargaWindows} peso="116 MB (.zip)"
          texto="Descomprime el .zip y abre MonterreyLightNight.exe. Control de Xbox o teclado y ratón." pronto="Muy pronto" />
      </div>
      <div className="requisitos mono">
        <p><b>WINDOWS:</b> Windows 10/11 64 bits · 4 GB RAM · gráfica con DirectX 11 · 400 MB libres</p>
      </div>
    </section>
  );
}
