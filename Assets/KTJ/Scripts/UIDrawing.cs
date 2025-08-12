using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GraphicRaycaster))]
public class UIDrawing : MonoBehaviour
{
    [Header("UI")]
    public RawImage drawingImage;    
    public int textureWidth = 512;
    public int textureHeight = 512;
    public int brushSize = 4;

    [Header("VR Input (BLS XR Origin)")]
    public InputActionReference triggerAction;  
    public Transform controllerTransform; 
    public LayerMask drawingLayerMask = ~0;
    [Range(0.01f, 1f)] public float uvSmoothing = 0.2f;

    public RectTransform cursorUI;
    public float cursorUISize = 12f;
    
    // 내부 상태
    Texture2D drawTex;
    bool isDrawing;
    bool hasPrev;
    Vector2 prevUV;
    Vector2 prevSmoothUV = new(-1f, -1f);
    readonly List<List<Vector2>> strokes = new();

    BoxCollider targetCollider;

    void Awake()
    {
        EnsureColliderOnRawImage();
    }

    void OnEnable()
    {
        if (triggerAction) triggerAction.action.Enable();
        EnsureTexture(); // 비활성→활성 시에도 보장
    }

    void OnDisable()
    {
        if (triggerAction) triggerAction.action.Disable();
        HideCursorUI();
    }

    void Start()
    {
        EnsureTexture();
        HideCursorUI();
    }

    void Update()
    {
        if (triggerAction == null || controllerTransform == null)
        {
            HideCursorUI();
            return;
        }

        float val = 0f;
        try { val = triggerAction.action.ReadValue<float>(); } catch { HideCursorUI(); return; }

        bool pressed = val > 0.5f;

        if (!pressed)
        {
            // ✨ 안 그릴 때: 커서만 표시
            if (isDrawing) isDrawing = false; // 드로잉 종료 처리
            UpdateHoverCursor();
            return;
        }

        // 🎨 그리는 중: 커서는 숨기고, 기존 드로잉 로직 수행
        HideCursorUI();

        if (pressed && !isDrawing)
        {
            isDrawing = true;
            hasPrev = false;
            prevSmoothUV = new(-1f, -1f);
            strokes.Add(new List<Vector2>());
        }

        if (isDrawing) TryDrawWithRay(); // 기존 함수: 실제로 선을 그리는 부분
    }


    void TryDrawWithRay()
    {
        EnsureTexture();
        var pos = controllerTransform.position;
        var dir = controllerTransform.forward;
        if (!IsValid(pos) || !IsValid(dir)) return;

        if (Physics.Raycast(pos, dir, out var hit, Mathf.Infinity, drawingLayerMask))
        {
            // 커서 갱신: 드로잉 대상에 맞았을 때만 표시
            UpdateCursorUIAtHit(hit.point, hit.collider == targetCollider);

            if (hit.collider == targetCollider)
                DrawAtWorldHit(hit.point);
        }
        else
        {
            UpdateCursorUIAtHit(Vector3.zero, false); // 못 맞추면 숨김
        }
    }


    void DrawAtWorldHit(Vector3 worldPoint)
    {
        var rt = drawingImage.rectTransform;
        Vector3 local = rt.InverseTransformPoint(worldPoint);

        float w = rt.rect.width;
        float h = rt.rect.height;
        float u = Mathf.Clamp01((local.x + rt.pivot.x * w) / w);
        float v = Mathf.Clamp01((local.y + rt.pivot.y * h) / h);
        Vector2 rawUV = new(u, v);

        Vector2 smooth = (prevSmoothUV.x < 0f)
            ? rawUV
            : Vector2.Lerp(prevSmoothUV, rawUV, uvSmoothing);

        prevSmoothUV = smooth;
        DrawAtUV(smooth);
    }

