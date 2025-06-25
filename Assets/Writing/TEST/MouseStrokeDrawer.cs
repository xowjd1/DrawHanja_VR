using System.Collections.Generic;
using UnityEngine;

public class MouseStrokeDrawer : MonoBehaviour
{
    [Header("Line Prefab (LineRenderer만)")]
    public GameObject linePrefab;

    [Header("카메라 참조 (Orthographic)")]
    public Camera   renderCamera;

    [Header("스무딩 파라미터")]
    [Range(0f, 1f)] public float smoothingFactor = 0.5f; 
    public float minDistance     = 0.01f;   
    
    [Header("캔버스")]
    public Transform canvasTransform; 
    // 내부 상태
    private LineRenderer       currentLine;
    private List<Vector2>      currentStroke;
    private List<List<Vector2>> strokes = new List<List<Vector2>>();

    void Update()
    {
        // 마우스 왼쪽 버튼 누름 → 스트로크 시작
        if (Input.GetMouseButtonDown(0))
        {
            StartStroke();
        }

        // 누른 상태로 드래그 중 → 포인트 추가
        if (Input.GetMouseButton(0) && currentLine != null)
        {
            AddPoint();
        }

        // 버튼 뗄 때 → 스트로크 종료
        if (Input.GetMouseButtonUp(0) && currentLine != null)
        {
            EndStroke();
        }
    }

    void StartStroke()
    {
        var go = Instantiate(linePrefab);
        currentLine    = go.GetComponent<LineRenderer>();
        currentLine.positionCount = 0;

        currentStroke = new List<Vector2>();
        strokes.Add(currentStroke);

        // (선택) 코너·캡을 둥글게
        currentLine.numCornerVertices = 5;
        currentLine.numCapVertices    = 5;
    }

    void AddPoint()
    {
        // 1) Raycast로 캔버스 충돌 지점 찾기
        Ray ray = renderCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit) || hit.transform != canvasTransform)
            return;   // 캔버스가 아니면 그리지 않음

        // 2) 충돌 지점을 worldPos 로 사용
        Vector3 worldPos = hit.point;

        // 3) 마지막 그려진 점과 보간 (스무딩)
        Vector3 prevPos = (currentLine.positionCount > 0)
            ? currentLine.GetPosition(currentLine.positionCount - 1)
            : worldPos;
        Vector3 newPos = Vector3.Lerp(prevPos, worldPos, 1 - smoothingFactor);

        // 4) 샘플링 거리 체크
        Vector2 p2d = new Vector2(newPos.x, newPos.y);
        if (currentStroke.Count > 0 &&
            Vector2.Distance(currentStroke[^1], p2d) < minDistance)
            return;

        // 5) 라인에 추가
        int idx = currentLine.positionCount;
        currentLine.positionCount = idx + 1;
        currentLine.SetPosition(idx, newPos);

        // 6) 벡터 저장
        currentStroke.Add(p2d);
    }


    void EndStroke()
    {
        // 획이 끝나기 직전에 벡터값들 출력
        if (currentStroke != null)
        {
            Debug.Log($"── 스트로크 #{strokes.Count} (점 {currentStroke.Count}개) ──");
            for (int i = 0; i < currentStroke.Count; i++)
            {
                Vector2 p = currentStroke[i];
                Debug.Log($"Point {i}: {p.x:F3}, {p.y:F3}");
            }
        }

        currentLine  = null;
        currentStroke = null;
    }


    /// <summary>
    /// 외부에서 호출해 캡처된 모든 스트로크 벡터 데이터를 얻을 수 있습니다.
    /// </summary>
    public List<List<Vector2>> GetStrokes()
    {
        return strokes;
    }
}
