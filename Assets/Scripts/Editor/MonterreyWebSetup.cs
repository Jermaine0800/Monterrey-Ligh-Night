using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú 21: versión web (WebGL) para subirla a itch.io, Firebase Hosting o GitHub Pages.
/// - Compresión Gzip con "decompression fallback": funciona en cualquier hosting aunque no mande los encabezados de compresión.
/// - Sale en D:\Proyectos\Monterrey Light Night - Web  (y un .zip listo para itch.io al lado).
/// OJO: la primera vez Unity cambia la plataforma a WebGL y reimporta todo (tarda). El menú 22 regresa a Windows.
/// </summary>
public static class MonterreyWebSetup
{
    public static string CarpetaWeb => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Monterrey Light Night - Web"));
    static string Reporte => Path.GetFullPath("Temp/web_resultado.txt");

    [MenuItem("Monterrey/21. Crear versión WEB (WebGL) para subir a una página")]
    public static void CompilarWeb()
    {
        StringBuilder sb = new StringBuilder();
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            sb.AppendLine("FALTA_MODULO: Unity no tiene instalado 'WebGL Build Support'. Instálalo en Unity Hub → Instalaciones → 6000.5.10f1 → Agregar módulos.");
            File.WriteAllText(Reporte, sb.ToString());
            Debug.LogError("[Web] " + sb);
            return;
        }
        EditorSceneManager.SaveOpenScenes();
        PlayerSettings.companyName = "Jermaine0800";
        PlayerSettings.productName = "Monterrey Light Night";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.showDiagnostics = false;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        try { PlayerSettings.WebGL.initialMemorySize = 256; PlayerSettings.WebGL.maximumMemorySize = 2048; } catch { }

        string[] escenas = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SampleScene.unity" };
        Directory.CreateDirectory(CarpetaWeb);
        BuildPlayerOptions op = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = CarpetaWeb,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        };
        BuildReport r = BuildPipeline.BuildPlayer(op);
        sb.AppendLine("Resultado: " + r.summary.result);
        sb.AppendLine("Tamaño: " + (r.summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB");
        sb.AppendLine("Tiempo: " + r.summary.totalTime);
        sb.AppendLine("Errores: " + r.summary.totalErrors + "  Avisos: " + r.summary.totalWarnings);
        foreach (BuildStep paso in r.steps)
            foreach (BuildStepMessage m in paso.messages)
                if (m.type == LogType.Error || m.type == LogType.Exception) sb.AppendLine("ERROR: " + m.content);
        File.WriteAllText(Reporte, sb.ToString());
        Debug.Log("[Web] " + sb);
    }

    [MenuItem("Monterrey/22. Regresar la plataforma a Windows")]
    public static void RegresarWindows()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
    }
}
