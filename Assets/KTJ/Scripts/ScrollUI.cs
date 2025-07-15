using System.Collections;
using UnityEngine;

public class ScrollUI : MonoBehaviour
{
    public RectTransform scrollPage;  // scroll page 객체 연결
    public float startX = 1505f;
    public float endX = 0f;
    public float duration = 1.5f;     // 이동 시간 (초)

    public GameObject hanjaImage;
    public GameObject drawPanel;
    public GameObject finishBtn;
    public GameObject clearBtn;

    private void Start()
    {
        // 시작 전엔 UI 요소 숨기기
        hanjaImage.SetActive(false);
        drawPanel.SetActive(false);
        finishBtn.SetActive(false);
        clearBtn.SetActive(false);

        scrollPage.anchoredPosition = new Vector2(startX, scrollPage.anchoredPosition.y);
        StartCoroutine(MoveScrollPage());
    }

    private IEnumerator MoveScrollPage()
    {
        float elapsed = 0f;
        Vector2 startPos = scrollPage.anchoredPosition;
        Vector2 targetPos = new Vector2(endX, startPos.y);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            scrollPage.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        scrollPage.anchoredPosition = targetPos;

        // 다 펼쳐지고 나서 UI 요소들 보여주기
        hanjaImage.SetActive(true);
        drawPanel.SetActive(true);
        finishBtn.SetActive(true);
        clearBtn.SetActive(true);
    }
}