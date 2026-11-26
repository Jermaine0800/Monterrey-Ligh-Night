// Todo el texto del sitio en un solo lugar (React lo escapa: no se inyecta HTML).
export const M = (p) => "/media/" + p;

export const TEXTO_ENTRADA =
  "CINTA VHS-C  ·  EVIDENCIA 07/98\nRECUPERADA EN: CERRO DE LA SILLA, N.L.\nCUADRILLA DE INSPECCIÓN: 3 INTEGRANTES\nSOBREVIVIENTES CONFIRMADOS: 0\n\nEl contenido de esta cinta no fue editado.";

export const MENU = [
  ["trailer", "Tráiler"], ["clips", "Clips"], ["historia", "Historia"], ["personajes", "Expedientes"],
  ["lugares", "Las 5 llaves"], ["manual", "Cómo se juega"], ["galeria", "Galería"], ["jugar", "Descargar"],
];

export const LINEA = [
  ["6:48 PM", "Campamento base.", "Llegas como el nuevo. Escuchas la leyenda.", "cine_campamento", "Atardecer en el campamento"],
  ["7:40 PM", "Algo se asoma", "entre los árboles. Nadie más lo ve.", "cine_asomado", "El Asomado detrás de un tronco"],
  ["11:31 PM", "Línea K.", "Las lámparas parpadean. Alguien te sonríe al fondo.", "cine_sonriente", "La Sonriente en el túnel"],
  ["??:??", "Despiertas", "en el piso. El bucle empieza. Cinco llaves. Una puerta.", "cine_portal", "La puerta del bucle"],
];

export const CINTAS = [
  { id: 1, nombre: "CINTA 1 · EL NUEVO", src: "CampamentoTarde.mp4", poster: "cine_atardecer" },
  { id: 2, nombre: "CINTA 2 · LÍNEA K", src: "IntroLineaK.mp4", poster: "cine_tunel" },
  { id: 3, nombre: "CINTA 3 · ██████ (SPOILER)", src: "PortalFinal.mp4", poster: "cine_monstruos", spoiler: true },
];

// Clips verticales (9:16) para TikTok / Reels / Shorts
export const CLIPS = [
  { src: "tiktok_1_no_voltees.mp4", poster: "tiktok_1.jpg", titulo: "NO VOLTEES", texto: "La Sonriente solo se mueve cuando no la ves." },
  { src: "tiktok_2_el_asomado.mp4", poster: "tiktok_2.jpg", titulo: "EL ASOMADO", texto: "Si lo ignoras demasiado tiempo... corre." },
  { src: "tiktok_3_cinco_llaves.mp4", poster: "tiktok_3.jpg", titulo: "5 LLAVES", texto: "Cinco lugares. Una puerta. Un bucle." },
];

