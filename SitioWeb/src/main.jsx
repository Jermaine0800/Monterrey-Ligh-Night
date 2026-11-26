import React from "react";
import { createRoot } from "react-dom/client";
// Fuentes guardadas dentro del sitio (sin Google Fonts: nada de terceros, CSP más estricta)
import "@fontsource/vt323/400.css";
import "@fontsource/special-elite/400.css";
import "@fontsource/ibm-plex-mono/400.css";
import "@fontsource/ibm-plex-mono/600.css";
import "./estilo.css";
import App from "./App.jsx";

createRoot(document.getElementById("root")).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
