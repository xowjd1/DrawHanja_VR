using UnityEngine;
using TMPro;

[System.Serializable]
public class Quest
{
    public string message;
    public bool isCompleted;
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public Quest[] quests;
    public int currentQuestIndex = 0;
    
    public GameObject questPanel;
    public TextMeshProUGUI questText;
    
    public GameObject wallBlocker1;
    

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        questPanel.SetActive(false);
    }

    public void StartNextQuest()
    {
        if (currentQuestIndex < quests.Length)
        {
            ShowQuestMessage(quests[currentQuestIndex].message);
        }
    }

    public bool IsQuestActive(int questNumber)
    {
        return currentQuestIndex == questNumber && !quests[questNumber].isCompleted;
    }
    
    void ShowQuestMessage(string message)
    {
        questPanel.SetActive(true);
        questText.text = message;
    }

    public void HideQuestMessage()
    {
        questPanel.SetActive(false);
    }
    
    public void CompleteQuest()
    {
        if (currentQuestIndex < quests.Length)
        {
            quests[currentQuestIndex].isCompleted = true;

            // 퀘스트별 후처리
            if (currentQuestIndex == 0) wallBlocker1.SetActive(false);
            // if (currentQuestIndex == 1) wallBlocker2.SetActive(false);

            currentQuestIndex++;
            questPanel.SetActive(false);
        }
    }
}
