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

        DynamicGI.UpdateEnvironment();
    }
}
