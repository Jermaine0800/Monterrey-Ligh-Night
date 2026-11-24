using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Menú 13: genera las voces del protagonista con la síntesis de voz de Windows (gratis, sin internet),
/// buscando una voz MASCULINA en español de México / Latinoamérica. No clona a ninguna persona real.
/// Las guarda en Assets/Resources/Voces/&lt;id&gt;.wav. En el juego se les pone filtro de videocámara.
/// </summary>
public static class MonterreyVocesSetup
{
    const string Carpeta = "Assets/Resources/Voces";

    [MenuItem("Monterrey/13. Generar voces del protagonista (Windows, español latino)")]
    public static void Generar()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor) { EditorUtility.DisplayDialog("Voces", "Solo funciona en Windows.", "OK"); return; }
        Directory.CreateDirectory(Carpeta);
        GenerarEn(VozProtagonista.Frases, Path.GetFullPath(Carpeta));
    }

    /// <summary>Menú 19: voces de la cinemática de inicio (Mario, Chuy y tú) → D:\Proyectos\Cinematica_audio\voces</summary>
    [MenuItem("Monterrey/19. Voces de la cinemática de inicio")]
    public static void VocesCinematica()
    {
        string carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Cinematica_audio/voces"));
        Directory.CreateDirectory(carpeta);
        var frases = new System.Collections.Generic.Dictionary<string, string>
        {
            { "m1", "Según el foro, la entrada de la Línea K está por aquí. Nunca la terminaron." },
            { "c1", "Ya es bien noche, güey. ¿Y si nos regresamos?" },
            { "m2", "Mira. Expediente cero ocho. Es aquí." },
            { "p1", "Sigue grabando... sigue grabando." },
            { "c2", "Esperen... ¿oyeron eso?" },
            { "m3", "Hay alguien allá al fondo... ¿Hola?" },
            { "c3", "No es una persona... ¡no es una persona!" },
            { "m4", "¡Corre! ¡Corre!" },
            { "p2", "No... no puedo moverme..." },
            { "p3", "¿Mario? ¿Chuy? ¿Son ustedes?" },
            { "m5", "Ven... ya se acabó. Ya nos podemos ir." },
            { "c4", "¡No voltees! ¡Apúrate!" },
        };
        GenerarEn(frases, carpeta);
    }

    static void GenerarEn(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> lista, string carpetaAbs)
    {
        string ps1 = Path.GetFullPath("Temp/voces.ps1");
        string log = Path.GetFullPath("Temp/voces.log");

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("$ErrorActionPreference = 'Continue'");
        sb.AppendLine("$log = '" + log + "'; '' | Out-File $log -Encoding utf8");
        sb.AppendLine("$frases = @{}");
        foreach (var f in lista) sb.AppendLine("$frases['" + f.Key + "'] = '" + f.Value.Replace("'", "''") + "'");
        sb.AppendLine("$salida = '" + carpetaAbs + "'");
        sb.AppendLine(@"
$hecho = $false
try {
  Add-Type -AssemblyName System.Runtime.WindowsRuntime
  $asTask = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
  function Esperar($op, $tipo) { $t = $asTask.MakeGenericMethod($tipo).Invoke($null, @($op)); $t.Wait(-1) | Out-Null; $t.Result }
  [Windows.Media.SpeechSynthesis.SpeechSynthesizer,Windows.Media.SpeechSynthesis,ContentType=WindowsRuntime] | Out-Null
  $todas = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices
  ""Voces WinRT: "" + (($todas | ForEach-Object { $_.DisplayName + ' (' + $_.Language + ', ' + $_.Gender + ')' }) -join '; ') | Out-File $log -Append -Encoding utf8
  $voz = $todas | Where-Object { $_.Language -eq 'es-MX' -and $_.Gender -eq 'Male' } | Select-Object -First 1
  if (-not $voz) { $voz = $todas | Where-Object { $_.Language -like 'es-*' -and $_.Language -ne 'es-ES' -and $_.Gender -eq 'Male' } | Select-Object -First 1 }
  if (-not $voz) { $voz = $todas | Where-Object { $_.Language -like 'es-*' -and $_.Gender -eq 'Male' } | Select-Object -First 1 }
  if (-not $voz) { $voz = $todas | Where-Object { $_.Language -like 'es-*' } | Select-Object -First 1 }
  if ($voz) {
    ""Usando: "" + $voz.DisplayName | Out-File $log -Append -Encoding utf8
    $s = New-Object Windows.Media.SpeechSynthesis.SpeechSynthesizer
    $s.Voice = $voz
    foreach ($id in $frases.Keys) {
      $texto = [System.Security.SecurityElement]::Escape($frases[$id])
      $ssml = ""<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='"" + $voz.Language + ""'><prosody rate='-12%' pitch='-6%'>"" + $texto + ""</prosody></speak>""
      $stream = Esperar ($s.SynthesizeSsmlToStreamAsync($ssml)) ([Windows.Media.SpeechSynthesis.SpeechSynthesisStream])
      $net = [System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($stream)
      $fs = [System.IO.File]::Create((Join-Path $salida ($id + '.wav')))
      $net.CopyTo($fs); $fs.Close(); $net.Close()
    }
    $hecho = $true
  }
} catch { ""WinRT falló: "" + $_ | Out-File $log -Append -Encoding utf8 }

if (-not $hecho) {
  Add-Type -AssemblyName System.Speech
  $s = New-Object System.Speech.Synthesis.SpeechSynthesizer
  $lista = $s.GetInstalledVoices() | ForEach-Object { $_.VoiceInfo }
  ""Voces SAPI: "" + (($lista | ForEach-Object { $_.Name + ' (' + $_.Culture + ', ' + $_.Gender + ')' }) -join '; ') | Out-File $log -Append -Encoding utf8
  $v = $lista | Where-Object { $_.Culture.Name -like 'es-*' -and $_.Gender -eq 'Male' } | Select-Object -First 1
  if (-not $v) { $v = $lista | Where-Object { $_.Culture.Name -like 'es-*' } | Select-Object -First 1 }
  if ($v) { $s.SelectVoice($v.Name); ""Usando SAPI: "" + $v.Name | Out-File $log -Append -Encoding utf8 }
  $s.Rate = -1
  foreach ($id in $frases.Keys) { $s.SetOutputToWaveFile((Join-Path $salida ($id + '.wav'))); $s.Speak($frases[$id]) }
  $s.SetOutputToNull()
}
'FIN' | Out-File $log -Append -Encoding utf8
");
        File.WriteAllText(ps1, sb.ToString(), new UTF8Encoding(true));

        ProcessStartInfo info = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File \"" + ps1 + "\"")
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        using (Process p = Process.Start(info))
        {
            string err = p.StandardError.ReadToEnd();
            p.WaitForExit(120000);
            if (!string.IsNullOrEmpty(err)) Debug.LogWarning("[Voces] " + err);
        }
        AssetDatabase.Refresh();
        Debug.Log("[Voces] " + (File.Exists(log) ? File.ReadAllText(log) : "sin log"));
    }
}
