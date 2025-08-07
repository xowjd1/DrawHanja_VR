using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class Boat : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Image panel;
    [SerializeField] private string nextSceneName;
    [SerializeField] private float fadeDuration = 2f;

    private bool hasStarted = false;

    private AudioSource audioSource;
    public AudioClip audioClip;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void BoatUI()
    {
        if (hasStarted) return;
        hasStarted = true;

        audioSource.PlayOneShot(audioClip);
        
        image.gameObject.SetActive(true);
        panel.gameObject.SetActive(true);

        StartCoroutine(FadeOutAndLoadScene());
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        float time = 0f;
        Color c = panel.color;

        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            panel.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, t));
            time += Time.deltaTime;
            yield return null;
        }

        panel.color = new Color(c.r, c.g, c.b, 0f);

        SceneManager.LoadScene(nextSceneName);
    }
}
    
   
