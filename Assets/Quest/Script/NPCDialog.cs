using UnityEngine;

public class NPCDialog : MonoBehaviour, IInteractable
{
    public string[] dialogLines;
    public bool triggerNextQuest = true;

    public void Interact()
    {
        if (triggerNextQuest)
        {
            DialogUI.Instance.StartDialog(dialogLines, () =>
            {
                QuestManager.Instance.StartNextQuest(); 
            });
        }
        else
        {
            DialogUI.Instance.StartDialog(dialogLines);
        }
    }
}
