using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BookOpen : MonoBehaviour
{
    private Animator animator;
    
    public GameObject effectRoot;
    public float delay = 3f;
    
    private bool hasStarted = false;
    private Coroutine particleRoutine;
    
    public Image fadeImage;          
    public float fadeDuration = 2f; 
    public string nextSceneName;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        effectRoot.SetActive(false);
        
        var c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;
    }

    private void Update()
    {
        bool isMoving = animator.GetBool("isMove");

        if (isMoving && !hasStarted)
        {
            Debug.Log(gameObject.name + " is moving");
            hasStarted = true;
            
            particleRoutine = StartCoroutine(StartParticlesAfterDelay());
        }
    }
    
    private System.Collections.IEnumerator StartParticlesAfterDelay()
    {
        yield return new WaitForSeconds(delay);
        effectRoot.SetActive(true);
        
        yield return new WaitForSeconds(2f);
        
        float time = 0f;
        Color color = fadeImage.color;

        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            fadeImage.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0f, 1f, t));
            time += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, 1f);
        
        SceneManager.LoadScene(nextSceneName);
    }
}
