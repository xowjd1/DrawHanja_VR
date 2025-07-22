using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class HanjaCardData
{
    public string id;
    public Sprite cardSprite;
    public Sprite detailSprite;  
}

public class HanjaCardSpawner : MonoBehaviour
{
    public GameObject hanjaCardPrefab;
    public List<HanjaCardData> hanjaCardList;
    
    public GameObject detailPanel;
    public Image detailImage;
    public Button xButton;

    void Awake()
    {
        xButton.onClick.AddListener(CloseDetail);
    }
    
    void Start()
    {
        foreach (var data in hanjaCardList)
        {
            GameObject card = Instantiate(hanjaCardPrefab, transform);
            card.GetComponent<Image>().sprite = data.cardSprite;
            
            Button btn = card.GetComponent<Button>();
            
            if (data.detailSprite != null)
            {
              btn.onClick.AddListener(() => ShowDetail(data));  
            }
        }
    }
    
    void ShowDetail(HanjaCardData data)
    {
        detailPanel.SetActive(true);
        detailImage.sprite = data.detailSprite;
    }
    
    void CloseDetail()
    {
        detailPanel.SetActive(false);
    }
}