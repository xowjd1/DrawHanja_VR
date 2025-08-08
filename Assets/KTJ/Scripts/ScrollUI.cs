using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScrollUI : MonoBehaviour
{
    public RectTransform scrollPage;
    public float startX = 1505f;
    public float endX = 0f;
    public float duration = 1.5f;

    public GameObject hanjaImage;
    public HanjaDataBase hanjaDatabase;
    public int currentHanjaIndex = 0;
    public GameObject drawPanel;
    public GameObject finishBtn;
    public GameObject clearBtn;
    public Button pronBtn;
    public Button exitBtn;
    private AudioSource audioSource;
    private void Start()
    {
        hanjaImage.SetActive(false);
        drawPanel.SetActive(false);
        finishBtn.SetActive(false);
        clearBtn.SetActive(false);
        pronBtn.gameObject.SetActive(false);
        exitBtn.gameObject.SetActive(false);

        scrollPage.anchoredPosition = new Vector2(startX, scrollPage.anchoredPosition.y);
        StartCoroutine(MoveScrollPage());
        
        audioSource = gameObject.AddComponent<AudioSource>();
        
        pronBtn.onClick.AddListener(PlayPronunciation);
        exitBtn.onClick.AddListener(ExitScroll);
        
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

        hanjaImage.SetActive(true);
        drawPanel.SetActive(true);
        finishBtn.SetActive(true);
        clearBtn.SetActive(true);
        pronBtn.gameObject.SetActive(true);
        exitBtn.gameObject.SetActive(true);
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

    private void ExitScroll()
    {
        gameObject.SetActive(false);
    }
}