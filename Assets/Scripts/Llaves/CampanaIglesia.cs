using System.Collections;
using UnityEngine;

/// <summary>Campana de la capilla: se balancea sola y suena.</summary>
public class CampanaIglesia : MonoBehaviour
{
    static AudioClip tan;
    public void Tocar(int veces) { StartCoroutine(Rutina(veces)); }

    IEnumerator Rutina(int veces)
    {
        if (tan == null) tan = Campanada();
        Quaternion base0 = transform.localRotation;
        for (int i = 0; i < veces; i++)
        {
            AudioSource.PlayClipAtPoint(tan, transform.position, 1f);
            for (float t = 0; t < 1.6f; t += Time.deltaTime)
            {
                transform.localRotation = base0 * Quaternion.Euler(Mathf.Sin(t * Mathf.PI * 2f / 1.6f) * 28f * (i % 2 == 0 ? 1 : -1), 0, 0);
                yield return null;
            }
        }
        transform.localRotation = base0;
    }

    static AudioClip Campanada()
    {
        int hz = 44100, n = hz * 4;
        float[] d = new float[n];
        float[] f = { 196f, 392f, 466f, 587f, 784f };
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / hz, s = 0;
            for (int k = 0; k < f.Length; k++) s += Mathf.Sin(2 * Mathf.PI * f[k] * t * (1f + k * 0.0015f)) * Mathf.Exp(-t * (0.6f + k * 0.5f)) / (k + 1);
            d[i] = s * 0.5f;
        }
        AudioClip a = AudioClip.Create("Campanada", n, 1, hz, false);
        a.SetData(d, 0);
        return a;
    }
}
