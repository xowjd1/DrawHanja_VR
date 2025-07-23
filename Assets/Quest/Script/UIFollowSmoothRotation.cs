using UnityEngine;

public class UIFollowSmoothRotation : MonoBehaviour
{
    public Transform cameraTransform; 
    public float rotationThreshold = 30f; 
    public float followSpeed = 5f; 

    private Quaternion targetRotation;

    void Start()
    {
        targetRotation = transform.rotation;
    }

    void LateUpdate()
    {
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0; // 수평 방향

        Vector3 uiForward = transform.forward;
        uiForward.y = 0;

        float angle = Vector3.Angle(cameraForward, uiForward);
        
        if (angle > rotationThreshold)
        {
            targetRotation = Quaternion.LookRotation(cameraForward);
        }
        
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
    }
}