    void DrawAtUV(Vector2 uv)
    {
        EnsureTexture();

        strokes[^1].Add(uv);

        int x = Mathf.RoundToInt(uv.x * (textureWidth - 1));
        int y = Mathf.RoundToInt(uv.y * (textureHeight - 1));

        if (hasPrev)
        {
            int x0 = Mathf.RoundToInt(prevUV.x * (textureWidth - 1));
            int y0 = Mathf.RoundToInt(prevUV.y * (textureHeight - 1));
            BresenhamLine(x0, y0, x, y);
        }
        else
        {
            DrawBrush(x, y);
            hasPrev = true;
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
        EnsureTexture(); // ✅ 안전 보장

        var cols = new Color[textureWidth * textureHeight];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;
        drawTex.SetPixels(cols);
        drawTex.Apply(false);

        strokes.Clear();
        hasPrev = false;
        prevUV = Vector2.zero;
        prevSmoothUV = new(-1f, -1f);
        Debug.Log("[UIDrawing] Cleared.");
    }

    public List<List<Vector2>> GetStrokes() => strokes;
    public Texture2D GetStrokeTexture()
    {
        EnsureTexture();
        return drawTex;
    }

    void EnsureTexture()
    {
        if (drawingImage && drawingImage.texture != null && drawTex == drawingImage.texture)
            return;

        if (drawTex == null || drawTex.width != textureWidth || drawTex.height != textureHeight)
        {
            drawTex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            var cols = new Color[textureWidth * textureHeight];
            for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;
            drawTex.SetPixels(cols);
            drawTex.Apply(false);
        }
        if (drawingImage)
        {
            drawingImage.texture = drawTex;
            drawingImage.color = Color.white;
        }
    }

    void EnsureColliderOnRawImage()
    {
        if (!drawingImage)
        {
            Debug.LogError("[UIDrawing] drawingImage가 비었습니다.");
            return;
        }

        var rt = drawingImage.rectTransform;
        var go = drawingImage.gameObject;

        targetCollider = go.GetComponent<BoxCollider>();
        if (targetCollider == null)
            targetCollider = go.AddComponent<BoxCollider>();

        float w = rt.rect.width;
        float h = rt.rect.height;
        targetCollider.size   = new Vector3(w, h, 0.01f);
        targetCollider.center = new Vector3((0.5f - rt.pivot.x) * w,
                                            (0.5f - rt.pivot.y) * h,
                                            0f);
    }

    static bool IsValid(Vector3 v)
    {
        return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                 float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
    }
    void UpdateCursorUIAtHit(Vector3 worldPoint, bool visible)
    {
        if (!cursorUI || !drawingImage) return;

        if (!visible)
        {
            if (cursorUI.gameObject.activeSelf) cursorUI.gameObject.SetActive(false);
            return;
        }

        var rt = drawingImage.rectTransform;
        Vector3 local = rt.InverseTransformPoint(worldPoint);

        float w = rt.rect.width;
        float h = rt.rect.height;

        float u = Mathf.Clamp01((local.x + rt.pivot.x * w) / w);
        float v = Mathf.Clamp01((local.y + rt.pivot.y * h) / h);

        Vector2 anchored = new(
            (u - rt.pivot.x) * w,
            (v - rt.pivot.y) * h
        );

        cursorUI.anchoredPosition = anchored;
        cursorUI.sizeDelta = Vector2.one * cursorUISize;

        if (!cursorUI.gameObject.activeSelf) cursorUI.gameObject.SetActive(true);
    }

    void HideCursorUI()
    {
        if (cursorUI && cursorUI.gameObject.activeSelf) cursorUI.gameObject.SetActive(false);
    }
    
    void UpdateHoverCursor()
    {
        if (!controllerTransform) { HideCursorUI(); return; }

        var pos = controllerTransform.position;
        var dir = controllerTransform.forward;
        if (!IsValid(pos) || !IsValid(dir)) { HideCursorUI(); return; }

        if (Physics.Raycast(pos, dir, out var hit, Mathf.Infinity, drawingLayerMask))
        {
            // 드로잉 대상(우리 RawImage 콜라이더)을 맞춘 경우에만 표시
            UpdateCursorUIAtHit(hit.point, hit.collider == targetCollider);
        }
        else
        {
            HideCursorUI();
        }
    }


}
