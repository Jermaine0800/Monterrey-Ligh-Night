import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Cabeceras de seguridad también en "npm run dev" / "npm run preview" (en Firebase las pone firebase.json)
const seguridad = {
  "X-Content-Type-Options": "nosniff",
  "Referrer-Policy": "strict-origin-when-cross-origin",
  "Permissions-Policy": "camera=(), microphone=(), geolocation=(), payment=(), usb=()",
};

export default defineConfig({
  plugins: [react()],
  server: { host: true, headers: seguridad },
  preview: { host: true, headers: seguridad },
  build: {
    outDir: "dist",
    sourcemap: false,          // no publicar el código fuente
    assetsInlineLimit: 0,      // nada de data: en scripts/estilos (CSP más estricta)
    chunkSizeWarningLimit: 800,
  },
});
