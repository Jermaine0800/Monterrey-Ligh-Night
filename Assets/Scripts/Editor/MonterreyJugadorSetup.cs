#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Configura al jugador: vida, golpe, escondite y cámara.</summary>
public static class MonterreyJugadorSetup
{
    [MenuItem("Monterrey/7. Configurar jugador (vida, golpe, esconderse, cámara)")]
    public static void ConfigurarJugador()
    {
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) { Debug.LogError("[Monterrey] No hay PlayerMovement en la escena."); return; }
        GameObject j = pm.gameObject;

        Agregar<SaludJugador>(j);
        Agregar<PlayerCombate>(j);
        Agregar<PlayerEscondite>(j);
        if (j.tag == "Untagged") j.tag = "Player";

        CameraController cam = Object.FindAnyObjectByType<CameraController>();
        if (cam != null && cam.objetivo == null) cam.objetivo = j.transform;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = j;
        Debug.Log("[Monterrey] Jugador listo: vida, golpe (RT), esconderse (RB en escondites), agacharse (B), correr (L3/LB), cámara (R3).");
    }

    public static T Agregar<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }
}
#endif
