using UnityEngine;

public class CollectibleItem : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        if (!QuestManager.Instance.IsQuestActive(1)) return;

        Destroy(gameObject);
        QuestManager.Instance.CheckItemQuest();
    }
}
