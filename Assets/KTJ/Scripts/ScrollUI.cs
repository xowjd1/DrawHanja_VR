using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(AudioSource))]
public class ScrollUI : MonoBehaviour
{
    public Transform head;
    public float appearDistance = 1.1f;
    public Vector3 appearOffset = new(0f, -0.1f, 0f);

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
    public UIDrawing drawing;
    public VisionCompareController comparer;

    AudioSource audioSource;
    CanvasGroup cg;
    bool playing;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake  = false;

        cg = GetComponent<CanvasGroup>();

        if (pronBtn)
        {
            pronBtn.onClick.RemoveAllListeners();
            pronBtn.onClick.AddListener(PlayPronunciation);
        }
        if (exitBtn)
        {
            exitBtn.onClick.RemoveAllListeners();
            exitBtn.onClick.AddListener(Close);   // ⬅ 소프트 클로즈
        }
        if (clearBtn)
        {
            var btn = clearBtn.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    drawing?.ClearAllStrokes();
                    comparer?.ResetResultUI();
                    Debug.Log("[ScrollUI] Clear clicked");
                });
            }
            else Debug.LogWarning("[ScrollUI] clearBtn에 Button 컴포넌트가 없습니다.");
        }

        SetChildrenActive(false);
        HideInstant();                             // ⬅ 시작은 완전 숨김
    }

    void OnEnable()
    {
        // 누가 실수로 SetActive(true)만 해도 스스로 복구
        if (pronBtn && !pronBtn.gameObject.activeSelf)
        {
            StopAllCoroutines();
            SetChildrenActive(false);
            scrollPage.anchoredPosition = new Vector2(startX, scrollPage.anchoredPosition.y);
            SnapInFrontOfHead();
            cg.alpha = 1f; cg.interactable = false; cg.blocksRaycasts = false;
            StartCoroutine(MoveScrollPage());
            Debug.Log("[ScrollUI] OnEnable → self-reopen");
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        playing = false;
        SetChildrenActive(false);
    }

    void EnsureHead()
    {
        if (head) return;
        var cam = Camera.main;
        if (cam) head = cam.transform;
    }

    void SnapInFrontOfHead()
    {
        EnsureHead();
        if (!head) return;
        var fwd = head.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-4f) fwd = head.forward;
        transform.position = head.position + fwd.normalized * appearDistance + appearOffset;
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }

    public void Open(int index)
    {
        currentHanjaIndex = index;

        // ✅ 연습 스프라이트를 함께 넘겨서 '크기 되돌리기 + 교체'까지 한 번에
        Sprite practiceSprite = null;
        if (hanjaDatabase && index >= 0 && index < hanjaDatabase.allowedHanja.Length)
            practiceSprite = hanjaDatabase.allowedHanja[index]?.practiceImage;

        if (comparer) comparer.ResetResultUI(practiceSprite);

        drawing?.ClearAllStrokes();

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        SnapInFrontOfHead();
        StopAllCoroutines();
        playing = false;
        SetChildrenActive(false);
        scrollPage.anchoredPosition = new Vector2(startX, scrollPage.anchoredPosition.y);

        var cg = GetComponent<CanvasGroup>();
        if (cg) { cg.alpha = 1f; cg.interactable = false; cg.blocksRaycasts = false; }

        StartCoroutine(MoveScrollPage());
        Debug.Log($"[ScrollUI] Open index={index}");
    }


    public void Close()
    {
        Debug.Log("[ScrollUI] Close (soft)");
        comparer?.ResetResultUI();
        drawing?.ClearAllStrokes();

        StopAllCoroutines();
        playing = false;
        SetChildrenActive(false);
        HideInstant();                              // ⬅ 하드 비활성화 금지
    }

    void HideInstant()
    {
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        if (scrollPage)
            scrollPage.anchoredPosition = new Vector2(startX, scrollPage.anchoredPosition.y);
    }

    IEnumerator MoveScrollPage()
    {
        playing = true;
        float elapsed = 0f;
        Vector2 startPos = scrollPage.anchoredPosition;
        Vector2 targetPos = new(endX, startPos.y);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;     // ⬅ 일시정지 무시
            float t = Mathf.Clamp01(elapsed / duration);
            scrollPage.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        scrollPage.anchoredPosition = targetPos;
        SetChildrenActive(true);
        cg.interactable = true;                    // ⬅ 버튼 다시 활성
        cg.blocksRaycasts = true;
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
        if (!hanjaDatabase) { Debug.LogWarning("[ScrollUI] hanjaDatabase 미할당"); return; }
        if (currentHanjaIndex < 0 || currentHanjaIndex >= hanjaDatabase.allowedHanja.Length)
        { Debug.LogWarning($"[ScrollUI] 인덱스 범위 밖: {currentHanjaIndex}"); return; }

        var data = hanjaDatabase.allowedHanja[currentHanjaIndex];
        var clip = data ? data.pronunciationAudio : null;
        if (clip) audioSource.PlayOneShot(clip);
        else Debug.LogWarning($"[{(data ? data.character : "null")}] 발음 파일 없음");
    }
}