export const PERSONAJES = [
  { img: "hud", exp: "EXP. 01 · TÚ", nombre: "HERNÁN CAMARENA", texto: "Practicante. Carga la videocámara \"con pila nueva\". Lo único que tiene para defenderse: una linterna, lo que encuentre en el camino y las ganas de salir.",
    hace: ["Corre (cansa: vigila la condición)", "Se esconde en roperos, ataúdes, arbustos y casas de campaña", "Pelea con lo que encuentre: tubo, pala, hacha, machete…", "Prende velas, fogatas, fusibles y hasta un carro manteniendo el botón"] },
  { img: "cine_campamento", exp: "EXP. 02 · 03", nombre: "MARIO Y CHUY", texto: "El jefe de cuadrilla que no cree en leyendas y el que las cuenta junto a la fogata. Esa noche se separaron en la Línea K. ¿Siguen ahí?",
    hace: ["Dejaron notas en cada lugar: léelas en pareja", "Sus voces se oyen en las cintas recuperadas"] },
  { img: "vigia2", exp: "EXP. 04 · ¿ALIADO?", nombre: "EL VIGÍA", texto: "Te espera en la entrada de cada lugar. Te señala el camino, te da pistas... y se ríe demasiado. \"Yo te espero del otro lado. Siempre espero.\"",
    hace: ["Aparece al llegar a cada punto y te explica qué hacer", "Señala hacia la siguiente llave", "Nunca te ataca… todavía"] },
  { img: "sonriente", exp: "EXP. 05 · PELIGRO", nombre: "LA SONRIENTE", monstruo: true, texto: "Solo se mueve cuando no la ves. Cada vez que volteas, está más cerca. No le regreses la sonrisa.",
    hace: ["Avanza a saltos cuando le das la espalda; mirándola se congela", "La linterna en la cara la espanta un rato", "Si llega a tu cara: susto y daño", "Es la jefa de la Línea K: te encierra en el metro hasta que la lastimes. Con las luces prendidas le haces el doble"],
    sobrevive: "Mírala, alúmbrala y no le des la espalda. En el metro: primero las cajas de fusibles." },
  { img: "asomado", exp: "EXP. 06 · PELIGRO", nombre: "EL ASOMADO", monstruo: true, texto: "Se asoma detrás de los troncos. Si lo ves, se esconde y aparece en otro árbol, más cerca. Si lo ignoras demasiado tiempo... corre hacia ti.",
    hace: ["Se asoma entre los árboles y cambia de tronco cuando lo miras", "Si lo ignoras, corre a toda velocidad hacia ti", "Aguanta más golpes que La Sonriente (unos 7)", "Lo espanta la alarma del carro del campamento"],
    sobrevive: "No lo pierdas de vista. Si corre, escóndete o pégale con algo pesado." },
];

export const LUGARES = [
  ["1", "campamento", "EL CAMPAMENTO", "La fogata está apagada: vuelve a prenderla (juntar ramas, raspar el cerillo, soplar la brasa) y el fuego deja ver la llave. Junto al campamento hay un carro viejo: puentéalo y su alarma espanta a los monstruos… un rato."],
  ["2", "panteon", "EL PANTEÓN", "Prende las tres velas de las tumbas con calavera (cerillo, cubrir la flama, la mecha). Con cada vela algo despierta. Con la tercera, la cripta se abre sola."],
  ["3", "anden", "LÍNEA K", "Alumbra la marca roja del muro. Abajo hay un andén y un vagón de metro abandonado. Entra… y todo se cierra. No sales hasta lastimar a La Sonriente. Prende las dos cajas de fusibles: con luz le haces el doble."],
  ["4", "casa", "LA CASA DE LOS GARZA", "La familia se fue en el 98. Dejaron la mesa puesta. La llave está frente al espejo. Ubica el ropero antes de tomarla."],
  ["5", "mirador", "EL MIRADOR", "Sigue la baliza roja hasta lo más alto. \"La luz roja miente.\""],
  ["+", "estacion", "ESTACIÓN DE GUARDABOSQUES", "Opcional. Cabaña, bodega y una torre desde donde se ven las cinco luces. Provisiones y un hacha."],
];

