#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SOLO EN EL EDITOR. Recorrido automático para revisar el juego y sacar capturas para el manual:
/// visita los 5 lugares, el túnel por dentro, muestra al Vigía, a los dos monstruos (idle, caminar, ataque, susto),
/// el portal, un documento y el menú principal. Mientras tanto anota TODOS los errores y excepciones.
/// Se activa con el menú Monterrey/16 (crea Temp/capturas.flag y le da Play).
/// </summary>
public class CapturasManual : MonoBehaviour
{
    public static string Carpeta => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Capturas Manual"));
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/capturas.flag"));
    static string Reporte => Path.Combine(Carpeta, "reporte_errores.txt");

    readonly List<string> errores = new List<string>();
    int fotos;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        if (!File.Exists(Flag)) return;
        if (FindAnyObjectByType<CapturasManual>() != null) return;
        File.Delete(Flag);
        Directory.CreateDirectory(Carpeta);
        MisionManager mm = FindAnyObjectByType<MisionManager>();
        if (mm != null) mm.saltarPrologo = true;
        GameObject g = new GameObject("CapturasManual");
        DontDestroyOnLoad(g);
        g.AddComponent<CapturasManual>();
    }

    void OnEnable() { Application.logMessageReceived += Log; }
    void OnDisable() { Application.logMessageReceived -= Log; }

    void Log(string msg, string stack, LogType t)
    {
        if (t == LogType.Error || t == LogType.Exception || t == LogType.Assert)
            errores.Add("[" + Time.realtimeSinceStartup.ToString("0.0") + "s] " + t + ": " + msg + "\n" + stack);
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(3f);
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        MisionManager mm = FindAnyObjectByType<MisionManager>();
        GuiaController guia = FindAnyObjectByType<GuiaController>();
        // La historia no corre durante el recorrido (si no, abre diálogos encima de las capturas)
        if (mm != null) { mm.StopAllCoroutines(); mm.enabled = false; }
        if (guia != null) guia.Desaparecer();
        HorrorGameState.modo = HorrorGameState.Modo.Jugando;
        if (AcechoDirector.Instancia != null) AcechoDirector.Instancia.Habilitar(false);
        PsicoDirector psico = FindAnyObjectByType<PsicoDirector>(); if (psico != null) psico.habilitado = false;
        SaludJugador salud = FindAnyObjectByType<SaludJugador>();
        Transform j = pm.transform;
        Vector3 inicio = j.position;

        // Esperar a que termine la cinemática de inicio si la hubiera
        yield return new WaitForSecondsRealtime(2f);
        yield return Foto("01_inicio_HUD");

        // Linterna en la mano y prendida
        FlashlightController fl = FindAnyObjectByType<FlashlightController>();
        FlashlightItem item = FindAnyObjectByType<FlashlightItem>();
        PlayerInteraction pi = pm.GetComponent<PlayerInteraction>();
        if (item != null && pi != null) item.Interact(pi);
        yield return new WaitForSecondsRealtime(1.2f);
        if (fl != null) typeof(FlashlightController).GetField("encendida", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(fl, true);

        // El Vigía
        if (guia != null)
        {
            Vector3 frente = Frente(j);
            guia.Aparecer(j.position + frente * 4f, j.position, GuiaController.Pose.Hablar);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Foto("02_vigia_hablando");
            guia.Animar(GuiaController.Pose.Saludar, 0.05f);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Foto("03_vigia_saludando");
            guia.Animar(GuiaController.Pose.Senalar, 0.05f);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Foto("04_vigia_senalando");
            guia.Animar(GuiaController.Pose.Sentado, 0.05f);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Foto("05_vigia_sentado");
            guia.Desaparecer();
        }

        // Los 5 lugares
        if (mm != null && mm.sitios != null)
            for (int i = 0; i < mm.sitios.Length; i++)
            {
                SitioLlave s = mm.sitios[i];
                if (s == null) continue;
                Vector3 hacia = inicio - s.transform.position; hacia.y = 0;
                float d = s is SitioIglesia ? 16f : s is SitioMirador ? 9f : 12f;
                Vector3 p = Mundo.EnSuelo(s.transform.position + hacia.normalized * d, 0.1f);
                Ir(pm, p, s.transform.position);
                yield return new WaitForSecondsRealtime(2.5f);
                yield return Foto("1" + i + "_lugar_" + Limpio(s.nombreLugar));
                // un segundo ángulo, más cerca
                Vector3 lado = Vector3.Cross(Vector3.up, hacia.normalized);
                Ir(pm, Mundo.EnSuelo(s.transform.position + hacia.normalized * d * 0.45f + lado * 4f, 0.1f), s.transform.position);
                yield return new WaitForSecondsRealtime(2f);
                yield return Foto("1" + i + "_lugar_" + Limpio(s.nombreLugar) + "_cerca");
            }

        // El túnel por dentro
        if (mm != null && mm.tunel != null && mm.tunel.interior != null)
        {
            Transform I = mm.tunel.interior;
            Ir(pm, I.TransformPoint(new Vector3(-3f, 1.05f, -16f)), I.TransformPoint(new Vector3(-3f, 1.5f, 5f)));
            yield return new WaitForSecondsRealtime(2f);
            yield return Foto("20_tunel_anden");
            Ir(pm, I.TransformPoint(new Vector3(-2.2f, 1.05f, -8f)), mm.tunel.monitor.position);
            yield return new WaitForSecondsRealtime(2f);
            yield return Foto("21_tunel_caseta_monitor");
        }

        // Casa por dentro, zonas nuevas, estación y un escondite
        if (mm != null && mm.sitios != null)
            foreach (SitioLlave s in mm.sitios)
            {
                if (s is SitioCasa)
                {
                    Transform t = s.transform; float y = 0.45f;
                    Ir(pm, t.TransformPoint(new Vector3(0, y, 5.5f)), t.TransformPoint(new Vector3(0, 1f, 0f)));
                    yield return new WaitForSecondsRealtime(1.5f); yield return Foto("50_casa_pasillo");
                    Ir(pm, t.TransformPoint(new Vector3(-0.8f, y, -0.6f)), t.TransformPoint(new Vector3(-0.8f, 1f, -3f)));
                    yield return new WaitForSecondsRealtime(1.5f); yield return Foto("51_casa_comedor");
                    Ir(pm, t.TransformPoint(new Vector3(3.6f, y, -0.6f)), t.TransformPoint(new Vector3(6.2f, 1f, -2.6f)));
                    yield return new WaitForSecondsRealtime(1.5f); yield return Foto("52_casa_recamara");
                    Ir(pm, t.TransformPoint(new Vector3(-5.0f, y, -1.6f)), t.TransformPoint(new Vector3(-5.4f, 1f, -3.8f)));
                    yield return new WaitForSecondsRealtime(1.5f); yield return Foto("53_casa_cuarto_llave");
                    Escondite ropero = null;
                    foreach (Escondite e in t.GetComponentsInChildren<Escondite>()) if (e.EsMueble) ropero = e;
                    PlayerEscondite pe = pm.GetComponent<PlayerEscondite>() ?? pm.gameObject.AddComponent<PlayerEscondite>();
                    if (ropero != null)
                    {
                        pe.Entrar(ropero);
                        yield return new WaitForSecondsRealtime(1.5f); yield return Foto("54_escondido_ropero");
                        pe.Salir();
                        yield return new WaitForSecondsRealtime(0.5f);
                    }
                }
                if (s.notaAyer != null)
                {
                    Transform n = s.notaAyer.transform.parent;
                    Ir(pm, Mundo.EnSuelo(n.position + n.forward * 2.2f, 0.1f), n.position);
                    yield return new WaitForSecondsRealtime(1.2f); yield return Foto("55_notas_" + Limpio(s.nombreLugar));
                }
                Transform ex = s.transform.Find("Expansion");
                if (ex != null)
                    foreach (Transform z in ex)
                    {
                        if (z.name.StartsWith("Letrero") || z.name.StartsWith("Trampa") || z.name.Contains("Modelo") || z.childCount < 3) continue;
                        Ir(pm, Mundo.EnSuelo(z.TransformPoint(new Vector3(1.5f, 0, 7f)), 0.1f), z.position + Vector3.up);
                        yield return new WaitForSecondsRealtime(1.5f); yield return Foto("56_zona_" + Limpio(s.nombreLugar) + "_" + z.name);
                    }
            }
        GameObject est = GameObject.Find("_EstacionGuardabosques");
        if (est != null)
        {
            Transform t = est.transform;
            Ir(pm, Mundo.EnSuelo(t.TransformPoint(new Vector3(-2f, 0, 10f)), 0.1f), t.position + Vector3.up * 2f);
            yield return new WaitForSecondsRealtime(1.5f); yield return Foto("57_estacion_afuera");
            Ir(pm, t.TransformPoint(new Vector3(-2.5f, 0, 1.6f)) + Vector3.up * (Mundo.AlturaSuelo(t.position) + 0.8f - t.position.y), t.TransformPoint(new Vector3(-3.5f, 1f, -2f)));
            yield return new WaitForSecondsRealtime(1.5f); yield return Foto("58_estacion_oficina");
            Transform torre = t.Find("Torre");
            if (torre != null)
            {
                Ir(pm, Mundo.EnSuelo(torre.position, 6.4f) - t.forward * 0.6f, t.position + t.forward * 30f);
                yield return new WaitForSecondsRealtime(1.5f); yield return Foto("59_estacion_desde_torre");
            }
        }

        // Prueba del maletín: recoger de verdad (Interact) curaciones, armasM, notas y llaves, y abrirlo
        {
            System.Text.StringBuilder logM = new System.Text.StringBuilder();
            InventoryUI uiM = FindAnyObjectByType<InventoryUI>();
            PlayerInteraction piM = pm.GetComponent<PlayerInteraction>();
            PlayerInventory invM = pm.GetComponent<PlayerInventory>() ?? FindAnyObjectByType<PlayerInventory>();
            BackpackPickup mochilaM = FindAnyObjectByType<BackpackPickup>();
            PlayerCombate pcM = pm.GetComponent<PlayerCombate>();
            // 1) un arma ANTES de la mochila (se equipa; al tomar la mochila debe guardarse)
            ArmaRecogible[] armasM = FindObjectsByType<ArmaRecogible>(FindObjectsSortMode.None);
            if (uiM != null) uiM.hasBackPack = false;
            if (armasM.Length > 0 && piM != null) armasM[0].Interact(piM);
            logM.AppendLine("Antes de la mochila: arma equipada = " + (pcM != null && pcM.TieneArma ? pcM.NombreArma : "ninguna") + ", objetos = " + (invM != null ? invM.objetos.Count : -1));
            if (mochilaM != null && piM != null) mochilaM.Interact(piM); else if (uiM != null) uiM.hasBackPack = true;
            yield return null;
            logM.AppendLine("Después de la mochila: objetos = " + (invM != null ? invM.objetos.Count : -1));
            foreach (Curacion c in FindObjectsByType<Curacion>(FindObjectsSortMode.None)) { if (piM != null) c.Interact(piM); if (invM != null && invM.Contar(ItemDefinition.Tipo.Curacion) >= 3) break; }
            for (int i = 1; i < armasM.Length && i < 3; i++) if (piM != null) armasM[i].Interact(piM);
            InventarioUtil.GuardarNota("PRUEBA — PISTAS DE AYER", "texto 1");
            InventarioUtil.GuardarNota("PRUEBA — INSTRUCCIONES", "texto 2");
            InventarioUtil.ActualizarLlaves(1, LlavesBucle.Total, "prueba");
            InventarioUtil.ActualizarLlaves(2, LlavesBucle.Total, "prueba");
            yield return null;
            if (invM != null) foreach (ItemDefinition it in invM.objetos) logM.AppendLine("  · " + it.tipo + " | " + it.nombre + (it.tipo == ItemDefinition.Tipo.Arma ? " | " + it.usos + " golpes" : "") + (it.icono != null ? " | con ícono" : " | SIN ÍCONO"));
            logM.AppendLine("Total casillas usadas: " + (invM != null ? invM.objetos.Count : -1) + " de " + (invM != null ? invM.capacidad : -1));
            if (pcM != null) { pcM.CambiarArma(1); logM.AppendLine("Cambiar arma → " + (pcM.TieneArma ? pcM.NombreArma : "manos")); }
            if (uiM != null)
            {
                MethodInfo abrirM = typeof(InventoryUI).GetMethod("Abrir", BindingFlags.Instance | BindingFlags.NonPublic);
                abrirM?.Invoke(uiM, null);
                yield return new WaitForSecondsRealtime(1f);
                yield return Foto("60_maletin_con_objetos");
                MethodInfo cerrarM = typeof(InventoryUI).GetMethod("Cerrar", BindingFlags.Instance | BindingFlags.NonPublic);
                cerrarM?.Invoke(uiM, null);
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(Carpeta, "prueba_maletin.txt"), logM.ToString());
        }

        // Prueba de combate: cuántos golpes aguantan, si se quedan después de un golpe y si mueren para siempre con 5 llaves
        {
            System.Text.StringBuilder lc = new System.Text.StringBuilder();
            AcechoDirector dirC = FindAnyObjectByType<AcechoDirector>();
            if (dirC != null) dirC.Habilitar(false);
            foreach (EnemigoBase e in FindObjectsByType<EnemigoBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                e.Reiniciar();
                e.ActivarEn(pm.transform.position + pm.transform.forward * 6f);
                yield return new WaitForSecondsRealtime(0.3f);
                lc.AppendLine(e.nombre + ": vida " + e.Vida + "/" + e.VidaMaxima + "  estado " + e.EstadoActual);
                e.RecibirDanio(60f, pm.transform.position);
                yield return new WaitForSecondsRealtime(1.0f);
                lc.AppendLine("  tras 1 golpe (60): vida " + e.Vida + "  estado " + e.EstadoActual + (e.Activo ? "  (sigue cazando ✔)" : "  (se fue)"));
                int golpes = 1;
                while (e.EstaVivo && golpes < 20)
                {
                    if (!e.Activo) { e.ActivarEn(pm.transform.position + pm.transform.forward * 6f); yield return new WaitForSecondsRealtime(0.2f); }
                    e.RecibirDanio(60f, pm.transform.position); golpes++;
                    yield return new WaitForSecondsRealtime(0.8f);
                }
                lc.AppendLine("  golpes de 60 para derrotarlo: " + golpes + "  → estado " + e.EstadoActual + "  para siempre: " + e.MuertoParaSiempre);
                yield return new WaitForSecondsRealtime(3f);   // termina la cinemática de derrota
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(Carpeta, "prueba_combate.txt"), lc.ToString());
            Time.timeScale = 1f;
        }

        // Los monstruos: idle, caminar, ataque y susto
        Ir(pm, Mundo.EnSuelo(inicio + Vector3.right * 3f, 0.1f), inicio + Vector3.right * 3f + Vector3.forward * 10f);
        yield return new WaitForSecondsRealtime(1.5f);
        foreach (EnemigoBase e in FindObjectsByType<EnemigoBase>(FindObjectsInactive.Exclude))
        {
            string n = Limpio(e.nombre);
            // ponerlo enfrente y visible (sin la lógica de "desaparece cuando lo ves")
            e.transform.position = Mundo.EnSuelo(j.position + Frente(j) * 4.5f);
            Vector3 mira = j.position - e.transform.position; mira.y = 0;
            e.transform.rotation = Quaternion.LookRotation(mira);
            typeof(EnemigoBase).GetMethod("Mostrar", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(e, new object[] { true });
            yield return null;
            Animator a = e.GetComponentInChildren<Animator>();
            foreach (string estado in new[] { "Idle", "Walk", "Run", "Attack", "Death" })
            {
                if (a == null || !a.HasState(0, Animator.StringToHash(estado))) { errores.Add("[ANIMACIÓN] " + e.nombre + " no tiene el estado " + estado); continue; }
                a.Play(estado, 0, 0.35f);
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Foto("3x_monstruo_" + n + "_" + estado);
            }
            e.Reiniciar();
            yield return new WaitForSecondsRealtime(0.5f);
        }
        // Un susto (screamer) de La Sonriente
        foreach (EnemigoBase e in FindObjectsByType<EnemigoBase>(FindObjectsInactive.Exclude))
            if (e is LaSonriente)
            {
                e.Activar();
                MethodInfo asustar = typeof(EnemigoBase).GetMethod("Asustar", BindingFlags.Instance | BindingFlags.NonPublic);
                asustar?.Invoke(e, null);
                yield return new WaitForSecondsRealtime(0.25f);
                yield return Foto("39_screamer");
                yield return new WaitForSecondsRealtime(1.5f);
                e.Reiniciar();
                if (salud != null) salud.Curar(100f);
                break;
            }

        // Portal
        if (mm != null && mm.puertaBucle != null)
        {
            Transform pb = mm.puertaBucle.transform;
            Ir(pm, Mundo.EnSuelo(pb.position - pb.forward * 7f, 0.1f), pb.position + Vector3.up * 1.4f);
            mm.puertaBucle.Abrir();
            yield return new WaitForSecondsRealtime(4.5f);
            yield return Foto("40_portal");
        }

        // Un documento
        StartCoroutine(DocumentoUI.Mostrar(Casillero.TituloDocumento, Casillero.CuerpoDocumento));
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Foto("41_documento");

        // Menú principal
        SceneManager.LoadScene("MainMenu");
        yield return new WaitForSecondsRealtime(5f);
        yield return Foto("00_menu_principal");

        File.WriteAllText(Reporte, "Capturas: " + fotos + "\nErrores/excepciones: " + errores.Count + "\n\n" + string.Join("\n\n", errores));
        Debug.Log("[Capturas] Listo: " + fotos + " capturas, " + errores.Count + " errores. Carpeta: " + Carpeta);
        UnityEditor.EditorApplication.isPlaying = false;
    }

    static Vector3 Frente(Transform j)
    {
        Transform c = Camera.main != null ? Camera.main.transform : j;
        Vector3 f = c.forward; f.y = 0; return f.normalized;
    }

    static void Ir(PlayerMovement pm, Vector3 pos, Vector3 mirar)
    {
        Vector3 d = mirar - pos; d.y = 0;
        Quaternion r = d.sqrMagnitude > 0.01f ? Quaternion.LookRotation(d) : pm.transform.rotation;
        pm.Teletransportar(pos, r);
        CameraController cc = FindAnyObjectByType<CameraController>();
        if (cc != null)
        {
            FieldInfo f = typeof(CameraController).GetField("giroHorizontal", BindingFlags.Instance | BindingFlags.NonPublic);
            f?.SetValue(cc, r.eulerAngles.y);
            FieldInfo v = typeof(CameraController).GetField("giroVertical", BindingFlags.Instance | BindingFlags.NonPublic);
            v?.SetValue(cc, 8f);
        }
    }

    IEnumerator Foto(string nombre)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(Carpeta, nombre + ".png"), 1);
        fotos++;
        yield return new WaitForSecondsRealtime(0.4f);
    }

    static string Limpio(string s)
    {
        string r = "";
        foreach (char c in s.ToLower()) r += char.IsLetterOrDigit(c) ? (c == 'á' ? 'a' : c == 'é' ? 'e' : c == 'í' ? 'i' : c == 'ó' ? 'o' : c == 'ú' ? 'u' : c == 'ñ' ? 'n' : c) : '_';
        return r;
    }
}
#endif
