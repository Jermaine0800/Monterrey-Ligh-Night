using UnityEngine;

/// <summary>
/// Animación procedural encima de la del Animator para que los monstruos no se vean tiesos:
/// respiración, cabeza que se ladea y da "tirones" bruscos, hombros que se encogen,
/// y al correr se inclinan hacia adelante y rebotan. Funciona con esqueletos humanoides (Mixamo).
/// </summary>
[DefaultExecutionOrder(500)]
public class EnemigoVida : MonoBehaviour
{
    Animator anim;
    Transform cabeza, cuello, pecho, columna, hombroI, hombroD;
    Vector3 ultimaPos;
    float velocidad, semilla, tiron, siguienteTiron, ladoTiron;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        semilla = Random.value * 50f;
        ultimaPos = transform.position;
        if (anim != null && anim.isHuman)
        {
            cabeza = anim.GetBoneTransform(HumanBodyBones.Head);
            cuello = anim.GetBoneTransform(HumanBodyBones.Neck);
            pecho = anim.GetBoneTransform(HumanBodyBones.Chest);
            columna = anim.GetBoneTransform(HumanBodyBones.Spine);
            hombroI = anim.GetBoneTransform(HumanBodyBones.LeftShoulder);
            hombroD = anim.GetBoneTransform(HumanBodyBones.RightShoulder);
        }
        else
        {
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLower();
                if (cabeza == null && n.Contains("head")) cabeza = t;
                else if (cuello == null && n.Contains("neck")) cuello = t;
                else if (pecho == null && (n.Contains("chest") || n.Contains("spine2"))) pecho = t;
                else if (columna == null && n.Contains("spine")) columna = t;
            }
        }
    }

    void LateUpdate()
    {
        if (cabeza == null && pecho == null) return;
        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null && !r.enabled) return;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 d = transform.position - ultimaPos; d.y = 0;
        velocidad = Mathf.Lerp(velocidad, d.magnitude / dt, 6f * dt);
        ultimaPos = transform.position;
        float t = Time.time + semilla;
        bool corriendo = velocidad > 2.5f;

        // Tirón brusco de cabeza cada pocos segundos (muy de película de terror)
        if (Time.time > siguienteTiron)
        {
            siguienteTiron = Time.time + Random.Range(1.5f, 4.5f);
            tiron = 1f; ladoTiron = Random.value < 0.5f ? -1f : 1f;
        }
        tiron = Mathf.MoveTowards(tiron, 0f, dt * 3.5f);
        float golpe = tiron > 0.75f ? 1f : tiron / 0.75f;

        if (cabeza != null)
        {
            float lado = Mathf.Sin(t * 0.7f) * 10f + golpe * 32f * ladoTiron;
            float frente = Mathf.Sin(t * 1.3f) * 4f + (corriendo ? -10f : 6f);
            cabeza.localRotation *= Quaternion.Euler(frente, Mathf.Sin(t * 0.45f) * 8f, lado);
        }
        if (cuello != null) cuello.localRotation *= Quaternion.Euler(0f, 0f, golpe * 10f * ladoTiron);
        // Respiración pesada y espalda encorvada; al correr se inclinan hacia adelante
        float resp = Mathf.Sin(t * (corriendo ? 9f : 2.2f));
        if (pecho != null) pecho.localRotation *= Quaternion.Euler(resp * (corriendo ? 4f : 3f) + (corriendo ? 14f : 6f), 0f, Mathf.Sin(t * 0.9f) * 3f);
        if (columna != null) columna.localRotation *= Quaternion.Euler(corriendo ? 12f : 4f, Mathf.Sin(t * (corriendo ? 9f : 0.6f)) * (corriendo ? 6f : 2f), 0f);
        if (hombroI != null) hombroI.localRotation *= Quaternion.Euler(0f, 0f, 6f + resp * 3f);
        if (hombroD != null) hombroD.localRotation *= Quaternion.Euler(0f, 0f, -6f - resp * 3f);
    }
}