export const CONTROLES = {
  xbox: [
    ["Stick izq.", "", "Caminar · L3 / LB para correr (gasta condición)"],
    ["Stick der.", "", "Mirar · R3 cambia cámara 1ª / 3ª persona"],
    ["A", "a", "Acción: recoger, leer, abrir, esconderse"],
    ["B", "b", "Agacharse / salir del escondite"],
    ["X", "x", "Prender o apagar la linterna"],
    ["Y", "y", "Abrir el maletín (inventario)"],
    ["RT", "", "Golpear con el arma"],
    ["◀ ▶", "", "Cambiar de arma"],
    ["▲", "", "Curarse (usa un botiquín)"],
    ["Mantén A", "a", "Acciones largas: prender velas, fogata, fusibles, puentear el carro"],
    ["Start", "", "Pausa · View: ver la ayuda dentro del juego"],
  ],
  teclado: [
    ["W A S D", "", "Caminar · Shift para correr (gasta condición)"],
    ["Ratón", "", "Mirar · V cambia cámara 1ª / 3ª persona"],
    ["E", "", "Acción: recoger, leer, abrir, esconderse"],
    ["C / Ctrl", "", "Agacharse / salir del escondite"],
    ["F", "", "Prender o apagar la linterna"],
    ["Tab", "", "Abrir el maletín (inventario)"],
    ["Clic izq.", "", "Golpear con el arma"],
    ["Rueda / Q R", "", "Cambiar de arma"],
    ["H", "", "Curarse (usa un botiquín)"],
    ["Mantén E", "", "Acciones largas: prender velas, fogata, fusibles, puentear el carro"],
    ["Tab + ratón", "", "Maletín: elige con el ratón, la rueda o las teclas 1–9"],
    ["Esc", "", "Pausa · F1 o G: ver la ayuda dentro del juego"],
  ],
  celular: [
    ["Stick izq.", "", "Caminar (en pantalla, abajo a la izquierda)"],
    ["Arrastrar", "", "Mirar: arrastra el dedo en la mitad derecha"],
    ["A", "a", "Acción · mantén para velas, fogata, fusibles y el carro"],
    ["B", "b", "Agacharse / salir del escondite"],
    ["X", "x", "Linterna"],
    ["Y", "y", "Maletín"],
    ["GOLPE", "", "Golpear con el arma"],
    ["CORRER", "", "Mantén para correr"],
    ["CURAR · ARMA", "", "Curarse rápido · cambiar de arma"],
    ["II", "", "Pausa"],
  ],
};

export const CONSEJOS = [
  "La linterna en la cara espanta a La Sonriente. Pero no por mucho tiempo.",
  "Si El Asomado se esconde detrás de un tronco, ya sabes dónde va a salir: más cerca.",
  "Si te quedas quieto mucho tiempo... voltea.",
  "Las notas siempre vienen en pareja. Léelas las dos. Pero cuando termines, algo se va a dar cuenta de que estás ahí.",
  "No corras todo el tiempo: sin aire no puedes pelear.",
  "Cada llave que tomas despierta algo. Ten un escondite a la mano.",
  "En el metro, a oscuras casi no le haces daño. Prende los fusibles primero.",
  "¿Te persiguen cerca del campamento? Puentea el carro: la alarma los espanta.",
  "El Vigía siempre dice la verdad. Casi siempre.",
];

export const FOTOS = [
  ["menu", "Menú principal"], ["campamento", "El campamento"], ["cine_campamento", "Cinemática · El nuevo"],
  ["vigia", "El Vigía"], ["panteon", "Panteón"], ["casa", "La casa"], ["sonriente", "La Sonriente"],
  ["mirador", "El mirador"], ["anden", "Andén de la Línea K"], ["cine_tunel", "Cinemática · Línea K"],
  ["asomado", "El Asomado"], ["pasillo", "Pasillo"], ["ropero", "Escondido en el ropero"], ["maletin", "El maletín"],
  ["notas", "Notas de la cuadrilla"], ["cueva", "La cueva"], ["mausoleo", "Mausoleo"], ["estacion", "Estación de guardabosques"],
  ["monitor", "Monitor"], ["hud", "La videocámara"], ["documento", "Documento"], ["muro", "El muro"],
  ["sonriente_ataque", "Ataque"], ["asomado_corre", "¡Corre!"], ["cine_asomado", "Cinemática · Lo que sale de noche"],
  ["screamer", "No voltees"],
];

export const CREDITOS = [
  "PSX Character Creator Free — heyheythere (CC BY 4.0)",
  "Animaciones — Mixamo (Adobe)",
  "\"La Sonriente\" — hooded_figure de Lowbit Lab; cara original del proyecto",
  "\"El Asomado\" — DC_Mimic de Desert Cryptids (Hearthlane)",
  "PSX Graveyard Starter Pack y PSX Forest Camp Starter Pack — Nickben12",
  "Casa — ALP Assets, Country House (Unity Asset Store)",
  "Filtros PSX / CRT — VNTG (ColbyO)",
  "Tema musical: \"Fundo Tema de Terror (slow+echo)\" — autor del proyecto",
  "Voces: síntesis de voz de Windows (no clona a ninguna persona real)",
];
