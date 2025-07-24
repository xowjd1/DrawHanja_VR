using UnityEngine;

public class UIFollowSmoothRotation : MonoBehaviour
{
    public Transform cameraTransform;
    public float followDistance = 2f;
    public float rotationThreshold = 30f;
    public float moveThreshold = 0.03f;
    public float followSpeed = 5f;

    private Quaternion targetRotation;
    private Vector3 targetPosition;
    private Vector3 lastCameraPosition;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        lastCameraPosition = cameraTransform.position;
        UpdateTargetToCamera();
    }

    void LateUpdate()
    {
        // 수평 방향만 고려한 회전 벡터
        Vector3 flatCameraForward = cameraTransform.forward;
        flatCameraForward.y = 0;
        flatCameraForward.Normalize();

        Vector3 flatUIForward = transform.forward;
        flatUIForward.y = 0;
        flatUIForward.Normalize();

        float angle = Vector3.Angle(flatCameraForward, flatUIForward);
        float moveDistance = Vector3.Distance(cameraTransform.position, lastCameraPosition);

        bool isSmallRotation = angle <= rotationThreshold;
        bool isMoving = moveDistance > moveThreshold;

        // 이동했거나 회전이 컸으면 UI 목표 위치/회전 갱신
        if (!isSmallRotation || isMoving)
        {
            UpdateTargetToCamera();
            lastCameraPosition = cameraTransform.position;
        }

        // 위치 및 회전 스무딩 적용
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
    }

    void UpdateTargetToCamera()
    {
        Vector3 forward = cameraTransform.forward;
        forward.y = 0;
        forward.Normalize();

        targetRotation = Quaternion.LookRotation(forward);
        targetPosition = cameraTransform.position + forward * followDistance;
    }
}
