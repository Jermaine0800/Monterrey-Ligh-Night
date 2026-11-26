using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Menú 17: ejecutable con la portada del juego.
/// - Ícono del .exe con la cara de La Sonriente.
/// - Pantalla de arranque (splash) con la portada del juego.
/// - Compila (menú 15), copia la portada a la carpeta del juego y crea un acceso directo en el Escritorio.
/// </summary>
public static class MonterreyPortadaBuild
{
    const string Carpeta = "Assets/Portada";

    [MenuItem("Monterrey/17. Crear ejecutable con portada + acceso directo en el Escritorio")]
    public static void Crear()
    {
        Texture2D icono = Importar(Carpeta + "/Icono.png", false);
        Texture2D portada = Importar(Carpeta + "/Portada.png", true);
        Texture2D logo = Importar(Carpeta + "/LogoSplash.png", true);

        // Ícono del ejecutable (todas las resoluciones usan la misma imagen)
        int n = PlayerSettings.GetIconSizes(UnityEditor.Build.NamedBuildTarget.Standalone, IconKind.Application).Length;
        Texture2D[] iconos = new Texture2D[n];
        for (int i = 0; i < n; i++) iconos[i] = icono;
        PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icono }, IconKind.Any);
        PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Standalone, iconos, IconKind.Application);

        // Pantalla de arranque con la portada
        Sprite sPortada = AssetDatabase.LoadAssetAtPath<Sprite>(Carpeta + "/Portada.png");
        Sprite sLogo = AssetDatabase.LoadAssetAtPath<Sprite>(Carpeta + "/LogoSplash.png");
        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;           // si la licencia no lo permite, Unity lo pone igual
        PlayerSettings.SplashScreen.backgroundColor = Color.black;
        PlayerSettings.SplashScreen.background = sPortada;
        PlayerSettings.SplashScreen.blurBackgroundImage = false;
        PlayerSettings.SplashScreen.overlayOpacity = 0f;
        PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
        PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
        PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(3f, null) };   // 3 s mostrando la portada
        AssetDatabase.SaveAssets();

        MonterreyBuildSetup.Compilar();

        string build = MonterreyBuildSetup.CarpetaBuild;
        string exe = Path.Combine(build, "MonterreyLightNight.exe");
        if (!File.Exists(exe)) { Debug.LogError("[Portada] No se generó el ejecutable."); return; }
        File.Copy(Path.GetFullPath(Carpeta + "/Portada.png"), Path.Combine(build, "Portada.png"), true);

        // Acceso directo en el Escritorio (usa el ícono del .exe)
        string ps = "$s=(New-Object -ComObject WScript.Shell).CreateShortcut([Environment]::GetFolderPath('Desktop')+'\\Monterrey Light Night.lnk');" +
                    "$s.TargetPath='" + exe.Replace("'", "''") + "';$s.WorkingDirectory='" + build.Replace("'", "''") + "';" +
                    "$s.IconLocation='" + exe.Replace("'", "''") + ",0';$s.Description='Monterrey Light Night';$s.Save()";
        using (Process p = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"" + ps.Replace("\"", "\\\"") + "\"")
               { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true }))
        {
            string err = p.StandardError.ReadToEnd();
            p.WaitForExit(30000);
            if (!string.IsNullOrEmpty(err)) Debug.LogWarning("[Portada] Acceso directo: " + err);
        }
        File.AppendAllText(Path.GetFullPath("Temp/build_resultado.txt"), "\nAcceso directo: Escritorio\\Monterrey Light Night.lnk\n");
        Debug.Log("[Portada] Listo: " + exe + " + acceso directo en el Escritorio.");
    }

    static Texture2D Importar(string ruta, bool sprite)
    {
        TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.isReadable = true;
            ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }
}
