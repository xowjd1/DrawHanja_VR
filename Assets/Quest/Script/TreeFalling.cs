using UnityEngine;

public class TreeFalling : MonoBehaviour
{
    public Rigidbody rb;
    public float tiltAngle = 90f;
    public float tiltSpeed = 90f;

    private float rotated = 0f;
    [HideInInspector] public bool isFalling = false;
    
    public AudioClip hitSound;
    public AudioClip fallSound;
    private AudioSource audioSource;

    void Start()
    {
        rb.isKinematic = true;
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!isFalling) return;

        float step = tiltSpeed * Time.deltaTime;
        float remaining = tiltAngle - rotated;
        float actualStep = Mathf.Min(step, remaining);

        transform.Rotate(Vector3.right, actualStep);
        rotated += actualStep;

        if (rotated >= tiltAngle && rb != null)
        {
            isFalling = false;
            audioSource.PlayOneShot(fallSound);
            QuestManager.Instance.CompleteQuest();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isFalling && collision.gameObject.CompareTag("Ax"))
        {
            isFalling = true;
            audioSource.PlayOneShot(hitSound);
        }
    }
}
