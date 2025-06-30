using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class StrokeEditorTool : MonoBehaviour
{
    [Header("Editor Settings")]
    public List<Transform> strokePoints = new List<Transform>();
    public GameObject pointPrefab;   // 프리팹: SphereCollider + StrokePoint

    [Header("Path Settings")]
    [SerializeField] private float pathThickness = 0.05f;
    [SerializeField, Tooltip("패스 길이에 비례한 여유 비율 (예: 0.1 = 10%)")]
    private float pathLengthPaddingRatio = 0.1f;

    public void GenerateStroke()
    {
        ClearChildren();

        // 포인트 생성
        for (int i = 0; i < strokePoints.Count; i++)
        {
            Transform p = strokePoints[i];
            GameObject point = Instantiate(pointPrefab, p.position, Quaternion.identity, transform);
            point.name = $"Point_{i}";

            var pointComp = point.GetComponent<StrokePoint>();
            if (pointComp != null)
            {
                pointComp.index = i;
                pointComp.isActive = (i == 0);
                pointComp.isHit = false;
            }
        }

        // 패스 생성
        for (int i = 0; i < strokePoints.Count - 1; i++)
        {
            Vector3 from = strokePoints[i].position;
            Vector3 to = strokePoints[i + 1].position;
            Vector3 dir = to - from;
            float baseLength = dir.magnitude;
            float paddedLength = baseLength * (1f + pathLengthPaddingRatio);
            Vector3 mid = (from + to) * 0.5f;

            GameObject path = new GameObject($"Path_{i}_{i + 1}");
            path.transform.SetParent(transform);
            path.transform.position = mid;
            path.transform.rotation = Quaternion.LookRotation(dir);

            CapsuleCollider col = path.AddComponent<CapsuleCollider>();
            col.height = paddedLength;
            col.direction = 2; // Z axis
            col.center = Vector3.zero;
            col.radius = pathThickness;
            col.isTrigger = true;

            StrokePath pathScript = path.AddComponent<StrokePath>();
            pathScript.isHit = false;

            var fromPoint = transform.Find($"Point_{i}")?.GetComponent<StrokePoint>();
            var toPoint = transform.Find($"Point_{i + 1}")?.GetComponent<StrokePoint>();
            if (fromPoint != null && toPoint != null)
            {
                pathScript.from = fromPoint;
                pathScript.to = toPoint;
            }
        }
    }

    public void ClearChildren()
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < strokePoints.Count; i++)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(strokePoints[i].position, 0.05f);

            if (i < strokePoints.Count - 1)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(strokePoints[i].position, strokePoints[i + 1].position);
            }
        }
    }
}
