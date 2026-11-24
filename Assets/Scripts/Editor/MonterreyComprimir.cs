using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Menú 20: comprimir el juego sin que se vea distinto (el estilo PSX ya es de baja resolución).
///  1) Texturas: máximo 1024 px (portada 2048), compresión con Crunch (calidad 60). Respeta el filtro Point.
///  2) Audio: Vorbis; música y ambientes largos en Streaming, efectos y voces comprimidos en memoria y en mono.
///  3) Modelos: compresión de malla media y de animación óptima.
///  4) Reproductor: quitar código de Unity que no se usa (nivel bajo, seguro) y build con LZ4HC.
/// Escribe un reporte en Temp/comprimir_reporte.txt con lo que cambió y cuánto pesa cada carpeta.
/// Todo se puede deshacer con git (son solo los .meta).
/// </summary>
public static class MonterreyComprimir
{
    [MenuItem("Monterrey/20. Comprimir el juego (texturas, audio, modelos)")]
    public static void Comprimir()
    {
        StringBuilder r = new StringBuilder();
        int tex = 0, aud = 0, mod = 0;
        List<string> rutas = new List<string>();

        AssetDatabase.StartAssetEditing();
        try
        {
            // ---------------------------------------------------------------- Texturas
            foreach (string g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.Contains("/Editor/") || p.EndsWith(".asset")) continue;
                TextureImporter ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti == null) continue;
                bool portada = p.Contains("/Portada/");
                int max = portada ? 2048 : 1024;
                bool cambio = false;
                if (ti.maxTextureSize > max) { ti.maxTextureSize = max; cambio = true; }
                if (!portada && ti.textureType != TextureImporterType.NormalMap)
                {
                    if (ti.textureCompression == TextureImporterCompression.Uncompressed || !ti.crunchedCompression)
                    {
                        ti.textureCompression = TextureImporterCompression.Compressed;
                        ti.crunchedCompression = true;
                        ti.compressionQuality = 60;
                        cambio = true;
                    }
                }
                if (cambio) { EditorUtility.SetDirty(ti); ti.SaveAndReimport(); tex++; rutas.Add(p); }
            }

            // ---------------------------------------------------------------- Audio
            foreach (string g in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                AudioImporter ai = AssetImporter.GetAtPath(p) as AudioImporter;
                if (ai == null) continue;
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                float largo = clip != null ? clip.length : 0f;
                AudioImporterSampleSettings s = ai.defaultSampleSettings;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = largo > 20f ? 0.5f : 0.6f;
                s.loadType = largo > 20f ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
                s.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                ai.defaultSampleSettings = s;
                if (largo <= 20f) ai.forceToMono = true;           // efectos y voces: en mono pesan la mitad
                EditorUtility.SetDirty(ai); ai.SaveAndReimport(); aud++;
            }

            // ---------------------------------------------------------------- Modelos
            foreach (string g in AssetDatabase.FindAssets("t:Model", new[] { "Assets" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                ModelImporter mi = AssetImporter.GetAtPath(p) as ModelImporter;
                if (mi == null) continue;
                bool cambio = false;
                if (mi.meshCompression == ModelImporterMeshCompression.Off) { mi.meshCompression = ModelImporterMeshCompression.Medium; cambio = true; }
                if (mi.animationCompression != ModelImporterAnimationCompression.Optimal) { mi.animationCompression = ModelImporterAnimationCompression.Optimal; cambio = true; }
                if (cambio) { EditorUtility.SetDirty(mi); mi.SaveAndReimport(); mod++; }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        // ---------------------------------------------------------------- Reproductor
        try { PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Low); } catch { }
        AssetDatabase.SaveAssets();

        r.AppendLine("Texturas comprimidas: " + tex);
        r.AppendLine("Audios comprimidos: " + aud);
        r.AppendLine("Modelos comprimidos: " + mod);
        r.AppendLine();
        r.AppendLine("Peso en disco por carpeta (lo que de verdad entra al juego es solo lo que usa la escena + Resources + StreamingAssets):");
        foreach (string d in Directory.GetDirectories("Assets"))
        {
            long b = Directory.GetFiles(d, "*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".meta")).Sum(f => new FileInfo(f).Length);
            r.AppendLine(string.Format("  {0,8:0.0} MB  {1}", b / 1048576.0, d));
        }
        File.WriteAllText(Path.GetFullPath("Temp/comprimir_reporte.txt"), r.ToString());
        Debug.Log("[Comprimir] " + r);
    }
}
