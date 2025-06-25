using UnityEngine;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;
    public int itemCount = 0;

    public int currentQuestIndex = 0;
    public bool questCompleted = false;
    
    public GameObject questPanel;
    public TextMeshProUGUI questText;
    public string[] questMessages;
    

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        questPanel.SetActive(false);
    }

    public void StartNextQuest()
    {
        if (currentQuestIndex < questMessages.Length)
        {
            string message = questMessages[currentQuestIndex];
            ShowQuestMessage(message);
            currentQuestIndex++;
            questCompleted = false;
        }
    }

    public bool IsQuestActive(int questNumber)
    {
        return currentQuestIndex == questNumber;
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
    
    public void CheckItemQuest()
    {
        itemCount++;
        
        if (itemCount >= 3 && !questCompleted)
        {
            questCompleted = true;
        }
    }
}
