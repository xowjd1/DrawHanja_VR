using UnityEngine;

public class StrokePoint : MonoBehaviour
{
    public int index;
    public bool isActive = false;
    public bool isHit = false;
    public StrokeSequence sequence;

    private static int brushLayer = -1;

    private void Awake()
    {
        // 한 번만 레이어 캐싱
        if (brushLayer == -1)
            brushLayer = LayerMask.NameToLayer("Brush");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || isHit) return;

        if (other.gameObject.layer == brushLayer)
        {
            isHit = true;
            sequence.OnPointHit(this);  // 다음 포인트 활성화 시도
        }
    }
}
