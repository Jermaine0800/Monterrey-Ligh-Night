#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>
/// SOLO EDITOR (menú Monterrey/24): prueba automática de los arreglos — velas + cripta, fogata + llave,
/// arma en la mano al golpear, susto al tomar llave y embestida dentro del túnel. Saca capturas y un reporte.
/// </summary>
public class PruebaArreglos : MonoBehaviour
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/prueba_arreglos.flag"));
    static string Carpeta => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Capturas Manual/prueba_arreglos"));
    System.Text.StringBuilder r = new System.Text.StringBuilder();
    int errores;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        if (!File.Exists(Flag)) return;
        File.Delete(Flag);
        MisionManager m = FindAnyObjectByType<MisionManager>();
        if (m != null) m.saltarPrologo = true;
        new GameObject("PruebaArreglos").AddComponent<PruebaArreglos>();
    }

    void OnEnable() { Application.logMessageReceived += Log; }
    void OnDisable() { Application.logMessageReceived -= Log; }
    void Log(string m, string st, LogType t) { if (t == LogType.Exception || t == LogType.Error) { errores++; r.AppendLine("ERROR: " + m + "\n" + st); } }

    IEnumerator Start()
    {
        Directory.CreateDirectory(Carpeta);
        yield return new WaitForSecondsRealtime(4f);
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        MisionManager mm = FindAnyObjectByType<MisionManager>();
        if (mm != null) { mm.StopAllCoroutines(); }
        HorrorGameState.modo = HorrorGameState.Modo.Jugando;
        Transform j = pm.transform;

        // 1) Panteón: prender las 3 velas
        SitioPanteon pan = FindAnyObjectByType<SitioPanteon>();
        if (pan != null && pan.velas != null)
        {
            Ir(pm, pan.transform.position + pan.transform.forward * 8f, pan.transform.position);
            yield return new WaitForSecondsRealtime(1.5f);
            MethodInfo prender = typeof(VelaPanteon).GetMethod("Prender", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (VelaPanteon v in pan.velas) { prender?.Invoke(v, null); yield return new WaitForSecondsRealtime(1.2f); }
            yield return Foto("01_velas_prendidas");
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Foto("02_cinematica_cripta");
            yield return new WaitForSecondsRealtime(4f);
            r.AppendLine("Panteón: cripta abierta = " + pan.CriptaAbierta);
        }

        // 2) Fogata
        SitioCampamento camp = FindAnyObjectByType<SitioCampamento>();
        if (camp != null && camp.fogata != null)
        {
            FogataCampamento f = camp.fogata.GetComponent<FogataCampamento>();
            Ir(pm, camp.fogata.transform.position + camp.transform.forward * 4f, camp.fogata.transform.position);
            yield return new WaitForSecondsRealtime(1f);
            r.AppendLine("Campamento: llave disponible ANTES de la fogata = " + (camp.llave != null && camp.llave.Disponible));
            if (f != null) f.StartCoroutine((IEnumerator)typeof(FogataCampamento).GetMethod("Prender", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f, null));
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Foto("03_fogata_prendida");
            yield return new WaitForSecondsRealtime(3f);
            r.AppendLine("Campamento: fogata prendida = " + (f != null && f.Prendida) + ", llave disponible = " + (camp.llave != null && camp.llave.Disponible));
        }

        // 2b) Carro: arranca, alarma y espanta a un monstruo cercano
        CarroCampamento carro = FindAnyObjectByType<CarroCampamento>();
        r.AppendLine("Carro creado = " + (carro != null));
        if (carro != null)
        {
            Ir(pm, carro.transform.position + carro.transform.forward * 5f, carro.transform.position);
            yield return new WaitForSecondsRealtime(0.8f);
            EnemigoBase mon = FindAnyObjectByType<LaSonriente>();
            if (mon != null) { mon.ActivarEn(carro.transform.position + carro.transform.right * 8f); }
            yield return new WaitForSecondsRealtime(0.3f);
            bool antes = mon != null && mon.Activo;
            carro.StartCoroutine((IEnumerator)typeof(CarroCampamento).GetMethod("Arrancar", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(carro, null));
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Foto("03b_carro_alarma");
            r.AppendLine("Carro: encendido = " + carro.Encendido + ", monstruo activo antes = " + antes + ", después = " + (mon != null && mon.Activo));
            yield return new WaitForSecondsRealtime(1f);
        }

        // 2c) Monstruo de cerca (animación con vida)
        EnemigoBase m2 = FindAnyObjectByType<ElAsomado>();
        if (m2 != null)
        {
            m2.Reiniciar();
            Ir(pm, pm.transform.position, pm.transform.position + pm.transform.forward);
            typeof(EnemigoBase).GetMethod("Mostrar", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(m2, new object[] { true });
            m2.enabled = false;
            m2.transform.position = Mundo.EnSuelo(pm.transform.position + pm.transform.forward * 4f);
            m2.transform.rotation = Quaternion.LookRotation(-pm.transform.forward);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Foto("03c_monstruo_vivo_a");
            yield return new WaitForSecondsRealtime(1.3f);
            yield return Foto("03d_monstruo_vivo_b");
            r.AppendLine("Monstruo con EnemigoVida = " + (m2.GetComponent<EnemigoVida>() != null));
            typeof(EnemigoBase).GetMethod("Mostrar", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(m2, new object[] { false });
            m2.enabled = true; m2.Reiniciar();
        }

        // 3) Arma en la mano y golpe
        PlayerCombate pc = pm.GetComponent<PlayerCombate>();
        ArmaRecogible arma = FindAnyObjectByType<ArmaRecogible>();
        if (pc != null && arma != null)
        {
            arma.Interact(pm.GetComponent<PlayerInteraction>());
            yield return new WaitForSecondsRealtime(0.8f);
            CameraController cc = FindAnyObjectByType<CameraController>();
            yield return Foto("04_arma_reposo");
            FieldInfo vis = typeof(PlayerCombate).GetField("visualArma", BindingFlags.Instance | BindingFlags.NonPublic);
            GameObject va = vis?.GetValue(pc) as GameObject;
            ArmaEnMano em = va != null ? va.GetComponent<ArmaEnMano>() : null;
            r.AppendLine("Arma: equipada = " + pc.NombreArma + ", visual = " + (va != null) + ", en mano = " + (em != null && em.mano != null));
            if (em != null) { em.Golpear(); yield return new WaitForSecondsRealtime(0.18f); yield return Foto("05_arma_golpe"); }
        }

        // 4) Susto al tomar una llave (afuera)
        LlaveBucle ll = null;
        foreach (LlaveBucle x in FindObjectsByType<LlaveBucle>(FindObjectsInactive.Exclude)) if (!x.Tomada && x.Disponible) { ll = x; break; }
        if (ll != null)
        {
            Ir(pm, ll.transform.position + Vector3.forward * 2f, ll.transform.position);
            yield return new WaitForSecondsRealtime(0.5f);
            ll.Interact(pm.GetComponent<PlayerInteraction>());
            yield return new WaitForSecondsRealtime(4.5f);
            AcechoDirector d = AcechoDirector.Instancia;
            r.AppendLine("Llave tomada: monstruo activo después = " + (d != null && d.HayMonstruoActivo) + ", director habilitado = " + (d != null && d.habilitado));
            yield return Foto("06_tras_llave");
        }

        // 5) Túnel: rampas y embestida
        TunelLineaK t = FindAnyObjectByType<TunelLineaK>();
        if (t != null && t.interior != null)
        {
            r.AppendLine("Túnel: rampas = " + (t.interior.Find("Rampa_Anden_0") != null));
            if (AcechoDirector.Instancia != null) AcechoDirector.Instancia.Habilitar(false);
            foreach (EnemigoBase e in FindObjectsByType<EnemigoBase>(FindObjectsInactive.Include)) e.Reiniciar();
            Ir(pm, t.interior.TransformPoint(new Vector3(2.15f, 0.1f, -2f)), t.interior.TransformPoint(new Vector3(-0.5f, 1f, -14f)));
            yield return new WaitForSecondsRealtime(1f);
            yield return Foto("07_rampa_desde_vias");
            yield return t.StartCoroutine((IEnumerator)typeof(TunelLineaK).GetMethod("Embestida", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(t, null));
            r.AppendLine("Túnel: embestida terminada; vida del jugador = " + (SaludJugador.Instancia != null ? SaludJugador.Instancia.Vida.ToString("0") : "?"));
        }

        // 6) Metro: entrar al vagón → pelea → luces → vencerla → llave
        MetroTunel mt = MetroTunel.Instancia;
        r.AppendLine("Metro creado = " + (mt != null));
        if (mt != null && t != null)
        {
            if (SaludJugador.Instancia != null) SaludJugador.Instancia.Curar(100f);
            Ir(pm, t.interior.TransformPoint(new Vector3(1.1f, 1.05f, 9f)), t.interior.TransformPoint(new Vector3(1.1f, 1.5f, 15f)));
            yield return new WaitForSecondsRealtime(3.5f);
            r.AppendLine("Metro: pelea activa al entrar = " + MetroTunel.PeleaActiva);
            yield return Foto("08_pelea_metro");
            float d0 = mt.DanioHecho;
            mt.RecibirGolpe(40f, pm.transform.position);
            r.AppendLine("Metro: golpe de 40 a oscuras → daño real = " + (mt.DanioHecho - d0).ToString("0"));
            foreach (CajaFusibles c in mt.Cajas) { c.PrenderYa(); yield return new WaitForSecondsRealtime(0.5f); }
            yield return Foto("09_metro_luces");
            d0 = mt.DanioHecho;
            mt.RecibirGolpe(40f, pm.transform.position);
            r.AppendLine("Metro: golpe de 40 con luces → daño real = " + (mt.DanioHecho - d0).ToString("0"));
            for (int i = 0; i < 6 && MetroTunel.PeleaActiva; i++) { mt.RecibirGolpe(40f, pm.transform.position); yield return new WaitForSecondsRealtime(0.4f); }
            yield return new WaitForSecondsRealtime(2.5f);
            SitioTunel st = t.GetComponent<SitioTunel>();
            r.AppendLine("Metro: vencida = " + mt.Vencida + ", llave disponible = " + (st != null && st.llave != null && st.llave.Disponible));
            yield return Foto("10_llave_vagon");
        }

        r.Insert(0, "Errores/excepciones: " + errores + "\n\n");
        File.WriteAllText(Path.Combine(Carpeta, "reporte.txt"), r.ToString());
        UnityEditor.EditorApplication.isPlaying = false;
    }

    static void Ir(PlayerMovement pm, Vector3 pos, Vector3 mirar)
    {
        Vector3 d = mirar - pos; d.y = 0;
        pos = pos.y < Mundo.AlturaSuelo(pos) - 5f ? pos : Mundo.EnSuelo(pos, 0.05f);
        pm.Teletransportar(pos, d.sqrMagnitude > 0.01f ? Quaternion.LookRotation(d) : pm.transform.rotation);
        CameraController cc = FindAnyObjectByType<CameraController>();
        if (cc != null)
        {
            typeof(CameraController).GetField("giroHorizontal", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(cc, Quaternion.LookRotation(d.sqrMagnitude > 0.01f ? d : Vector3.forward).eulerAngles.y);
            typeof(CameraController).GetField("giroVertical", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(cc, 8f);
        }
    }

    IEnumerator Foto(string n)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(Carpeta, n + ".png"), 1);
        yield return new WaitForSecondsRealtime(0.3f);
    }
}
#endif
