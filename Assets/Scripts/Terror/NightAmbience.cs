using UnityEngine;

/// <summary>
/// Convierte la escena en noche al darle Play, sin tocar tu configuración guardada:
/// luna tenue, neblina negra-azulada, sin cielo de día y sin lightmaps de día.
/// </summary>
public class NightAmbience : MonoBehaviour
{
    [Header("Luna (Directional Light)")]
    public Color colorLuna = new Color(0.45f, 0.52f, 0.72f);
    [Range(0, 1)] public float luzLuna = 0.25f;

    [Header("Ambiente")]
    public Color ambienteNoche = new Color(0.06f, 0.065f, 0.09f);

    [Header("Neblina")]
    public Color neblinaColor = new Color(0.025f, 0.027f, 0.035f);
    [Range(0, 0.2f)] public float neblina = 0.045f;

    [Header("Brillo general")]
    [Tooltip("Exposición extra de la cámara (post-proceso). 0 = sin cambio, 1 = el doble de luz.")]
    [Range(-1, 3)] public float exposicion = 0.9f;
    [Tooltip("Tamaño del tramado PSX (cuadritos). El original de VNTG (0.2) se ve como tablero de ajedrez de noche.")]
    [Range(0, 1)] public float tramadoPSX = 0f;
    [Tooltip("Qué tan fuerte se ven los filtros CRT/VHS (1 = como venían, 0.5 = a la mitad)")]
    [Range(0, 1)] public float intensidadFiltros = 0.35f;
    [Tooltip("Resolución vertical del efecto PSX (pixeleado). 240 = PS1, 360 = más nítido.")]
    public float resolucionPSX = 360f;

    [Header("Extras")]
    [Tooltip("Quita el skybox de día y pone fondo del color de la neblina")]
    public bool quitarCielo = true;
    [Tooltip("Ignora la iluminación horneada de día (Lightmap). Recomendado hasta que vuelvas a hornear de noche.")]
    public bool quitarLightmapsDeDia = true;

    void Start()
    {
        float brillo = PlayerPrefs.GetFloat("brillo", 1f);
        AudioListener.volume = PlayerPrefs.GetFloat("volumen", 0.8f);

        Light sol = RenderSettings.sun;
        if (sol == null)
        {
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sol = l; break; }
        }
        if (sol != null)
        {
            sol.color = colorLuna;
            sol.intensity = luzLuna * brillo;
            sol.shadows = LightShadows.Soft;
            sol.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambienteNoche * brillo;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = neblinaColor;
        RenderSettings.fogDensity = neblina;

        RenderSettings.reflectionIntensity = 0.15f;

        if (quitarCielo)
        {
            RenderSettings.skybox = null;
            foreach (Camera c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = neblinaColor;
            }
        }

        if (quitarLightmapsDeDia) LightmapSettings.lightmaps = new LightmapData[0];

        // Exposición: se suma al perfil del Global Volume (copia en tiempo de ejecución, no toca el asset)
        foreach (UnityEngine.Rendering.Volume v in FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Exclude))
        {
            if (!v.isGlobal || v.profile == null) continue;
            UnityEngine.Rendering.VolumeProfile perfil = v.profile;   // instancia propia en Play
            if (!perfil.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments ca))
                ca = perfil.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
            ca.postExposure.overrideState = true;
            ca.postExposure.value = exposicion + Mathf.Log(Mathf.Max(0.1f, brillo), 2f);
            break;
        }

        SuavizarTramado();
        DynamicGI.UpdateEnvironment();
    }

    /// <summary>
    /// Ajusta el efecto PSX de VNTG. La escena no tenía override, así que usaba los valores por defecto
    /// (256x256 de resolución y tramado grande = tablero de ajedrez). Aquí se agrega uno en tiempo de ejecución.
    /// </summary>
    void SuavizarTramado()
    {
        foreach (UnityEngine.Rendering.Volume v in FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Exclude))
        {
            if (!v.isGlobal || v.profile == null) continue;
            UnityEngine.Rendering.VolumeProfile perfil = v.profile;
            if (!perfil.TryGet(out ColbyO.VNTG.PSX.PSXEffectSettings psx))
                psx = perfil.Add<ColbyO.VNTG.PSX.PSXEffectSettings>(false);
            psx.active = true;
            psx.Enabled.Override(true);
            psx.PixelResolution.Override(new Vector2(resolucionPSX * 16f / 9f, resolucionPSX));
            psx.ColorPrecision.Override(64f);
            psx.EnableDither.Override(tramadoPSX > 0.001f);
            psx.DitherScale.Override(tramadoPSX);
            psx.AmbientColor.Override(ambienteNoche * 0.5f);

            // Filtros CRT / VHS a la mitad (los valores del perfil se multiplican por intensidadFiltros)
            if (perfil.TryGet(out ColbyO.VNTG.CRT.CRTSettings crt))
            {
                float k = intensidadFiltros;
                crt.ScreenBend.Override(crt.ScreenBend.value * k);
                crt.ScanLineStrength.Override(crt.ScanLineStrength.value * k);
                crt.ScanLineHorizontalOpacity.Override(crt.ScanLineHorizontalOpacity.value * k);
                crt.NoiseScale.Override(crt.NoiseScale.value * k);
                crt.InterferenceAmplitude.Override(crt.InterferenceAmplitude.value * k);
                crt.GlitchChance.Override(crt.GlitchChance.value * k);
                crt.TrackingJitter.Override(crt.TrackingJitter.value * k);
                crt.ChromaticOffset.Override(crt.ChromaticOffset.value * k);
                crt.UnsharpAmount.Override(crt.UnsharpAmount.value * k);
                crt.VignetteOpacity.Override(crt.VignetteOpacity.value * k);
                if (k < 0.75f) crt.EnableSubPixels.Override(false);   // la rejilla de subpíxeles es lo que más "ensucia"
            }
            break;
        }
    }
}
