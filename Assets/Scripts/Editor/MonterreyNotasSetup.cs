using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// En cada lugar de llave hay SIEMPRE dos notas juntas, sobre una caja a la entrada:
///  1) "PISTAS DE AYER": tu propia letra, de un bucle que no recuerdas.
///  2) Una nota de alguien que ya estuvo en el bucle, con instrucciones para conseguir la llave.
/// Al leer las dos (o a los ~4 minutos) algo se hace presente (ver SitioLlave.Presencia).
/// Se llama al final del menú 14 (ambientación).
/// </summary>
public static class MonterreyNotasSetup
{
    struct Par { public string tAyer, ayer, tBucle, bucle; }

    static Par Textos(SitioLlave s)
    {
        switch (s)
        {
            case SitioCampamento _:
                return new Par
                {
                    tAyer = "PISTAS DE AYER (mi letra)",
                    ayer = "No me acuerdo de haber escrito esto, pero es mi letra.\n\n" +
                           "· La llave NO está dentro de las casas de campaña. Ya busqué tres veces.\n" +
                           "· Brilla y gira. Se ve desde lejos si te alejas de la fogata.\n" +
                           "· El radio dice que no es el guía. No sé qué significa.\n" +
                           "· Cuando terminé de leer esto, algo salió de entre los árboles. CORRE hacia la luz, no hacia el bosque.",
                    tBucle = "MARIO — EQUIPO DE EXCURSIÓN",
                    bucle = "Si estás leyendo esto, ya estás en el bucle. Yo llevo 9 noches.\n\n" +
                            "1. Rodea el campamento por fuera, por la orilla de los árboles. La llave está tirada cerca de una bota.\n" +
                            "2. Tómala con [A / E]. Va directo a tu maletín.\n" +
                            "3. Hay vendas y un tubo por aquí. Agárralos: el tubo sí les duele.\n" +
                            "4. En cuanto terminas de leer, ya saben que estás aquí. No te quedes quieto.\n\n— M."
                };
            case SitioPanteon _:
                return new Par
                {
                    tAyer = "PISTAS DE AYER (mi letra)",
                    ayer = "· Las velas de las tumbas: hay que prenderlas TODAS. Con una que falte, la cripta no abre.\n" +
                           "· La pala está junto a una tumba abierta. Llévatela.\n" +
                           "· Dentro de la cripta no hay a dónde correr. Entra, toma la llave y sal sin voltear.\n" +
                           "· Ayer me agarró entre las tumbas. Me escondí detrás de la cripta grande y funcionó.",
                    tBucle = "DOÑA CHAYO — VELADORA DEL PANTEÓN",
                    bucle = "Mijo, a todos los que suben les pasa lo mismo.\n\n" +
                            "Prende cada vela con [A / E], una por tumba. Cuando estén todas, la reja de la cripta se abre sola.\n" +
                            "La llave está adentro, sobre el ataúd.\n" +
                            "Si oyes que te cantan, no contestes. Escóndete y deja que pase.\n\n— Rosario, que ya no sabe cuántas noches lleva aquí."
                };
            case SitioTunel _:
                return new Par
                {
                    tAyer = "PISTAS DE AYER (mi letra)",
                    ayer = "· El muro de concreto tiene una marca pintada. Alúmbrala con la linterna [X / F] y se abre.\n" +
                           "· Abajo hay una estación que nunca terminaron. La caseta del vigilante primero.\n" +
                           "· El casillero de Chuy. Ahí está.\n" +
                           "· Al salir corre. NO te detengas en las escaleras.",
                    tBucle = "BITÁCORA — CUADRILLA 3, LÍNEA K",
                    bucle = "Somos 7. Bueno, éramos.\n\n" +
                            "Instrucciones para el que siga:\n" +
                            "1. Linterna a la marca del muro (EXPEDIENTE 08). Entra.\n" +
                            "2. Revisa la caseta: el monitor te enseña qué viene.\n" +
                            "3. Abre el casillero [A / E] y toma la llave.\n" +
                            "4. Sal del túnel. Ya. Lo que vive ahí abajo no se cansa.\n\n— Ing. Treviño"
                };
            case SitioCasa _:
                return new Par
                {
                    tAyer = "PISTAS DE AYER (mi letra)",
                    ayer = "· La llave está en el cuarto del fondo, frente al espejo. No mires el reflejo mucho tiempo.\n" +
                           "· En cuanto la tomas, la puerta de enfrente se azota y se va la luz.\n" +
                           "· El ropero de la recámara grande. Métete AHÍ. Debajo de la cama también sirve.\n" +
                           "· La mecedora se mueve sola. No es nada. Creo.",
                    tBucle = "FAMILIA GARZA — RECADO EN LA PUERTA",
                    bucle = "Al que entre a nuestra casa:\n\n" +
                            "1. Revisa todos los cuartos: el pasillo, la sala, la cocina, el cuarto de la niña y el del fondo.\n" +
                            "2. La llave está en el tocador del fondo. Tómala con [A / E].\n" +
                            "3. Antes de tomarla, ubica el ropero. Vas a tener 3 segundos.\n" +
                            "4. Escondido, no te muevas aunque lo oigas respirar del otro lado.\n" +
                            "5. En la cocina dejamos un machete. Afuera, junto a la leña, un hacha.\n\nNosotros ya no podemos salir. Tú sí."
                };
            case SitioMirador _:
                return new Par
                {
                    tAyer = "PISTAS DE AYER (mi letra)",
                    ayer = "· La baliza roja. La llave cuelga de ahí.\n" +
                           "· Con la quinta llave vuelve al claro: aparece una puerta.\n" +
                           "· La niebla se pone roja. No es sangre, es la luz. (¿o sí?)\n" +
                           "· Esta vez llegué hasta aquí. Si lees esto, llegaste más lejos que yo.",
                    tBucle = "PLACA RAYADA — \"LA LUZ ROJA MIENTE\"",
                    bucle = "Para el que llegue al mirador:\n\n" +
                            "1. Sube hasta la baliza y toma la llave [A / E].\n" +
                            "2. Detrás de la banca dejé una varilla. Úsala si te alcanzan.\n" +
                            "3. Baja SIN DETENERTE hasta el claro de donde saliste.\n" +
                            "4. Pon las cinco llaves en la puerta. No mires atrás cuando cruces.\n\n— alguien que ya lo intentó 14 veces"
                };
        }
        return new Par { tAyer = "PISTAS DE AYER", ayer = "...", tBucle = "NOTA", bucle = "..." };
    }

