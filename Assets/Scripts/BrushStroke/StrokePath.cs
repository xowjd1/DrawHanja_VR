using UnityEngine;

public class StrokePath : MonoBehaviour
{
    public StrokePoint from;
    public StrokePoint to;
    public bool isHit = false;

    private static int brushLayer = -1;

    private void Awake()
    {
        if (brushLayer == -1)
            brushLayer = LayerMask.NameToLayer("Brush");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHit) return;

        if (other.gameObject.layer == brushLayer)
        {
            isHit = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isHit) return;

        if (other.gameObject.layer == brushLayer)
        {
            if (!to.isHit)
            {
                from.sequence.OnPathMissed(); // 전체 리셋
            }
        }
    }
}
