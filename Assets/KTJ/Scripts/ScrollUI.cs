using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScrollUI : MonoBehaviour
{
    public RectTransform scrollPage;  // scroll page 객체 연결
    public float startX = 1505f;
    public float endX = 0f;
    public float duration = 1.5f;     // 이동 시간 (초)

    public GameObject hanjaImage;
    public HanjaDataBase hanjaDatabase;
    public int currentHanjaIndex = 0;
    public GameObject drawPanel;
    public GameObject finishBtn;
    public GameObject clearBtn;
    public Button  pronBtn;
    private AudioSource audioSource;
    private void Start()
    {
        // 시작 전엔 UI 요소 숨기기
        hanjaImage.SetActive(false);
        drawPanel.SetActive(false);
        finishBtn.SetActive(false);
        clearBtn.SetActive(false);
        pronBtn.gameObject.SetActive(false);

        scrollPage.anchoredPosition = new Vector2(startX, scrollPage.anchoredPosition.y);
        StartCoroutine(MoveScrollPage());
        
        audioSource = gameObject.AddComponent<AudioSource>();

        // ↓ 버튼 리스너 연결
        pronBtn.onClick.AddListener(PlayPronunciation);
        
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
        pronBtn.gameObject.SetActive(true);
    }
    
    private void PlayPronunciation()
    {
        if (hanjaDatabase == null) return;

        var data = hanjaDatabase.allowedHanja[currentHanjaIndex];
        var clip = data.pronunciationAudio;
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[{data.character}]에 할당된 발음 파일이 없습니다.");
        }
    }
}