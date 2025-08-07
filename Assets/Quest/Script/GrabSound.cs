using UnityEngine;

public class GrabSound : MonoBehaviour
{
    public AudioClip grabSound;
    private AudioSource audioSource;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    
    public void PlaySound()
    {
        if (grabSound != null)
        {
            audioSource.PlayOneShot(grabSound);
        }
    }
}
