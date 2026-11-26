import { CONFIG } from "../config.js";
import { ligaSegura, useRevelar } from "../efectos.js";
import { Titulo } from "./Videos.jsx";

function Descarga({ icono, titulo, texto, liga, peso, pronto, accion = "⬇ DESCARGAR · ", nueva = false }) {
  const [ref, cls] = useRevelar();
  const url = ligaSegura(liga);
  const contenido = (
    <>
      <span className="opcion-icono">{icono}</span>
      <b>{titulo}</b>
      <span>{texto}</span>
      <span className="peso mono">{url ? accion + peso : pronto}</span>
    </>
  );
  return url
    ? <a ref={ref} className={"opcion " + cls} href={url} {...(nueva ? { target: "_blank", rel: "noopener noreferrer" } : { download: true })}>{contenido}</a>
    : <div ref={ref} className={"opcion apagada " + cls}>{contenido}</div>;
}

export default function Jugar() {
  return (
    <section id="jugar" className="seccion jugar">
      <Titulo num="08">DESCARGAR Y JUGAR</Titulo>
      <p className="intro">Gratis. Descarga el .zip para tu computadora, o juega directo en el navegador desde tu iPhone, Android o PC.</p>
      <div className="opciones-jugar">
        <Descarga icono="💾" titulo="WINDOWS" liga={CONFIG.descargaWindows} peso="116 MB (.zip)"
          texto="Descomprime el .zip y abre MonterreyLightNight.exe. Control de Xbox o teclado y ratón." pronto="Muy pronto" />
        <Descarga icono="📱" titulo="iPHONE · ANDROID · NAVEGADOR" liga={CONFIG.jugarWeb} peso="sin instalar (~70 MB)" accion="▶ JUGAR AHORA · " nueva
          texto="Ábrelo en Safari o Chrome, pon el celular horizontal y usa los controles en pantalla. También sirve un control Bluetooth." pronto="Muy pronto" />
      </div>
      <div className="requisitos mono">
        <p><b>WINDOWS:</b> Windows 10/11 64 bits · 4 GB RAM · gráfica con DirectX 11 · 400 MB libres</p>
        <p><b>NAVEGADOR:</b> iPhone con iOS 16 o más nuevo (Safari) · Android con Chrome · 4 GB RAM recomendados · buena señal o Wi-Fi para la primera carga</p>
      </div>
    </section>
  );
}
