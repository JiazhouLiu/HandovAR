using UnityEngine;

[ExecuteAlways]
public class SphericalLayout : MonoBehaviour
{
    [Header("Grid size")]
    public int columns = 3;
    public int rows = 2;

    [Header("Sphere settings")]
    public float radius = 1.5f;
    [Range(0f, 180f)] public float horizontalArc = 90f;  // left-right spread
    [Range(0f, 120f)] public float verticalArc = 60f;    // up-down spread

    [Header("Options")]
    public bool faceCentre = true;
    public bool autoUpdate = true;

    void OnValidate() { if (autoUpdate) Arrange(); }

    [ContextMenu("Arrange Now")]
    public void Arrange()
    {
        int count = transform.childCount;
        if (count == 0) return;

        int index = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (index >= count) return;
                Transform panel = transform.GetChild(index);

                // spread evenly across each arc
                float tx = (columns == 1) ? 0.5f : c / (float)(columns - 1);
                float ty = (rows == 1) ? 0.5f : r / (float)(rows - 1);

                float az = Mathf.Lerp(-horizontalArc / 2f, horizontalArc / 2f, tx) * Mathf.Deg2Rad;
                float el = Mathf.Lerp(-verticalArc / 2f, verticalArc / 2f, ty) * Mathf.Deg2Rad;

                // point on a sphere
                Vector3 pos = new Vector3(
                    Mathf.Sin(az) * Mathf.Cos(el),
                    Mathf.Sin(el),
                    Mathf.Cos(az) * Mathf.Cos(el)
                ) * radius;

                panel.localPosition = pos;
                if (faceCentre)
                    panel.localRotation = Quaternion.LookRotation(pos);  // faces outward toward user

                index++;
            }
        }
    }
}


