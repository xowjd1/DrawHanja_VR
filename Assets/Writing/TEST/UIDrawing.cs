using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(GraphicRaycaster))]
public class TransparentUIDrawing : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public RawImage drawingImage;       // DrawingArea 할당
    public int    textureWidth  = 512;
    public int    textureHeight = 512;
    public int    brushSize     = 4;

    // 내부 상태
    private Texture2D       drawTex;
    private bool            isDrawing;
    private Vector2         prevUV;
    private GraphicRaycaster raycaster;
    private EventSystem      eventSystem;
    private List<List<Vector2>> strokes = new();

    void Awake()
    {
        raycaster   = GetComponent<GraphicRaycaster>();
        eventSystem = EventSystem.current;
    }

    void Start()
    {
        // 1) 투명 배경 텍스쳐 생성
        drawTex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        Color[] cols = new Color[textureWidth * textureHeight];
        for (int i = 0; i < cols.Length; i++)
            cols[i] = Color.clear;               // ← 완전 투명으로 초기화
        drawTex.SetPixels(cols);
        drawTex.Apply();

        // 2) DrawingArea에 할당
        drawingImage.texture = drawTex;
        drawingImage.color   = Color.white;      // 획 색 그대로 보이게
    }

    void Update()
    {
        // 마우스 누르면 드로잉 시작
        if (Input.GetMouseButtonDown(0))
        {
            isDrawing = true;
            strokes.Add(new List<Vector2>());
            prevUV = Vector2.zero;
        }

        // 누르고 있는 동안
        if (isDrawing && Input.GetMouseButton(0))
        {
            var pd = new PointerEventData(eventSystem) { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            raycaster.Raycast(pd, results);

            foreach (var rr in results)
            {
                if (rr.gameObject == drawingImage.gameObject)
                {
                    DrawAt(rr.screenPosition);
                    break;
                }
            }
        }

        // 버튼 떼면 드로잉 종료
        if (isDrawing && Input.GetMouseButtonUp(0))
        {
            isDrawing = false;
        }
    }

    void DrawAt(Vector2 screenPos)
    {
        // 1) Screen → 로컬 → UV
        RectTransform rt = drawingImage.rectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out Vector2 local);
        float px = local.x + rt.rect.width  * .5f;
        float py = local.y + rt.rect.height * .5f;
        float u  = Mathf.Clamp01(px / rt.rect.width);
        float v  = Mathf.Clamp01(py / rt.rect.height);
        Vector2 uv = new Vector2(u, v);
        strokes[^1].Add(uv);

        // 2) UV → 픽셀 좌표
        int x = Mathf.RoundToInt(u * (textureWidth  - 1));
        int y = Mathf.RoundToInt(v * (textureHeight - 1));

        // 3) 선 연결 또는 점 찍기
        if (prevUV != Vector2.zero)
        {
            int x0 = Mathf.RoundToInt(prevUV.x * (textureWidth  - 1));
            int y0 = Mathf.RoundToInt(prevUV.y * (textureHeight - 1));
            BresenhamLine(x0, y0, x, y);
        }
        else DrawBrush(x, y);

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
                if (dx*dx + dy*dy <= r*r)
                    SetPixelSafe(cx + dx, cy + dy, Color.black);
    }

    void SetPixelSafe(int x, int y, Color c)
    {
        if (x < 0 || x >= textureWidth || y < 0 || y >= textureHeight) return;
        drawTex.SetPixel(x, y, c);
    }

    // 획 벡터 데이터
    public List<List<Vector2>> GetStrokes() => strokes;

    // 나중 비교를 위해 투명 배경 + 획만 담긴 drawTex를 사용하세요
    public Texture2D GetStrokeTexture() => drawTex;
}
