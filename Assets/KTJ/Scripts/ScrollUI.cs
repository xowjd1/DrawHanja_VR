using System.Collections;
using UnityEngine;

public class ScrollUI : MonoBehaviour
{
    [Header("Mask Transform (Pivot X=1, Anchor X=1)")]
    public RectTransform maskRect;   

    [Header("언롤될 최대 너비")]
    public float targetWidth = 1000f;    

    [Header("애니메이션 시간")]
    public float duration = 1.5f;       

    void Awake()
    {
        // (선택) 코드로 Pivot/Anchor 설정하고 싶다면:
        maskRect.pivot     = new Vector2(1f, 0.5f);
        maskRect.anchorMin = new Vector2(1f, 0.5f);
        maskRect.anchorMax = new Vector2(1f, 0.5f);
    }

    void Start()
    {
        // 시작 시 너비를 0으로 초기화 (높이는 그대로 유지)
        var sz = maskRect.sizeDelta;
        maskRect.sizeDelta = new Vector2(0f, sz.y);

        StartCoroutine(Unroll());
    }

    IEnumerator Unroll()
    {
        float elapsed = 0f;
        Vector2 startSize = maskRect.sizeDelta;
        Vector2 endSize   = new Vector2(targetWidth, startSize.y);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            maskRect.sizeDelta = Vector2.Lerp(startSize, endSize, t);
            yield return null;
        }

        maskRect.sizeDelta = endSize;
    }
}
