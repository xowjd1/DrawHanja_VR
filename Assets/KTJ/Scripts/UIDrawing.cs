using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Input System
using UnityEngine.XR;

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
    public InputActionReference triggerAction;  // BLS XR Origin Input Action Asset에서 Trigger 연결
    public Transform controllerTransform;       // Right Controller 또는 Ray Origin Transform
    public LayerMask drawingLayerMask;

    [Header("Smoothing")]
    [Range(0.01f, 1f)]
    public float uvSmoothing = 0.2f;

    // 내부 상태
    private Texture2D drawTex;
    private bool isDrawing;
    private Vector2 prevUV;
    private Vector2 prevSmoothUV = new Vector2(-1f, -1f);
    private List<List<Vector2>> strokes = new();

    void Awake()
    {
        // BoxCollider 세팅
        var bc = GetComponent<BoxCollider>();
        if (bc == null) bc = gameObject.AddComponent<BoxCollider>();

        bc.isTrigger = false;
        var rt = drawingImage.rectTransform;
        bc.size = new Vector3(rt.rect.width, rt.rect.height, 0.01f);
        bc.center = Vector3.zero;
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

        // Input Action 활성화
        if (triggerAction != null)
            triggerAction.action.Enable();
    }

    void Update()
    {
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
            Debug.LogWarning("[UIDrawing] ControllerTransform position or direction is invalid (NaN/Infinity).");
            return;
        }

        Ray ray = new Ray(pos, dir);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, drawingLayerMask))
        {
            if (hit.collider.gameObject == drawingImage.gameObject)
                DrawAtWorldHit(hit.point);
        }
    }

    void DrawAtWorldHit(Vector3 worldHitPoint)
    {
        var rt = drawingImage.rectTransform;
        Vector3 local3D = rt.InverseTransformPoint(worldHitPoint);
        float u = Mathf.Clamp01(local3D.x / rt.rect.width + 0.5f);
        float v = Mathf.Clamp01(local3D.y / rt.rect.height + 0.5f);
        Vector2 rawUV = new Vector2(u, v);

        Vector2 smoothUV;
        if (prevSmoothUV.x < 0f)
            smoothUV = rawUV;
        else
            smoothUV = Vector2.Lerp(prevSmoothUV, rawUV, uvSmoothing);

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
        drawTex.Apply();
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
        if (x < 0 || x >= textureWidth || y < 0 || y >= textureHeight) return;
        drawTex.SetPixel(x, y, c);
    }

    public void ClearAllStrokes()
    {
        var cols = new Color[textureWidth * textureHeight];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;
        drawTex.SetPixels(cols);
        drawTex.Apply();
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
