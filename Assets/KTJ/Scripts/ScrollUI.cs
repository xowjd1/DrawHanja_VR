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
    bool playing;
    public UIDrawing drawing;   
    public VisionCompareController comparer;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        pronBtn.onClick.RemoveAllListeners();
        pronBtn.onClick.AddListener(PlayPronunciation);
        exitBtn.onClick.RemoveAllListeners();
        exitBtn.onClick.AddListener(Close);

        SetChildrenActive(false);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        playing = false;
        SetChildrenActive(false);
    }

    // ✅ 외부에서 이 메서드만 호출
    public void Open(int index)
    {
        currentHanjaIndex = index;
        if (comparer) comparer.ResetResultUI();
        if (drawing) drawing.ClearAllStrokes();  

        // 1) 활성화(비활성 상태면 OnEnable 호출됨)
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        // 2) ‘이미 활성 상태’에서도 재오픈 가능하도록 리셋 후 코루틴 재시작
        StopAllCoroutines();
        playing = false;
        SetChildrenActive(false);
        scrollPage.anchoredPosition = new Vector2(startX, scrollPage.anchoredPosition.y);
        StartCoroutine(MoveScrollPage());
        // 필요 시 CanvasGroup도 초기화
        var cg = GetComponent<CanvasGroup>();
        if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }

        Debug.Log($"[ScrollUI] Open index={index}");
    }

    public void Close()
    {
        Debug.Log("[ScrollUI] Close");
        if (comparer) comparer.ResetResultUI();
        if (drawing) drawing.ClearAllStrokes();
        gameObject.SetActive(false); // OnDisable에서 정리됨
    }

    IEnumerator MoveScrollPage()
    {
        playing = true;
        float elapsed = 0f;
        Vector2 startPos = scrollPage.anchoredPosition;
        Vector2 targetPos = new(endX, startPos.y);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            scrollPage.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        scrollPage.anchoredPosition = targetPos;
        SetChildrenActive(true);
        playing = false;
    }

    void SetChildrenActive(bool v)
    {
        if (hanjaImage) hanjaImage.SetActive(v);
        if (drawPanel)  drawPanel.SetActive(v);
        if (finishBtn)  finishBtn.SetActive(v);
        if (clearBtn)   clearBtn.SetActive(v);
        if (pronBtn)    pronBtn.gameObject.SetActive(v);
        if (exitBtn)    exitBtn.gameObject.SetActive(v);
    }

    void PlayPronunciation()
    {
        if (!hanjaDatabase) return;
        var data = hanjaDatabase.allowedHanja[currentHanjaIndex];
        var clip = data ? data.pronunciationAudio : null;
        if (clip) audioSource.PlayOneShot(clip);
        else Debug.LogWarning($"[{(data? data.character : "null")}] 발음 파일 없음");
    }
}