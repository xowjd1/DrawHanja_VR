using UnityEngine;

public class CollectibleItem : MonoBehaviour, IInteractable
{
    private static int count = 0;
    public void Interact()
    {
        if (!QuestManager.Instance.IsQuestActive(0)) return;

        Destroy(gameObject);
        count++;
        Debug.Log("CollectibleItem: " + count);
        
        if (count >= 3)
        {
            QuestManager.Instance.CompleteQuest();
        }
    }
}
