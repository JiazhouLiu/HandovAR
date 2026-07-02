using UnityEngine;

[ExecuteAlways]  // runs in edit mode too, so you see the arc without pressing Play
public class CurvedLayout : MonoBehaviour
{
    [Header("Panels to arrange (drag them here, or use children)")]
    public Transform[] panels;
    public bool useChildren = true;   // if true, ignores the array and uses child objects

    [Header("Curve settings")]
    public float radius = 1.5f;       // distance from centre (metres)
    [Range(0f, 180f)]
    public float arcDegrees = 80f;    // total spread — 80 = shallow arc, 180 = full hemisphere
    public float heightOffset = 0f;   // raise/lower the whole arc

    [Header("Options")]
    public bool facePanelsInward = true;   // panels turn to face the centre (the nurse)
    public bool autoUpdate = true;         // re-arrange whenever you change a value

    void OnValidate()
    {
        if (autoUpdate) Arrange();
    }

    [ContextMenu("Arrange Now")]   // right-click the component → "Arrange Now"
    public void Arrange()
    {
        Transform[] items = useChildren ? GetChildren() : panels;
        if (items == null || items.Length == 0) return;

        int n = items.Length;
        for (int i = 0; i < n; i++)
        {
            if (items[i] == null) continue;

            // spread panels evenly across the arc
            float t = (n == 1) ? 0.5f : i / (float)(n - 1);
            float angleDeg = Mathf.Lerp(-arcDegrees / 2f, arcDegrees / 2f, t);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            // position on a horizontal arc in front of the centre
            Vector3 pos = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad)) * radius;
            pos.y += heightOffset;
            items[i].localPosition = pos;

            // rotate each panel to face the centre (so it faces the nurse)
            if (facePanelsInward)
                items[i].localRotation = Quaternion.LookRotation(pos);
        }
    }

    Transform[] GetChildren()
    {
        Transform[] result = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            result[i] = transform.GetChild(i);
        return result;
    }
}
