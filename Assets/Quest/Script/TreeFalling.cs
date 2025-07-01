using UnityEngine;

public class TreeFalling : MonoBehaviour
{
    public float fallSpeed = 20f;          
    public float fallThreshold = 60f;      
    public Rigidbody rb;                   
    public bool isHit = false;
    
    private float currentAngle = 0f;
    
    void Start()
    {
        rb.isKinematic = true;
    }
    
    void Update()
    {
        if (isHit && currentAngle < fallThreshold)
        {
            float delta = fallSpeed * Time.deltaTime;
            transform.Rotate(Vector3.right, delta);
            currentAngle += delta;

            if (currentAngle >= fallThreshold)
            {
                rb.isKinematic = false;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("무언가와 충돌: " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("Ax"))
        {
            isHit = true;
            Debug.Log("충돌 발생: " + collision.gameObject.name);
        }
    }
}
