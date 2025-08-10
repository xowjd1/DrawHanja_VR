using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GraphicRaycaster))]
public class UIDrawing : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public RawImage drawingImage; // DrawingArea
    public Button clearButton;    // 전체 지우기 버튼
    public int textureWidth = 512;
    public int textureHeight = 512;
    public int brushSize = 4;

    [Header("VR Input (BLS XR Origin)")]
    public InputActionReference triggerAction;  // BLS Input Action Asset의 Trigger
    public Transform controllerTransform;       // Right Controller(또는 Ray Origin)
    public LayerMask drawingLayerMask;

    [Header("Smoothing")]
    [Range(0.01f, 1f)]
    public float uvSmoothing = 0.2f;

    private Texture2D drawTex;
    private bool isDrawing;
    private Vector2 prevUV;
    private Vector2 prevSmoothUV = new Vector2(-1f, -1f);
    private readonly List<List<Vector2>> strokes = new();

    private BoxCollider targetCollider;
    private bool _initialized; // ✅ 초기화 완료 여부

    void Awake()
    {
        TryEnsureCollider();
        // 초기화가 끝났다고 표시
        _initialized = targetCollider != null && drawingImage != null;
    }

    void OnEnable()
    {
        // 도중에 꺼져있다 켜질 때도 복구
        TryEnsureCollider();
        if (triggerAction != null) triggerAction.action.Enable();
    }

    void OnDisable()
    {
        if (triggerAction != null) triggerAction.action.Disable();
    }

    // ✅ 여기서 바로 UpdateCollider를 부르면 때때로 Awake 전에 호출돼 NRE가 발생.
    //    가드 + 지연 복구만 수행.
    void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;
        if (drawingImage == null) return;

        TryEnsureCollider(); // 필요 시 다시 붙임
        if (targetCollider == null) return;

        // Rect가 바뀌었을 때만 안전하게 업데이트
        SafeUpdateCollider();
    }

    void TryEnsureCollider()
    {
        if (drawingImage == null)
        {
            // 인스펙터에 할당 안 됨
            return;
        }

        // RawImage 자신에게 콜라이더 확보/부착
        if (targetCollider == null)
            targetCollider = drawingImage.GetComponent<BoxCollider>();

        if (targetCollider == null)
            targetCollider = drawingImage.gameObject.AddComponent<BoxCollider>();

        // 확보됐다면 즉시 한 번 사이즈 맞추기
        if (targetCollider != null)
            SafeUpdateCollider();
    }

    void SafeUpdateCollider()
    {
        if (drawingImage == null || targetCollider == null) return;

        var rt = drawingImage.rectTransform;
        float w = rt.rect.width;
        float h = rt.rect.height;

        // size는 rect 기준, center는 피벗 보정
        targetCollider.size = new Vector3(w, h, 0.01f);
        targetCollider.center = new Vector3(
            (0.5f - rt.pivot.x) * w,
            (0.5f - rt.pivot.y) * h,
            0f
        );
    }

    void Start()
    {
        // 투명 텍스처 생성
        drawTex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        var cols = new Color[textureWidth * textureHeight];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;
        drawTex.SetPixels(cols);
        drawTex.Apply();

        drawingImage.texture = drawTex;
        drawingImage.color = Color.white;

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearAllStrokes);

        // 혹시 모를 초기화 누락 대비
        TryEnsureCollider();
        _initialized = targetCollider != null && drawingImage != null;
    }

    void Update()
    {
        if (!_initialized) return; // ✅ 초기화 이전 호출 가드
        if (triggerAction == null || controllerTransform == null) return;

        bool triggerPressed = triggerAction.action.ReadValue<float>() > 0.5f;

        if (triggerPressed && !isDrawing)
        {
            prevSmoothUV = new Vector2(-1f, -1f);
            isDrawing = true;
            strokes.Add(new List<Vector2>());
            prevUV = Vector2.zero;
        }

        if (isDrawing && triggerPressed)
            TryDrawWithRay();

        if (isDrawing && !triggerPressed)
        {
            isDrawing = false;
            prevUV = Vector2.zero;
        }
    }

    void TryDrawWithRay()
    {
        Vector3 pos = controllerTransform.position;
        Vector3 dir = controllerTransform.forward;

        if (!IsVectorValid(pos) || !IsVectorValid(dir))
        {
            Debug.LogWarning("[UIDrawing] ControllerTransform position/forward invalid.");
            return;
        }

        if (Physics.Raycast(pos, dir, out RaycastHit hit, Mathf.Infinity, drawingLayerMask))
        {
            if (hit.collider == targetCollider)
                DrawAtWorldHit(hit.point);
        }
    }

    void DrawAtWorldHit(Vector3 worldHitPoint)
    {
        var rt = drawingImage.rectTransform;
        Vector3 local3D = rt.InverseTransformPoint(worldHitPoint);

        float w = rt.rect.width;
        float h = rt.rect.height;

        float u = Mathf.Clamp01((local3D.x + rt.pivot.x * w) / w);
        float v = Mathf.Clamp01((local3D.y + rt.pivot.y * h) / h);
        Vector2 rawUV = new Vector2(u, v);

        Vector2 smoothUV = (prevSmoothUV.x < 0f)
            ? rawUV
            : Vector2.Lerp(prevSmoothUV, rawUV, uvSmoothing);

        prevSmoothUV = smoothUV;
        DrawAtUV(smoothUV);
    }

    void DrawAtUV(Vector2 uv)
    {
        strokes[^1].Add(uv);

        int x = Mathf.RoundToInt(uv.x * (textureWidth - 1));
        int y = Mathf.RoundToInt(uv.y * (textureHeight - 1));

        if (prevUV != Vector2.zero)
        {
            int x0 = Mathf.RoundToInt(prevUV.x * (textureWidth - 1));
            int y0 = Mathf.RoundToInt(prevUV.y * (textureHeight - 1));
            BresenhamLine(x0, y0, x, y);
        }
        else
        {
            DrawBrush(x, y);
        }

        prevUV = uv;
        drawTex.Apply(false);
    }

    void BresenhamLine(int x0, int y0, int x1, int y1)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            DrawBrush(x0, y0);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    void DrawBrush(int cx, int cy)
    {
        int r = brushSize;
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
                if (dx * dx + dy * dy <= r * r)
                    SetPixelSafe(cx + dx, cy + dy, Color.black);
    }

    void SetPixelSafe(int x, int y, Color c)
    {
        if ((uint)x >= (uint)textureWidth || (uint)y >= (uint)textureHeight) return;
        drawTex.SetPixel(x, y, c);
    }

    public void ClearAllStrokes()
    {
        var cols = new Color[textureWidth * textureHeight];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;
        drawTex.SetPixels(cols);
        drawTex.Apply(false);
        strokes.Clear();
        Debug.Log("All strokes cleared.");
    }

    public List<List<Vector2>> GetStrokes() => strokes;
    public Texture2D GetStrokeTexture() => drawTex;

    bool IsVectorValid(Vector3 v)
    {
        return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                 float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
    }
}
