import { useState } from "react";
import { M, CONTROLES, CONSEJOS } from "../data.js";
import { Titulo } from "./Videos.jsx";

const PESTANAS = [["objetivo", "Objetivo"], ["controles", "Controles"], ["sobrevivir", "Sobrevivir"], ["maletin", "Maletín"], ["consejos", "Consejos"]];

function Controles() {
  const [tipo, setTipo] = useState("xbox");
  return (
    <>
      <div className="selector-controles">
        <button className={"sel" + (tipo === "xbox" ? " activa" : "")} onClick={() => setTipo("xbox")}>CONTROL XBOX</button>
        <button className={"sel" + (tipo === "teclado" ? " activa" : "")} onClick={() => setTipo("teclado")}>TECLADO Y RATÓN</button>
        <button className={"sel" + (tipo === "celular" ? " activa" : "")} onClick={() => setTipo("celular")}>CELULAR</button>
      </div>
      <table className="controles">
        <tbody>
          {CONTROLES[tipo].map(([b, c, t]) => (
            <tr key={b}><td><span className={"boton-glifo " + c}>{b}</span></td><td>{t}</td></tr>
          ))}
        </tbody>
      </table>
      <p className="tenue mono">En la versión web: haz clic en el juego para capturar el ratón · Esc lo suelta · ayuda de controles con G.</p>
    </>
  );
}

export default function Manual() {
  const [p, setP] = useState("objetivo");
  return (
    <section id="manual" className="seccion">
      <Titulo num="06">CÓMO SE JUEGA</Titulo>
      <div className="pestanas" role="tablist">
        {PESTANAS.map(([id, txt]) => (
          <button key={id} role="tab" aria-selected={p === id} className={"pestana" + (p === id ? " activa" : "")} onClick={() => setP(id)}>{txt}</button>
        ))}
      </div>

      <div className="panel activo" role="tabpanel" key={p}>
        {p === "objetivo" && (
          <div className="dos-col">
            <div>
              <h4 className="rojo">SAL DEL BUCLE</h4>
              <ol className="lista">
                <li>Sigue al <b>Vigía</b>: te muestra el camino a cada lugar. El mapa redondo (arriba a la derecha) marca las llaves con rombos numerados.</li>
                <li>En cada lugar lee las <b>dos notas</b> juntas: te dicen qué hacer para conseguir la llave.</li>
                <li>Cumple el reto del lugar (prender velas, alumbrar la marca, abrir el casillero...) y toma la <b>llave</b>.</li>
                <li>Con las <b>cinco llaves</b> vuelve al claro donde despertaste: ahí está la <b>puerta del bucle</b>.</li>
              </ol>
              <p>Si mueres, la cinta se rebobina: <b>las llaves que ya tienes se quedan contigo</b>.</p>
            </div>
            <img className="captura" src={M("img/hud.jpg")} alt="HUD del juego" loading="lazy" />
          </div>
        )}
        {p === "controles" && <Controles />}
        {p === "sobrevivir" && (
          <div className="dos-col">
            <div>
              <h4 className="rojo">ESCÓNDETE</h4>
              <p>Arbustos, roperos, casilleros, debajo de la cama, detrás de un ataúd. Acércate y presiona <b>[A / E]</b>. Mientras estés escondido no te pueden ver... a menos que te hayan visto entrar.</p>
              <h4 className="rojo">PELEA (SI NO HAY DE OTRA)</h4>
              <p>Los monstruos aguantan <b>5 a 8 golpes</b>. Cuando los golpeas se tambalean y siguen. Si les quitas casi la mitad de la vida, huyen.</p>
              <p><b>Antes de la quinta llave no mueren del todo</b>: se hunden en la tierra y regresan. Con las cinco llaves, sí mueren para siempre.</p>
              <h4 className="rojo">ACCIONES LARGAS (MANTÉN [A / E])</h4>
              <p>Velas, fogata, cajas de fusibles y el carro se hacen <b>en pasos</b>: mantén el botón y mira la barra. Si lo sueltas o te mueves en el paso delicado, empiezas otra vez.</p>
              <h4 className="rojo">EL METRO</h4>
              <p>Al entrar al vagón se cierra la Línea K y aparece <b>La Sonriente</b> con su barra de vida. Prende las dos cajas de fusibles: con luz le haces hasta el doble. Cuando la lastimas lo suficiente huye y aparece la llave.</p>
              <h4 className="rojo">CONDICIÓN FÍSICA</h4>
              <p>Correr y golpear cansan. Si te quedas <b>sin aire</b> caminas más lento, no puedes golpear y jadeas (te pueden oír). Herido, te cansas antes.</p>
            </div>
            <img className="captura" src={M("img/ropero.jpg")} alt="Escondido en un ropero" loading="lazy" />
          </div>
        )}
        {p === "maletin" && (
          <div className="dos-col">
            <div>
              <h4 className="rojo">EL MALETÍN</h4>
              <p>Lo tienes cuando encuentras la <b>mochila</b>. Ábrelo con <b>[Y / Tab]</b>.</p>
              <ul className="lista">
                <li><b>Curaciones</b> (agua +20, vendas +30, botiquín +55): úsalas desde el maletín o rápido con <b>[▲ / H]</b>.</li>
                <li><b>Armas</b> (pala, tubo, varilla, crucifijo, machete, hacha): cada una tiene golpes limitados y se rompe. Cambia con <b>[◀ ▶ / rueda]</b>.</li>
                <li><b>Llaves del bucle</b>: una sola casilla, "n / 5".</li>
                <li><b>Notas</b>: todas en una casilla; vuelve a leerlas cuando quieras.</li>
              </ul>
              <p>La <b>condición</b> (Bien · Precaución · Peligro) se ve en el electrocardiograma.</p>
            </div>
            <img className="captura" src={M("img/maletin.jpg")} alt="Maletín" loading="lazy" />
          </div>
        )}
        {p === "consejos" && <ul className="consejos">{CONSEJOS.map((c) => <li key={c}>{c}</li>)}</ul>}
      </div>
      <p className="centro"><a className="btn" href={M("Manual_Monterrey_Light_Night.docx")} download>⬇ MANUAL DE USUARIO COMPLETO (WORD)</a></p>
    </section>
  );
}
