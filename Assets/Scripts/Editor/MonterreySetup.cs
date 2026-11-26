#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú "Monterrey" en la barra de Unity. Arma las escenas automáticamente
/// para no tener que arrastrar cosas a mano.
/// </summary>
public static class MonterreySetup
{
    const string RutaMenu = "Assets/Scenes/MainMenu.unity";
    const string RutaJuego = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Monterrey/1. Crear escena del Menú Principal")]
    public static void CrearMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cam = new GameObject("Main Camera");
        cam.tag = "MainCamera";
        Camera c = cam.AddComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = Color.black;
        cam.AddComponent<AudioListener>();

        GameObject menu = new GameObject("MainMenu");
        menu.AddComponent<MainMenuController>();

        EditorSceneManager.SaveScene(escena, RutaMenu);

        // Build Settings: Menú primero, luego el juego
        var lista = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(RutaMenu, true),
            new EditorBuildSettingsScene(RutaJuego, true),
        };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != RutaMenu && s.path != RutaJuego) lista.Add(s);
        EditorBuildSettings.scenes = lista.ToArray();

        EditorSceneManager.OpenScene(RutaJuego);
        Debug.Log("[Monterrey] Escena MainMenu creada y agregada como primera en Build Settings.");
    }

    [MenuItem("Monterrey/2. Agregar sistemas de terror a la escena abierta")]
    public static void AgregarSistemas()
    {
        GameObject sistemas = GameObject.Find("_SistemasTerror");
        if (sistemas == null) sistemas = new GameObject("_SistemasTerror");

        Agregar<NightAmbience>(sistemas);
        Agregar<AmbientAudio>(sistemas);
        Agregar<PauseMenu>(sistemas);
        VHSOverlay vhs = Agregar<VHSOverlay>(sistemas);
        vhs.modoMenu = false;

        PlayerMovement jugador = Object.FindAnyObjectByType<PlayerMovement>();
        if (jugador != null)
        {
            Agregar<FlashlightController>(jugador.gameObject);
            if (jugador.GetComponent<CharacterController>() != null) Agregar<FootstepAudio>(jugador.gameObject);
            if (jugador.gameObject.tag == "Untagged") jugador.gameObject.tag = "Player";
        }
        else Debug.LogWarning("[Monterrey] No encontré un PlayerMovement activo en la escena.");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = sistemas;
        Debug.Log("[Monterrey] Sistemas de terror agregados: noche, audio, VHS, pausa, linterna y pasos.");
    }

    [MenuItem("Monterrey/3. Reporte de peso (assets más pesados)")]
    public static void ReportePeso()
    {
        var archivos = System.IO.Directory.GetFiles("Assets", "*.*", System.IO.SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".meta"))
            .Select(f => new System.IO.FileInfo(f))
            .OrderByDescending(f => f.Length)
            .Take(30);
        System.Text.StringBuilder sb = new System.Text.StringBuilder("[Monterrey] 30 archivos más pesados:\n");
        foreach (var f in archivos) sb.AppendLine((f.Length / 1048576f).ToString("0.0") + " MB  " + f.FullName.Replace("\\", "/"));
        Debug.Log(sb.ToString());
    }

    [MenuItem("Monterrey/4. Probar desde el Menú Principal  ▶")]
    public static void ProbarDesdeMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(RutaMenu);
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Monterrey/5. Abrir escena del juego")]
    public static void AbrirJuego()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(RutaJuego);
    }


    // ======================================================================== EL VIGÍA

    const string CarpetaGuia = "Assets/Animations/Animaciones Monterrey Incident";
    const string RutaModeloGuia = CarpetaGuia + "/GuiaIncident_texture.fbx";
    const string CarpetaAnimGuia = CarpetaGuia + "/Guia";
    const string RutaControlador = CarpetaAnimGuia + "/VigiaTerror.controller";

    [MenuItem("Monterrey/6. Configurar al Vigía, misiones y limpiar inventario viejo")]
    public static void ConfigurarVigia()
    {
        if (!AssetDatabase.IsValidFolder(CarpetaAnimGuia)) AssetDatabase.CreateFolder(CarpetaGuia, "Guia");

        // 1) Modelo del Vigía como Humanoid (si el esqueleto lo permite)
        bool humano = ConfigurarModelo(out Avatar avatar);

        // 2) Animaciones: las de la carpeta Guia + las que ya tenías (saludar / caminar)
        var rutas = new List<string>();
        rutas.AddRange(AssetDatabase.FindAssets("t:Model", new[] { CarpetaAnimGuia }).Select(AssetDatabase.GUIDToAssetPath));
        rutas.Add(CarpetaGuia + "/M_Incident saludar.fbx");
        rutas.Add(CarpetaGuia + "/M_IncidentCaminar.fbx");

        var clips = new Dictionary<GuiaController.Pose, AnimationClip>();
        foreach (string ruta in rutas.Distinct())
        {
            if (AssetImporter.GetAtPath(ruta) == null || ruta == RutaModeloGuia) continue;
            GuiaController.Pose? pose = PoseDeArchivo(ruta);
            if (pose == null) { Debug.Log("[Monterrey] No sé qué pose es: " + ruta); continue; }
            bool loop = pose == GuiaController.Pose.Idle || pose == GuiaController.Pose.Sentado ||
                        pose == GuiaController.Pose.Caminar || pose == GuiaController.Pose.Hablar;
            ConfigurarClip(ruta, humano, avatar, loop);
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>()
                                  .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip != null && (!clips.ContainsKey(pose.Value) || ruta.StartsWith(CarpetaAnimGuia))) clips[pose.Value] = clip;
        }

        // 3) Animator Controller con un estado por pose
        if (AssetDatabase.LoadAssetAtPath<Object>(RutaControlador) != null) AssetDatabase.DeleteAsset(RutaControlador);
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState porDefecto = null;
        foreach (GuiaController.Pose p in System.Enum.GetValues(typeof(GuiaController.Pose)))
        {
            AnimationClip clip;
            if (!clips.TryGetValue(p, out clip))
            {
                // Sin clip: Idle usa la caminata congelada para no quedar en T-pose
                if (p == GuiaController.Pose.Idle && clips.TryGetValue(GuiaController.Pose.Caminar, out clip))
                {
                    AnimatorState congelado = sm.AddState("Idle");
                    congelado.motion = clip; congelado.speed = 0f;
                    porDefecto = congelado;
                }
                continue;
            }
            AnimatorState st = sm.AddState(p.ToString());
            st.motion = clip;
            if (p == GuiaController.Pose.Idle) porDefecto = st;
        }
        if (porDefecto != null) sm.defaultState = porDefecto;

        // 4) Conectar en la escena
        GameObject guiaGO = BuscarEnEscena("GuiaIncident");
        if (guiaGO == null) { Debug.LogError("[Monterrey] No encontré a GuiaIncident_texture en la escena."); return; }
        Undo.RecordObject(guiaGO, "Vigia");
        guiaGO.SetActive(true);
        Animator anim = guiaGO.GetComponentInChildren<Animator>(true);
        if (anim == null) anim = Undo.AddComponent<Animator>(guiaGO);
        Undo.RecordObject(anim, "Vigia");
        anim.runtimeAnimatorController = ctrl;
        if (avatar != null) anim.avatar = avatar;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        GuiaController gc = Agregar<GuiaController>(guiaGO);
        gc.animator = anim;
        foreach (var gt in guiaGO.GetComponentsInChildren<GuiaTrigger>(true)) Undo.DestroyObjectImmediate(gt);

        GameObject sistemas = GameObject.Find("_SistemasTerror");
        if (sistemas == null) sistemas = new GameObject("_SistemasTerror");
        Agregar<DialogoUI>(sistemas);
        Agregar<AnimasManager>(sistemas);
        MisionManager mm = Agregar<MisionManager>(sistemas);
        mm.guia = gc;
        mm.llave = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Scripts/Inventory/Item_Llave.asset");
        GameObject casa = GameObject.Find("House_Prefab");
        if (casa != null) mm.casa = casa.transform;
        EditorUtility.SetDirty(mm);

        // 5) Quitar el inventario viejo (Panel con 6 slots)
        InventoryUI inv = Object.FindAnyObjectByType<InventoryUI>();
        if (inv != null && inv.inventoryPanel != null)
        {
            Undo.DestroyObjectImmediate(inv.inventoryPanel);
            inv.inventoryPanel = null;
            EditorUtility.SetDirty(inv);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        // 6) Ordenar: InventoryUI.cs dentro de Assets/Scripts
        if (AssetDatabase.LoadAssetAtPath<Object>("Assets/InventoryUI.cs") != null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scripts/Inventario")) AssetDatabase.CreateFolder("Assets/Scripts", "Inventario");
            string err = AssetDatabase.MoveAsset("Assets/InventoryUI.cs", "Assets/Scripts/Inventario/InventoryUI.cs");
            if (!string.IsNullOrEmpty(err)) Debug.LogWarning("[Monterrey] No pude mover InventoryUI.cs: " + err);
        }

        Debug.Log("[Monterrey] Vigía listo. Humanoid: " + humano + ". Poses con animación: " +
                  string.Join(", ", clips.Keys.Select(k => k.ToString())));
    }

    static bool ConfigurarModelo(out Avatar avatar)
    {
        ModelImporter imp = AssetImporter.GetAtPath(RutaModeloGuia) as ModelImporter;
        avatar = null;
        if (imp == null) return false;
        if (imp.animationType != ModelImporterAnimationType.Human)
        {
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.SaveAndReimport();
        }
        avatar = AssetDatabase.LoadAllAssetsAtPath(RutaModeloGuia).OfType<Avatar>().FirstOrDefault();
        if (avatar != null && avatar.isValid && avatar.isHuman) return true;

        Debug.LogWarning("[Monterrey] El esqueleto del Vigía no sirvió como Humanoid; lo dejo en Generic.");
        imp.animationType = ModelImporterAnimationType.Generic;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.SaveAndReimport();
        avatar = AssetDatabase.LoadAllAssetsAtPath(RutaModeloGuia).OfType<Avatar>().FirstOrDefault();
        return false;
    }

    static void ConfigurarClip(string ruta, bool humano, Avatar avatar, bool loop)
    {
        ModelImporter imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
        if (imp == null) return;
        imp.importAnimation = true;
        if (humano)
        {
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }
        else
        {
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            imp.sourceAvatar = avatar;
        }
        imp.SaveAndReimport();

        ModelImporterClipAnimation[] clips = imp.defaultClipAnimations;
        foreach (var c in clips)
        {
            c.loopTime = loop;
            c.loopPose = loop;
            c.lockRootRotation = true; c.keepOriginalOrientation = true;
            c.lockRootHeightY = true;  c.keepOriginalPositionY = true;
            c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
        }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();
    }

    static GuiaController.Pose? PoseDeArchivo(string ruta)
    {
        string n = System.IO.Path.GetFileNameWithoutExtension(ruta).ToLowerInvariant();
        if (n.Contains("sit")) return GuiaController.Pose.Sentado;
        if (n.Contains("intimid") || n.Contains("taunt") || n.Contains("threat") || n.Contains("angry") || n.Contains("roar") || n.Contains("scream") || n.Contains("yell")) return GuiaController.Pose.Intimidar;
        if (n.Contains("laugh") || n.Contains("reir")) return GuiaController.Pose.Reir;
        if (n.Contains("point") || n.Contains("senal")) return GuiaController.Pose.Senalar;
        if (n.Contains("talk") || n.Contains("habl")) return GuiaController.Pose.Hablar;
        if (n.Contains("wav") || n.Contains("salud") || n.Contains("greet")) return GuiaController.Pose.Saludar;
        if (n.Contains("walk") || n.Contains("caminar")) return GuiaController.Pose.Caminar;
        if (n.Contains("idle")) return GuiaController.Pose.Idle;
        return null;
    }

    static GameObject BuscarEnEscena(string contiene)
    {
        foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            if (raiz.name.Contains(contiene)) return raiz;
        return null;
    }

    static T Agregar<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }
}
#endif
