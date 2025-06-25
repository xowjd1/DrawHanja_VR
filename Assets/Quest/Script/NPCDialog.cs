using UnityEngine;

public class NPCDialog : MonoBehaviour, IInteractable
{
    public string[] dialogBeforeQuestComplete;
    public string[] dialogAfterQuestComplete;
    
    public bool triggerNextQuest = true;
    public int questIndex = 0;

    public void Interact()
    {
        if (QuestManager.Instance.questCompleted)
        {
            DialogUI.Instance.StartDialog(dialogAfterQuestComplete);

            if (QuestManager.Instance.questPanel == true)
            {
               QuestManager.Instance.HideQuestMessage(); 
            }
        }
        else
        {
            DialogUI.Instance.StartDialog(dialogBeforeQuestComplete, () =>
            {
                if (triggerNextQuest)
                {
                    QuestManager.Instance.StartNextQuest();
                }
            });
        }
    }
}
