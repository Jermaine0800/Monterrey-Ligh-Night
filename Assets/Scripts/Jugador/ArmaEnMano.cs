using UnityEngine;

/// <summary>
/// El arma equipada va empuñada en la MANO DERECHA (la que golpea) y, al pegar, hace un arco de golpe:
/// se levanta y baja con fuerza hacia el frente. Funciona con cualquier modelo de arma: busca su lado más largo
/// (el mango) y lo alinea con el brazo.
/// </summary>
public class ArmaEnMano : MonoBehaviour
{
    public Transform mano;      // hueso de la mano derecha (puede ser null)
    public Transform duenio;    // el jugador

    Quaternion alinear = Quaternion.identity;   // del eje largo del modelo a "arriba"
    Vector3 centroLocal;                        // centro del modelo (espacio local)
    float largo = 0.8f;
    float golpeT = -1f;
    const float DuracionGolpe = 0.38f;

    void Start()
    {
        // Medir el modelo sin rotación para saber cuál es su eje largo
        Quaternion r0 = transform.rotation; transform.rotation = Quaternion.identity;
        Renderer[] rs = GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds; foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            Vector3 s = b.size;
            Vector3 eje = s.x >= s.y && s.x >= s.z ? Vector3.right : (s.y >= s.z ? Vector3.up : Vector3.forward);
            largo = Mathf.Max(s.x, Mathf.Max(s.y, s.z));
            alinear = Quaternion.FromToRotation(eje, Vector3.up);
            centroLocal = transform.InverseTransformPoint(b.center);
        }
        transform.rotation = r0;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
    }

    public void Golpear() { golpeT = 0f; }

    void LateUpdate()
    {
        if (duenio == null) return;
        // Ángulo del arco: reposo apuntando arriba-adelante; al golpear sube atrás y baja con fuerza
        float ang = 0f;
        if (golpeT >= 0f)
        {
            golpeT += Time.deltaTime;
            float k = golpeT / DuracionGolpe;
            ang = k < 0.3f ? Mathf.Lerp(0f, -45f, k / 0.3f) : Mathf.Lerp(-45f, 105f, Mathf.SmoothStep(0f, 1f, (k - 0.3f) / 0.7f));
            if (k >= 1f) golpeT = -1f;
        }
        // En primera persona (la cámara está en la cabeza) el arma se ve abajo a la derecha de la pantalla
        Camera cam = Camera.main;
        bool primera = cam != null && Vector3.Distance(cam.transform.position, duenio.position + Vector3.up * 1.6f) < 0.7f;
        Vector3 adelante = primera ? cam.transform.forward : duenio.forward;
        Vector3 arriba = primera ? cam.transform.up : Vector3.up;
        Vector3 derecha = primera ? cam.transform.right : duenio.right;
        Vector3 reposo = primera ? (adelante * 1.0f + arriba * 0.55f - derecha * 0.2f).normalized : (adelante * 0.45f + arriba * 0.9f).normalized;
        Vector3 dir = Quaternion.AngleAxis(ang, derecha) * reposo;
        Quaternion giro = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(0f, (primera ? cam.transform : duenio).eulerAngles.y, 0f);
        transform.rotation = giro * alinear;

        Vector3 empunadura = primera
            ? cam.transform.position + derecha * 0.36f - arriba * 0.42f + adelante * 0.3f
            : (mano != null ? mano.position : duenio.position + Vector3.up * 1.15f + duenio.right * 0.32f + duenio.forward * 0.3f);
        // el mango queda en la mano: el centro del arma va "largo/2" más allá, en la dirección del arma
        Vector3 centroMundo = empunadura + dir * (largo * 0.38f);
        transform.position = centroMundo - (transform.rotation * Vector3.Scale(centroLocal, transform.lossyScale));
    }
}
