using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class HanjaCardData
{
    public string id;
    public Sprite cardSprite;
}

public class HanjaCardSpawner : MonoBehaviour
{
    public GameObject hanjaCardPrefab;
    public List<HanjaCardData> hanjaCardList;

    void Start()
    {
        foreach (var data in hanjaCardList)
        {
            GameObject card = Instantiate(hanjaCardPrefab, transform);
            card.GetComponent<Image>().sprite = data.cardSprite;
        }
    }
}