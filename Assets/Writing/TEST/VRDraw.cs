using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VRDraw : MonoBehaviour
{
   [Header("컨트롤러 설정")]
    public XRNode    xrNode = XRNode.RightHand;      // 오른손 트리거 사용
    public Transform controllerTransform;            // Ray 발사 기준 위치

    [Header("캔버스 설정")]
    public Transform canvasTransform;                // Quad 등의 그리기 영역
    public Vector2   canvasSize = new(1,1);

    [Header("렌더링 & 스무딩")]
    public GameObject linePrefab;                    // LineRenderer 전용 프리팹
    [Range(0f,1f)] public float smoothingFactor = 0.8f;
    public float      minDistance     = 0.02f;
    
    [Header("노이즈 필터")]
    [Tooltip("최근 프레임 위치 몇 개로 평균낼지")]
    public int         filterWindow    = 5;
    // 내부 상태
    InputDevice            device;
    bool                   isDrawing     = false;
    LineRenderer           currentLine;
    List<Vector2>          currentStroke;
    List<List<Vector2>>    strokes       = new();

    Queue<Vector3>         posQueue   = new();
    
    void Start()
    {
        device = InputDevices.GetDeviceAtXRNode(xrNode);
    }

    void Update()
    {
        if (!device.isValid)
            device = InputDevices.GetDeviceAtXRNode(xrNode);

        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger) && trigger)
        {
            if (!isDrawing) StartStroke();
            AddPoint();
        }
        else if (isDrawing)
        {
            EndStroke();
        }
    }

    void StartStroke()
    {
        // 새 LineRenderer 생성
        var go = Instantiate(linePrefab);
        currentLine = go.GetComponent<LineRenderer>();
        currentLine.positionCount = 0;
        currentLine.numCornerVertices = 5;
        currentLine.numCapVertices    = 5;

        currentStroke = new List<Vector2>();
        strokes.Add(currentStroke);

        posQueue.Clear();
        isDrawing = true;
    }

    void AddPoint()
    {
        // 1) Raycast로 캔버스 hit
        Ray ray = new(controllerTransform.position, controllerTransform.forward);
        if (!Physics.Raycast(ray, out var hit) || hit.transform != canvasTransform)
            return;

        // 2) hit.point 을 큐에 넣고 평균내기
        posQueue.Enqueue(hit.point);
        if (posQueue.Count > filterWindow) posQueue.Dequeue();
        Vector3 avgPos = Vector3.zero;
        foreach (var p in posQueue) avgPos += p;
        avgPos /= posQueue.Count;

        // 3) 부드럽게 보간
        Vector3 prev = currentLine.positionCount > 0
            ? currentLine.GetPosition(currentLine.positionCount - 1)
            : avgPos;
        Vector3 smoothed = Vector3.Lerp(prev, avgPos, 1 - smoothingFactor);

        // 4) 충분히 떨어졌는지 체크
        Vector2 p2d = new(smoothed.x, smoothed.y);
        if (currentStroke.Count > 0 &&
            Vector2.Distance(currentStroke[^1], p2d) < minDistance)
            return;

        // 5) LineRenderer에 한 점 추가
        int idx = currentLine.positionCount;
        currentLine.positionCount = idx + 1;
        currentLine.SetPosition(idx, smoothed);

        // 6) 매칭용 벡터에도 저장
        currentStroke.Add(p2d);
    }

    void EndStroke()
    {
        // 디버그: 한 획의 벡터 찍어보기
        Debug.Log($"── Stroke #{strokes.Count} ({currentStroke.Count} points) ──");
        for (int i = 0; i < currentStroke.Count; i++)
        {
            var v = currentStroke[i];
            Debug.Log($"{i}: {v.x:F3}, {v.y:F3}");
        }

        currentLine    = null;
        currentStroke  = null;
        isDrawing      = false;
    }

    /// <summary>나중에 궤적 데이터가 필요할 때 호출하세요.</summary>
    public List<List<Vector2>> GetStrokes() => strokes;
}
