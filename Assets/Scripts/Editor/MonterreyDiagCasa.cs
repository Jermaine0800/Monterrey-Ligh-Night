using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MonterreyDiagCasa
{
    [MenuItem("Monterrey/99. Diagnóstico casa")]
    public static void Diag()
    {
        GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ALP_Assets/country house01/Prefabs/House_Prefab.prefab");
        GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(pf);
        g.transform.position = new Vector3(0, -500, 0);
        StringBuilder sb = new StringBuilder();
        foreach (Transform t in g.GetComponentsInChildren<Transform>(true))
        {
            Renderer r = t.GetComponent<Renderer>();
            string path = t.name; Transform p = t.parent; while (p != null && p != g.transform) { path = p.name + "/" + path; p = p.parent; }
            sb.AppendLine(path + " | lpos " + (t.position - g.transform.position).ToString("F2") + " rot " + t.eulerAngles.ToString("F0") + " scl " + t.lossyScale.ToString("F2")
                + (r != null ? " | b " + (r.bounds.center - g.transform.position).ToString("F2") + " sz " + r.bounds.size.ToString("F2") + " mats " + r.sharedMaterials.Length + " col " + (t.GetComponent<Collider>() != null) : "")
                + " | comps " + string.Join(",", System.Array.ConvertAll(t.GetComponents<Component>(), c => c != null ? c.GetType().Name : "null")));
        }
        File.WriteAllText("Temp/diag_casa.txt", sb.ToString());
        Object.DestroyImmediate(g);
        Debug.Log("[Diag] casa listo");
    }
}
