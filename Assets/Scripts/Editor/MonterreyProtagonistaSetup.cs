#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Arma al protagonista (senderista) con "PSX Character Creator Free" (CC BY 4.0, heyheythere)
/// y le pasa TODO lo que tenía el X Bot: movimiento, inventario, mochila, linterna, vida, golpe, esconderse.
/// Animaciones de Mixamo (Humanoid) + "cower" del pack para cuando se esconde.
/// </summary>
public static class MonterreyProtagonistaSetup
{
    const string Carpeta = "Assets/Characters/Protagonista/PSXCreator";
    const string Cuerpo = Carpeta + "/man_2.fbx";
    const string Atlas = Carpeta + "/atlas.png";
    const string CarpetaAnim = "Assets/Animations/Jugador";
    const string RutaControlador = "Assets/Animations/Jugador/PlayerTerror.controller";

    // Piezas y colores del senderista (multiplican el atlas)
    static readonly (string pieza, Color color)[] Ropa =
    {
        ("man_hair_short", new Color(0.16f, 0.11f, 0.08f)),   // pelo castaño oscuro
        ("man_tee",        new Color(0.30f, 0.30f, 0.30f)),   // playera gris
        ("man_jacket",     new Color(0.33f, 0.36f, 0.22f)),   // chamarra verde olivo
        ("man_jeans",      new Color(0.20f, 0.25f, 0.36f)),   // jeans
        ("man_boots",      new Color(0.30f, 0.20f, 0.12f)),   // botas café
    };

