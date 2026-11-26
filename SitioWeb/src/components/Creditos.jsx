import { CREDITOS } from "../data.js";

export default function Creditos() {
  return (
    <footer className="creditos">
      <p className="titulo-chico">MONTERREY LIGHT NIGHT</p>
      <p>Proyecto universitario · Unity 6 · Blender · 2026</p>
      <details>
        <summary>Créditos de terceros</summary>
        <ul>{CREDITOS.map((c) => <li key={c}>{c}</li>)}</ul>
      </details>
      <p className="tenue mono">FIN DE LA CINTA ■</p>
    </footer>
  );
}
