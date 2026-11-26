import { useCallback, useEffect, useRef, useState } from "react";
import Capas from "./components/Capas.jsx";
import Entrada from "./components/Entrada.jsx";
import Menu from "./components/Menu.jsx";
import Hero from "./components/Hero.jsx";
import { Trailer, Clips, Historia } from "./components/Videos.jsx";
import { Personajes, Lugares } from "./components/Mundo.jsx";
import Manual from "./components/Manual.jsx";
import Galeria from "./components/Galeria.jsx";
import Jugar from "./components/Jugar.jsx";
import Creditos from "./components/Creditos.jsx";
import { M } from "./data.js";

export default function App() {
  const musica = useRef(null);
  const [entro, setEntro] = useState(false);
  const [sonando, setSonando] = useState(false);

  const tocar = useCallback(() => {
    const a = musica.current; if (!a) return;
    a.volume = 0.45;
    a.play().catch(() => {});
  }, []);
  const alternar = useCallback(() => {
    const a = musica.current; if (!a) return;
    if (a.paused) tocar(); else a.pause();
  }, [tocar]);

  // Cuando un video empieza, la música se pausa; al pausar el video, regresa.
  useEffect(() => {
    let volver = false;
    const alPlay = (e) => {
      if (e.target.tagName !== "VIDEO") return;
      document.querySelectorAll("video").forEach((v) => { if (v !== e.target) v.pause(); });
      const a = musica.current;
      if (a && !a.paused) { volver = true; a.pause(); }
    };
    const alPausa = (e) => {
      if (e.target.tagName !== "VIDEO" || !volver) return;
      if (![...document.querySelectorAll("video")].some((v) => !v.paused)) { volver = false; tocar(); }
    };
    document.addEventListener("play", alPlay, true);
    document.addEventListener("pause", alPausa, true);
    document.addEventListener("ended", alPausa, true);
    return () => {
      document.removeEventListener("play", alPlay, true);
      document.removeEventListener("pause", alPausa, true);
      document.removeEventListener("ended", alPausa, true);
    };
  }, [tocar]);

  useEffect(() => { document.body.classList.toggle("sin-entrar", !entro); }, [entro]);

  return (
    <>
      <Capas />
      {!entro && <Entrada alEntrar={(conMusica) => { setEntro(true); if (conMusica) tocar(); }} />}
      <Menu sonando={sonando} alternar={alternar} />
      <main>
        <Hero />
        <Trailer />
        <Clips />
        <Historia />
        <Personajes />
        <Lugares />
        <Manual />
        <Galeria />
        <Jugar />
        <Creditos />
      </main>
      <audio ref={musica} src={M("audio/tema.mp3")} loop preload="none"
        onPlay={() => setSonando(true)} onPause={() => setSonando(false)} />
    </>
  );
}
