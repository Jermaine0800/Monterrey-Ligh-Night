# Monterrey Light Night — sitio web (React + Vite) con el juego en /jugar

## Comandos (terminal de VS Code, en esta carpeta)
| Qué | Comando |
|---|---|
| Instalar (solo la primera vez) | `npm install` |
| Ver la página mientras editas (PC y celular en la misma red) | `npm run dev` → abre la liga que dice "Local" o "Network" |
| Generar la versión final (carpeta `dist`) | `npm run build` |
| Probar la versión final | `npm run preview` |
| Revisar librerías con fallas de seguridad | `npm run seguridad` |
| Publicar en Firebase | `npm run deploy` (primero los pasos de abajo) |

## Dónde está cada cosa
- `src/data.js` → todos los textos (historia, personajes, controles, galería, clips).
- `src/config.js` → ligas: descarga de Windows, TikTok. **Nunca pongas contraseñas aquí: todo es público.**
- `src/components/` → cada sección de la página.
- `public/media/` → imágenes, videos, música y el manual.
- `public/jugar/` → el juego de Unity (WebGL). Para actualizarlo: compila con el menú **Monterrey → 21** y copia
  `Build/*` renombrando a `mln.data.unityweb`, `mln.framework.js.unityweb`, `mln.wasm.unityweb`, `mln.loader.js`,
  más `StreamingAssets/`. NO copies el `index.html` de Unity (el nuestro no tiene scripts dentro y es más seguro).

## Publicar en Firebase (gratis, plan Spark)
1. https://console.firebase.google.com → **Agregar proyecto** (sin Google Analytics). Copia el **ID del proyecto**.
2. Pon ese ID en `.firebaserc` (donde dice `PON-AQUI-EL-ID-DE-TU-PROYECTO`).
3. En la terminal: `npx firebase-tools@13.29.1 login` (tú inicias sesión en el navegador).
4. `npm run deploy` → te da la liga `https://TU-ID.web.app`.

**Límites del plan gratis:** 10 GB guardados y **360 MB de descarga al día**. El juego pesa ~80 MB, así que
alcanza para **~4 partidas nuevas al día** (más las visitas a la página). Si se pasa, Firebase deja de servir
el sitio hasta el día siguiente — no te cobra nada si no metes tarjeta. Si el juego se hace popular:
sube el juego a itch.io (gratis, sin límite) y pon esa liga en `src/config.js` → `jugarWeb`.

## Seguridad (qué se hizo)
- **Página estática**: no hay servidor, base de datos, login ni formularios → no hay nada que "hackear" para robar datos.
- **Content-Security-Policy estricta** (firebase.json): solo se ejecuta código de este mismo sitio; nada de scripts
  externos, ni inline, ni iframes de otros. El juego tiene su propia regla (necesita WebAssembly).
- **HSTS, nosniff, Referrer-Policy, Permissions-Policy, COOP/CORP, X-Frame-Options** → evita clickjacking,
  que otros sitios metan la página en un iframe, y que el navegador "adivine" tipos de archivo.
- **Fuentes guardadas dentro del sitio** (sin Google Fonts) → cero terceros, nadie rastrea a tus visitantes.
- **React escapa todo el texto** y no se usa `dangerouslySetInnerHTML`. Las ligas de config pasan por `ligaSegura()`.
- **Versiones exactas** de cada librería (sin `^`) y `package-lock.json` → nadie puede colar una versión nueva maliciosa.
- **Sin source maps** en producción.
- **Contra tumbarla (DDoS):** Firebase Hosting sirve desde la CDN de Google, que absorbe ataques grandes.
  Lo único que puede pasar en el plan gratis es que se acabe la cuota diaria (ver arriba).
- Revísala tú mismo cuando esté publicada en https://developer.mozilla.org/en-US/observatory (Mozilla Observatory)
  y https://securityheaders.com — debería salir **A+ / A**.
