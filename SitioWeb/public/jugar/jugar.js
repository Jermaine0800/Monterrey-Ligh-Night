// Arranque del juego (sin scripts inline: así la página puede tener una CSP estricta)
(function () {
  "use strict";
  var canvas = document.getElementById("unity-canvas");
  var carga = document.getElementById("carga");
  var relleno = document.getElementById("relleno");
  var porcentaje = document.getElementById("porcentaje");
  var estado = document.getElementById("estado");
  var error = document.getElementById("error");

  function mostrarError(texto) {
    error.textContent = "";
    var p = document.createElement("p"); p.textContent = "⚠ LA CINTA SE ATORÓ: " + texto; error.appendChild(p);
    var a = document.createElement("a"); a.href = "/"; a.textContent = "Volver al sitio"; error.appendChild(a);
    error.hidden = false;
  }

  var celular = /Android|iPhone|iPad|iPod|Mobile/i.test(navigator.userAgent) || (navigator.maxTouchPoints > 1 && /Macintosh/.test(navigator.userAgent));
  var gl2 = (function () { try { return !!document.createElement("canvas").getContext("webgl2"); } catch (e) { return false; } })();

  if (celular) {
    // En el celular se juega con controles en pantalla (o un control Bluetooth). Mejor acostado.
    estado.textContent = "GIRA TU CELULAR (HORIZONTAL) · CONTROLES EN PANTALLA";
    document.body.classList.add("celular");
  }
  if (!gl2) {
    mostrarError("tu navegador no tiene WebGL 2. Actualiza Chrome, Edge o Firefox, o activa la aceleración por hardware.");
    carga.hidden = true;
    return;
  }
  if (typeof createUnityInstance !== "function") {
    mostrarError("no se pudo cargar el juego. Recarga la página.");
    return;
  }

  var config = {
    dataUrl: "Build/mln.data.unityweb",
    frameworkUrl: "Build/mln.framework.js.unityweb",
    codeUrl: "Build/mln.wasm.unityweb",
    streamingAssetsUrl: "StreamingAssets",
    companyName: "Monterrey Light Night",
    productName: "Monterrey Light Night",
    productVersion: "1.0",
    devicePixelRatio: celular ? 1 : Math.min(window.devicePixelRatio || 1, 1.5),
    showBanner: function (msg, tipo) {
      if (tipo === "error") mostrarError(msg);
      else console.warn(msg);
    }
  };

  createUnityInstance(canvas, config, function (p) {
    var n = Math.round(p * 100);
    relleno.style.width = n + "%";
    porcentaje.textContent = n + "%";
    if (n >= 90) estado.textContent = "CASI... NO VOLTEES.";
  }).then(function (unity) {
    carga.style.display = "none";
    canvas.focus();
    document.getElementById("pantalla-completa").addEventListener("click", function () { unity.SetFullscreen(1); });
  }).catch(function (e) {
    mostrarError(String(e && e.message ? e.message : e));
  });
})();
