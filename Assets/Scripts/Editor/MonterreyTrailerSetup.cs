using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Menú 23: graba las tomas verticales para los tráilers de TikTok (ver TomasTrailer).</summary>
public static class MonterreyTrailerSetup
{
    [MenuItem("Monterrey/23. Grabar tomas para tráiler vertical (TikTok)")]
    public static void Grabar()
    {
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        File.WriteAllText(Path.GetFullPath("Temp/tomas.flag"), "1");
        EditorApplication.isPlaying = true;
    }
}
