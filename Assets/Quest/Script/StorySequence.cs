using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StorySequence : MonoBehaviour
{
    public Image imageDisplay;
    public TMP_Text storyText;
    public Button nextButton;

    public Sprite[] images;
    public string[] texts;

    private int currentIndex = 0;


    public GameObject storyCube;
    public Image fadePanel;
    public float fadeDuration = 2f;


    void Start()
    {
        ShowPage(0);

        nextButton.onClick.AddListener(NextPage);
    }

    void ShowPage(int index)
    {
        if (index < images.Length)
            imageDisplay.sprite = images[index];

        if (index < texts.Length)
            storyText.text = texts[index];
    }

    void NextPage()
    {
        currentIndex++;

        if (currentIndex < images.Length || currentIndex < texts.Length)
        {
            ShowPage(currentIndex);
        }
        else
        {
            nextButton.gameObject.SetActive(false);
            StartCoroutine(FadeOutAndCleanup());
        }
    }

    IEnumerator FadeOutAndCleanup()
    {

        Destroy(storyCube);

        if (fadePanel != null)
        {
            float time = 0f;
            Color c = fadePanel.color;

            while (time < fadeDuration)
            {
                float t = time / fadeDuration;
                fadePanel.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, t));
                time += Time.deltaTime;
                yield return null;
            }

            fadePanel.color = new Color(c.r, c.g, c.b, 0f);
        }

        gameObject.SetActive(false);
    }
}