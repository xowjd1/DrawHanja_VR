using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
public class VRDraw : MonoBehaviour
{
    [Header("캔버스 설정")]
    public Transform canvasTransform;    // 쓰기 평면(Quad)의 Transform
    public Vector2 canvasSize = new(0.2f, 0.2f); // Quad 실제 크기(m)

    [Header("렌더링")]
    public GameObject linePrefab;         // 순수 LineRenderer 프리팹

    [Header("트리거 입력")]
    public XRNode xrNode = XRNode.RightHand; // 트리거 입력을 읽어올 컨트롤러

    // 내부 상태
    private bool isColliding;   // 붓 끝이 캔버스에 닿아 있는가
    private bool  isDrawing;     // 현재 그리기 중인가
    private LineRenderer currentLine;
    private List<Vector2> currentStroke;
    private List<List<Vector2>> strokes = new();

    private InputDevice device;

    void Start()
    {
        device = InputDevices.GetDeviceAtXRNode(xrNode);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform == canvasTransform)
            isColliding = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform == canvasTransform)
            isColliding = false;
    }

    void Update()
    {
        // 디바이스가 유효하지 않으면 재획득
        if (!device.isValid)
            device = InputDevices.GetDeviceAtXRNode(xrNode);

        // 트리거 버튼 입력 읽기
        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed))
        {
            if (triggerPressed && isColliding)
            {
                if (!isDrawing)
                    StartStroke();
                else
                    AddPoint();
            }
            else if (isDrawing) // 트리거 뗐을 때
            {
                EndStroke();
            }
        }
    }

    void StartStroke()
    {
        var go = Instantiate(linePrefab);
        currentLine = go.GetComponent<LineRenderer>();
        currentLine.positionCount = 0;

        currentStroke = new List<Vector2>();
        strokes.Add(currentStroke);

        isDrawing = true;
    }

    void AddPoint()
    {
        // 월드 → 캔버스 로컬 → 0~1 정규화
        Vector3 worldPos = transform.position;
        Vector3 localPos = canvasTransform.InverseTransformPoint(worldPos);

        float xN = (localPos.x / canvasSize.x) + 0.5f;
        float yN = (localPos.y / canvasSize.y) + 0.5f;
        Vector2 uv = new(xN, yN);

        if (currentStroke.Count == 0 || Vector2.Distance(currentStroke[^1], uv) > 0.01f)
        {
            // LineRenderer에 점 추가
            int idx = currentLine.positionCount;
            currentLine.positionCount = idx + 1;
            currentLine.SetPosition(idx, worldPos);

            // 매칭용 벡터에도 저장
            currentStroke.Add(uv);
        }
    }

    void EndStroke()
    {
        isDrawing = false;
        currentLine = null;
        currentStroke = null;
    }

    // 외부에서 궤적 데이터가 필요할 때 호출
    public List<List<Vector2>> GetStrokes() => strokes;
}
