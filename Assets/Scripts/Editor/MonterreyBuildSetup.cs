using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú 15: compila el juego para Windows (ejecutable .exe que corre sin Unity).
/// Menú 16: recorrido automático que revisa errores y saca capturas para el manual.
/// </summary>
public static class MonterreyBuildSetup
{
    public static string CarpetaBuild => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Monterrey Light Night - Juego"));

    [MenuItem("Monterrey/15. Crear ejecutable para Windows (.exe)")]
    public static void Compilar()
    {
        EditorSceneManager.SaveOpenScenes();

        // Ajustes del jugador para que arranque bien en cualquier PC con Windows
        PlayerSettings.companyName = "Jermaine0800";
        PlayerSettings.productName = "Monterrey Light Night";
        PlayerSettings.runInBackground = true;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.visibleInBackground = true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

        string[] escenas = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SampleScene.unity" };
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(escenas[0], true), new EditorBuildSettingsScene(escenas[1], true) };

        Directory.CreateDirectory(CarpetaBuild);
        BuildPlayerOptions op = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = Path.Combine(CarpetaBuild, "MonterreyLightNight.exe"),
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };
        BuildReport r = BuildPipeline.BuildPlayer(op);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Resultado: " + r.summary.result);
        sb.AppendLine("Tamaño: " + (r.summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB");
        sb.AppendLine("Tiempo: " + r.summary.totalTime);
        sb.AppendLine("Errores: " + r.summary.totalErrors + "  Avisos: " + r.summary.totalWarnings);
        sb.AppendLine("Salida: " + op.locationPathName);
        foreach (BuildStep paso in r.steps)
            foreach (BuildStepMessage m in paso.messages)
                if (m.type == LogType.Error || m.type == LogType.Exception) sb.AppendLine("ERROR: " + m.content);
        File.WriteAllText(Path.GetFullPath("Temp/build_resultado.txt"), sb.ToString());
        Debug.Log("[Build] " + sb);
    }

    [MenuItem("Monterrey/16. Revisar el juego y sacar capturas para el manual (automático)")]
    public static void Capturas()
    {
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        File.WriteAllText(Path.GetFullPath("Temp/capturas.flag"), "1");
        EditorApplication.isPlaying = true;
    }
}
