using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HanjaUIController : MonoBehaviour
{
    public HanjaDataBase hanjaDataBase;
    public Image hanjaImageUI;
    public ScrollUI scrollUI;

    // DB 인덱스
    const int IDX_KATANA = 0;   // 刀
    const int IDX_DANGO  = 1;   // 力
    const int IDX_KIRU   = 2;
    const int IDX_MOK    = 3;   // 木
    const int IDX_SUI    = 4;   // 水
    const int IDX_HI     = 5;   // 火
    const int IDX_INU    = 6;   // 犬
    const int IDX_SARU   = 7;   // 猿
    const int IDX_TORI   = 8;   // 鳥

    public int currentHanjaIndex = 0;

    public GameObject katana, dango, fireVFX, crossTheRiver, tree;
    [SerializeField] private TreeFalling treeFalling;
    [SerializeField] private QuestManager questManager;

    bool openedDog, openedMonkey, openedBird;

    void Start()
    {
        ShowPracticeImage(currentHanjaIndex);
        if (scrollUI) scrollUI.gameObject.SetActive(false);
    }

    public void ShowPracticeImage(int index)
    {
        if (!hanjaDataBase || !hanjaImageUI) return;
        if (index < 0 || index >= hanjaDataBase.allowedHanja.Length) return;
        var hanjaData = hanjaDataBase.allowedHanja[index];
        if (hanjaData && hanjaData.practiceImage) hanjaImageUI.sprite = hanjaData.practiceImage;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == katana)               OpenPanelWithIndex(IDX_KATANA);
        else if (other.gameObject == dango)           OpenPanelWithIndex(IDX_DANGO);
        else if (other.gameObject == fireVFX)         OpenPanelWithIndex(IDX_HI);
        else if (other.gameObject == crossTheRiver)   OpenPanelWithIndex(IDX_SUI);
        else if (other.gameObject == tree)            OpenPanelWithIndex(IDX_MOK);
    }

    void Update()
    {
        if (!questManager) return;

        // ✅ 퀘스트 완료 시 1회만, 1.5초 뒤 오픈
        if (!openedDog && questManager.quests[0].isCompleted)
        {
            openedDog = true;
            StartCoroutine(OpenAfterDelay(IDX_INU, 1.5f));
        }
        if (!openedMonkey && questManager.quests[1].isCompleted)
        {
            openedMonkey = true;
            StartCoroutine(OpenAfterDelay(IDX_SARU, 1.5f));
        }
        if (!openedBird && questManager.quests[2].isCompleted)
        {
            openedBird = true;
            StartCoroutine(OpenAfterDelay(IDX_TORI, 1.5f));
        }
    }

    IEnumerator OpenAfterDelay(int index, float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds); // timeScale 0이어도 대기
        OpenPanelWithIndex(index);
    }

    public void OpenPanelWithIndex(int index)
    {
        ShowPracticeImage(index);
        if (scrollUI != null) scrollUI.Open(index);
        else Debug.LogError("[HanjaUI] scrollUI 미할당");
    }
}
