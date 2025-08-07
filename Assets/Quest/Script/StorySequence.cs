using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StorySequence : MonoBehaviour
{
    public Image imageDisplay;
    public TMP_Text storyText;

    public Sprite[] images;
    public string[] texts;

    private int currentIndex = 0;


    public GameObject storyCube;
    public float pageDelay = 5f;


    void Start()
    {
        ShowPage(0);
        StartCoroutine(AutoAdvance());
    }

    void ShowPage(int index)
    {
        if (index < images.Length)
            imageDisplay.sprite = images[index];

        if (index < texts.Length)
            storyText.text = texts[index];
    }

    IEnumerator AutoAdvance()
    {
        while (currentIndex < images.Length || currentIndex < texts.Length)
        {
            yield return new WaitForSeconds(pageDelay);
            currentIndex++;

            if (currentIndex < images.Length || currentIndex < texts.Length)
            {
                ShowPage(currentIndex);
            }
            else
            {
                gameObject.SetActive(false);
                Destroy(storyCube);
            }
        }
    }
}