    [MenuItem("Monterrey/8. Crear protagonista senderista (reemplaza al X Bot)")]
    public static void CrearProtagonista()
    {
        // ---------- 1. Importación ----------
        ConfigurarTextura();
        Avatar avatar = ConfigurarCuerpo();
        if (avatar == null || !avatar.isHuman) { Debug.LogError("[Monterrey] El cuerpo no generó un Avatar Humanoid válido."); return; }
        foreach (var r in Ropa) ConfigurarPieza(Carpeta + "/" + r.pieza + ".fbx");
        AnimatorController ctrl = CrearControlador();

        // ---------- 2. Material ----------
        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
        Material piel = CrearMaterial("Senderista_Cuerpo", atlas, Color.white);

        // ---------- 3. Jugador actual (X Bot) ----------
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) { Debug.LogError("[Monterrey] No encontré al jugador (PlayerMovement)."); return; }
        GameObject jugador = pm.gameObject;
        if (PrefabUtility.IsPartOfPrefabInstance(jugador))
            PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(jugador), PrefabUnpackMode.Completely, InteractionMode.UserAction);
        Undo.RegisterFullObjectHierarchyUndo(jugador, "Protagonista");

        // Huesos viejos del X Bot (para mover los sockets)
        Animator animViejo = jugador.GetComponent<Animator>();
        Transform manoVieja = animViejo != null && animViejo.isHuman ? animViejo.GetBoneTransform(HumanBodyBones.RightHand) : null;

        // ---------- 4. Modelo nuevo como hijo ----------
        GameObject modeloAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Cuerpo);
        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(modeloAsset);
        PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        Undo.RegisterCreatedObjectUndo(modelo, "Protagonista");
        modelo.name = "Modelo_Senderista";
        modelo.transform.SetParent(jugador.transform, false);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;
        foreach (SkinnedMeshRenderer s in modelo.GetComponentsInChildren<SkinnedMeshRenderer>()) s.sharedMaterial = piel;

        Dictionary<string, Transform> huesos = modelo.GetComponentsInChildren<Transform>().GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());

        // Ropa: cada pieza se vuelve a amarrar al esqueleto del cuerpo
        foreach (var r in Ropa)
        {
            GameObject piezaAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + "/" + r.pieza + ".fbx");
            if (piezaAsset == null) continue;
            GameObject pieza = Object.Instantiate(piezaAsset);
            Material mat = CrearMaterial("Senderista_" + r.pieza, atlas, r.color);
            foreach (SkinnedMeshRenderer s in pieza.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                s.bones = s.bones.Select(b => b != null && huesos.TryGetValue(b.name, out Transform h) ? h : null).ToArray();
                if (s.rootBone != null && huesos.TryGetValue(s.rootBone.name, out Transform rb)) s.rootBone = rb;
                s.sharedMaterial = mat;
                s.updateWhenOffscreen = true;
                s.transform.SetParent(modelo.transform, false);
                s.gameObject.name = r.pieza;
            }
            Object.DestroyImmediate(pieza);
        }

        // Animator en el modelo nuevo
        Animator anim = modelo.GetComponent<Animator>();
        if (anim == null) anim = modelo.AddComponent<Animator>();
        anim.avatar = avatar;
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // ---------- 5. Mover sockets (mano y espalda) ----------
        Transform manoNueva = anim.GetBoneTransform(HumanBodyBones.RightHand);
        Transform pechoNuevo = anim.GetBoneTransform(HumanBodyBones.Chest) ?? anim.GetBoneTransform(HumanBodyBones.Spine);
        foreach (Transform t in jugador.GetComponentsInChildren<Transform>(true).ToArray())
        {
            if (t == null) continue;
            if (t.name == "SocketMano" && manoNueva != null)
            {
                t.SetParent(manoNueva, false);
                t.localPosition = new Vector3(0f, 0.08f, 0.02f);
            }
            if (t.name == "BackpackSocket" && pechoNuevo != null)
            {
                t.SetParent(pechoNuevo, true);
            }
        }

        // ---------- 6. Quitar el cuerpo del X Bot ----------
        if (animViejo != null) Undo.DestroyObjectImmediate(animViejo);
        foreach (Transform hijo in jugador.transform.Cast<Transform>().ToArray())
        {
            if (hijo == modelo.transform) continue;
            if (hijo.GetComponentInChildren<FlashlightItem>(true) != null || hijo.GetComponentInChildren<BackpackPickup>(true) != null)
                continue; // la linterna o la mochila (si ya estaban recogidas) se quedan
            if (hijo.name.StartsWith("mixamorig") || hijo.GetComponent<SkinnedMeshRenderer>() != null)
                Undo.DestroyObjectImmediate(hijo.gameObject);
        }

        jugador.name = "Jugador_Senderista";
        if (jugador.tag == "Untagged") jugador.tag = "Player";

        // Cápsula a la altura del nuevo modelo (~1.75 m)
        CharacterController cc = jugador.GetComponent<CharacterController>();
        if (cc != null) { cc.height = 1.75f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.9f, 0); }

        // Avisar a los scripts que cambió el Animator
        foreach (var c in jugador.GetComponents<MonoBehaviour>()) EditorUtility.SetDirty(c);
        CameraController cam = Object.FindAnyObjectByType<CameraController>();
        if (cam != null) { cam.objetivo = jugador.transform; EditorUtility.SetDirty(cam); }

        // Créditos (licencia CC BY 4.0)
        EscribirCreditos();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = jugador;
        Debug.Log("[Monterrey] Protagonista senderista listo, con todos los componentes del X Bot.");
    }

    // ---------------------------------------------------------------- Importación

    static void ConfigurarTextura()
    {
        TextureImporter ti = AssetImporter.GetAtPath(Atlas) as TextureImporter;
        if (ti == null) return;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.maxTextureSize = 1024;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
    }

    static Avatar ConfigurarCuerpo()
    {
        ModelImporter imp = AssetImporter.GetAtPath(Cuerpo) as ModelImporter;
        if (imp == null) return null;
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.materialImportMode = ModelImporterMaterialImportMode.None;
        imp.importAnimation = true;
        var clips = imp.defaultClipAnimations;
        foreach (var c in clips)
        {
            bool loop = c.name.Contains("idle") || c.name.Contains("walk") || c.name.Contains("run") ||
                        c.name.Contains("cower") || c.name.Contains("talk") || c.name.Contains("sit") || c.name.Contains("wave");
            c.loopTime = loop; c.loopPose = loop;
            c.lockRootRotation = true; c.keepOriginalOrientation = true;
            c.lockRootHeightY = true; c.keepOriginalPositionY = true;
            c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
        }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(Cuerpo).OfType<Avatar>().FirstOrDefault();
    }

    static void ConfigurarPieza(string ruta)
    {
        ModelImporter imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
        if (imp == null) return;
        imp.animationType = ModelImporterAnimationType.Generic;
        imp.importAnimation = false;
        imp.materialImportMode = ModelImporterMaterialImportMode.None;
        imp.SaveAndReimport();
    }

    static Material CrearMaterial(string nombre, Texture2D tex, Color color)
    {
        string carpeta = Carpeta + "/Materiales";
        if (!AssetDatabase.IsValidFolder(carpeta)) AssetDatabase.CreateFolder(Carpeta, "Materiales");
        string ruta = carpeta + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", 0.05f);
        m.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------------------------------------------------------- Animator

    static AnimationClip Clip(string ruta, bool loop)
    {
        ModelImporter imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
        if (imp == null) return null;
        if (imp.animationType != ModelImporterAnimationType.Human)
        {
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.SaveAndReimport();
        }
        var clips = imp.defaultClipAnimations;
        foreach (var c in clips)
        {
            c.loopTime = loop; c.loopPose = loop;
            c.lockRootRotation = true; c.keepOriginalOrientation = true;
            c.lockRootHeightY = true; c.keepOriginalPositionY = true;
            c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
        }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    static AnimationClip ClipDelCuerpo(string contiene)
    {
        return AssetDatabase.LoadAllAssetsAtPath(Cuerpo).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__") && c.name.ToLower().Contains(contiene));
    }

    static AnimatorController CrearControlador()
    {
        AnimationClip idle = Clip(CarpetaAnim + "/Player Idle.fbx", true);
        AnimationClip walk = Clip(CarpetaAnim + "/Player Walking.fbx", true);
        AnimationClip run = Clip(CarpetaAnim + "/Player Running.fbx", true);
        AnimationClip cIdle = Clip(CarpetaAnim + "/Player Crouch Idle.fbx", true);
        AnimationClip cWalk = Clip(CarpetaAnim + "/Player Crouch Walking.fbx", true);
        AnimationClip punch = Clip(CarpetaAnim + "/Player Punching.fbx", false);
        AnimationClip hit = Clip(CarpetaAnim + "/Player Hit Reaction.fbx", false);
        AnimationClip cower = ClipDelCuerpo("cower") ?? cIdle;

        if (AssetDatabase.LoadAssetAtPath<Object>(RutaControlador) != null) AssetDatabase.DeleteAsset(RutaControlador);
        AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter("Agachado", AnimatorControllerParameterType.Bool);
        c.AddParameter("Escondido", AnimatorControllerParameterType.Bool);
        c.AddParameter("Golpe", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Golpeado", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = c.layers[0].stateMachine;

        // Caminar / correr
        AnimatorState loco = c.CreateBlendTreeInController("Locomocion", out BlendTree bt, 0);
        bt.blendParameter = "Speed";
        bt.useAutomaticThresholds = false;
        bt.AddChild(idle, 0f);
        bt.AddChild(walk, 0.5f);
        bt.AddChild(run, 1f);
        sm.defaultState = loco;

        // Agachado
        AnimatorState agachado = c.CreateBlendTreeInController("Agachado", out BlendTree bt2, 0);
        bt2.blendParameter = "Speed";
        bt2.useAutomaticThresholds = false;
        bt2.AddChild(cIdle, 0f);
        bt2.AddChild(cWalk, 0.25f);

        AnimatorState escondido = sm.AddState("Escondido");
        escondido.motion = cower;
        AnimatorState golpe = sm.AddState("Golpe");
        golpe.motion = punch;
        AnimatorState golpeado = sm.AddState("Golpeado");
        golpeado.motion = hit;

        Transicion(loco, agachado, "Agachado", true);
        Transicion(agachado, loco, "Agachado", false);
        Transicion(loco, escondido, "Escondido", true);
        Transicion(agachado, escondido, "Escondido", true);
        Transicion(escondido, loco, "Escondido", false);

        // Golpe y recibir golpe desde cualquier estado, y regresan solos
        var t1 = sm.AddAnyStateTransition(golpe);
        t1.AddCondition(AnimatorConditionMode.If, 0, "Golpe");
        t1.duration = 0.08f; t1.canTransitionToSelf = false;
        var s1 = golpe.AddTransition(loco); s1.hasExitTime = true; s1.exitTime = 0.85f; s1.duration = 0.15f;

        var t2 = sm.AddAnyStateTransition(golpeado);
        t2.AddCondition(AnimatorConditionMode.If, 0, "Golpeado");
        t2.duration = 0.05f; t2.canTransitionToSelf = false;
        var s2 = golpeado.AddTransition(loco); s2.hasExitTime = true; s2.exitTime = 0.85f; s2.duration = 0.15f;

        AssetDatabase.SaveAssets();
        return c;
    }

    static void Transicion(AnimatorState de, AnimatorState a, string param, bool valor)
    {
        var t = de.AddTransition(a);
        t.hasExitTime = false;
        t.duration = 0.2f;
        t.AddCondition(valor ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
    }

    static void EscribirCreditos()
    {
        string ruta = "Assets/CREDITOS.txt";
        string linea = "PSX Character Creator Free by heyheythere - https://heyheythere.itch.io/psx-character-creator-free - CC BY 4.0";
        string actual = System.IO.File.Exists(ruta) ? System.IO.File.ReadAllText(ruta) : "MONTERREY LIGHT NIGHT - CREDITOS DE TERCEROS\n\n";
        if (!actual.Contains(linea)) actual += linea + "\nAnimaciones: Mixamo (Adobe) - uso libre en proyectos\n";
        System.IO.File.WriteAllText(ruta, actual);
        AssetDatabase.ImportAsset(ruta);
    }
}
#endif
