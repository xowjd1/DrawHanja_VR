using UnityEngine;

public class TreeFalling : MonoBehaviour
{
    public Rigidbody rb;
    public float tiltAngle = 90f;
    public float tiltSpeed = 90f;

    private float rotated = 0f;
    private bool isFalling = false;

    void Start()
    {
        rb.isKinematic = true;
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
            QuestManager.Instance.CompleteQuest();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isFalling && collision.gameObject.CompareTag("Ax"))
        {
            isFalling = true;
        }
    }
}
