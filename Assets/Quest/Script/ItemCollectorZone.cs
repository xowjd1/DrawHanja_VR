using UnityEngine;
using System.Collections.Generic;

public class ItemCollectorZone : MonoBehaviour
{
    private int count = 3;
    private HashSet<GameObject> itemsInZone = new HashSet<GameObject>();
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Stick"))
        {
            itemsInZone.Add(other.gameObject);
            CheckQuestCompletion();
            Debug.Log("wwwwww");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Stick"))
        {
            itemsInZone.Remove(other.gameObject);
        }
    }
    
    private void CheckQuestCompletion()
    {
        if (!QuestManager.Instance.IsQuestActive(0)) return;

        if (itemsInZone.Count >= count)
        {
            Debug.Log("Completed Quest1");
            QuestManager.Instance.CompleteQuest();
        }
    }
}
