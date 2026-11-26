using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú 25: crea el APK de Android (celulares). Controles táctiles automáticos (ControlesTactiles),
/// horizontal, ARM64 + IL2CPP, mismo look PSX/CRT (calidad "PC"). Reporte en Temp/android_resultado.txt.
/// </summary>
public static class MonterreyAndroidSetup
{
    public static string Carpeta => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Monterrey Light Night - Android"));

    [MenuItem("Monterrey/25. Crear APK para celulares Android")]
    public static void CrearApk()
    {
        string rep = Path.GetFullPath("Temp/android_resultado.txt");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            File.WriteAllText(rep, "FALTA_MODULO: instala Android Build Support en Unity Hub.");
            Debug.LogError("[Android] Falta el módulo Android Build Support.");
            return;
        }
        EditorSceneManager.SaveOpenScenes();
        NamedBuildTarget nt = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(nt, "com.monterreylightnight.juego");
        PlayerSettings.productName = "Monterrey Light Night";
        PlayerSettings.SetScriptingBackend(nt, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.Android.forceInternetPermission = false;
        EditorUserBuildSettings.buildAppBundle = false;
        CalidadComoPC();

        Directory.CreateDirectory(Carpeta);
        string apk = Path.Combine(Carpeta, "MonterreyLightNight_Android.apk");
        BuildPlayerOptions op = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SampleScene.unity" },
            locationPathName = apk,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.CompressWithLz4HC,
        };
        BuildReport r = BuildPipeline.BuildPlayer(op);
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Resultado: " + r.summary.result);
        sb.AppendLine("Tamaño: " + (File.Exists(apk) ? (new FileInfo(apk).Length / 1048576.0).ToString("0.0") + " MB" : "?"));
        sb.AppendLine("Tiempo: " + r.summary.totalTime);
        sb.AppendLine("Errores: " + r.summary.totalErrors + "  Avisos: " + r.summary.totalWarnings);
        sb.AppendLine("Salida: " + apk);
        foreach (var s in r.steps) foreach (var m in s.messages) if (m.type == LogType.Error) sb.AppendLine("ERROR: " + m.content);
        File.WriteAllText(rep, sb.ToString());
        Debug.Log("[Android] " + sb);
    }

    static void CalidadComoPC()
    {
        Object[] qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
        if (qs == null || qs.Length == 0) return;
        SerializedObject so = new SerializedObject(qs[0]);
        SerializedProperty lista = so.FindProperty("m_PerPlatformDefaultQuality");
        if (lista == null) return;
        int pc = System.Array.IndexOf(QualitySettings.names, "PC");
        if (pc < 0) return;
        for (int i = 0; i < lista.arraySize; i++)
        {
            SerializedProperty par = lista.GetArrayElementAtIndex(i);
            if (par.FindPropertyRelative("first").stringValue == "Android") par.FindPropertyRelative("second").intValue = pc;
        }
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }
}