    public static void Colocar(MisionManager mm)
    {
        Material caja = MonterreyAmbientacionSetup.Mat("CajaNotas", null, new Color(0.28f, 0.21f, 0.14f));
        foreach (SitioLlave s in mm.sitios)
        {
            if (s == null) continue;
            // Quita las notas sueltas del lugar (con su hoja y su luz) y las de una corrida anterior
            Transform viejo = s.transform.Find("NotasDelLugar");
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
            foreach (NotaLeible n in s.GetComponentsInChildren<NotaLeible>(true))
            {
                Transform padre = n.transform.parent;
                Vector3 p = n.transform.position;
                List<GameObject> borrar = new List<GameObject> { n.gameObject };
                if (padre != null)
                    foreach (Transform h in padre)
                        if (h != n.transform && (h.name == "Nota" || h.GetComponent<Light>() != null) && Vector3.Distance(h.position, p) < 0.7f)
                            borrar.Add(h.gameObject);
                foreach (GameObject g in borrar) Object.DestroyImmediate(g);
            }

            // Caja a la entrada del lugar, entre el lugar y por donde llega el jugador
            Transform t = new GameObject("NotasDelLugar").transform;
            t.SetParent(s.transform, true);
            Vector3 hacia = (s.puntoVigia != null ? s.puntoVigia.position : s.transform.position + s.transform.forward * 10f) - s.transform.position;
            hacia.y = 0;
            float d = Mathf.Clamp(s.radioLlegada * 0.6f, 4f, 9f);
            if (s is SitioCasa) d = 9f;
            Vector3 c = Mundo.EnSuelo(s.transform.position + hacia.normalized * d + Vector3.Cross(Vector3.up, hacia.normalized) * 1.5f);
            Quaternion rot = Quaternion.LookRotation(-hacia.normalized);
            t.position = c; t.rotation = rot;
            MonterreyAmbientacionSetup.Cubo(t, "CajaNotas", c + Vector3.up * 0.25f, new Vector3(1.1f, 0.5f, 0.6f), rot, caja, true);
            Vector3 arriba = c + Vector3.up * 0.52f;
            Vector3 lado = rot * Vector3.right * 0.28f;
            Par par = Textos(s);
            s.notaAyer = MonterreyAmbientacionSetup.Nota(t, arriba - lado, par.tAyer, par.ayer, "Nota");
            s.notaBucle = MonterreyAmbientacionSetup.Nota(t, arriba + lado, par.tBucle, par.bucle, "Nota");
            // Una veladora encendida junto a las notas para que se vean desde lejos
            Light l = MonterreyAmbientacionSetup.Luz(t, c + Vector3.up * 1.1f, new Color(1f, 0.75f, 0.45f), 5f, 1.8f, true);
            l.name = "LuzNotas";
            s.esperaMonstruo = 240f;
            EditorUtility.SetDirty(s);
        }
        Debug.Log("[Monterrey] Notas: 2 por lugar (pistas de ayer + instrucciones del bucle).");
    }
}
