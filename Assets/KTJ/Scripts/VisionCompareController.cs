using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Text;

public class VisionCompareController : MonoBehaviour
{
    [Header("드로잉 스크립트")]
    public UIDrawing drawing;

    [Header("한자 데이터베이스")]
    public HanjaDataBase database;

    [Header("UI 컴포넌트")]
    public Image practiceImageUI;         // ✅ 연습 이미지를 보여주는 Image (기존 hanjaPracticeGO 대신)
    public Button completeButton;
    public TMP_Text resultText;

    string apiKey;
    
    Vector2 _defaultSizeDelta;
    Vector3 _defaultScale;
    bool    _defaultPreserveAspect;

    void Awake() {
        apiKey = Resources.Load<TextAsset>("google_api_key").text.Trim();
        Debug.Log($"[Vision] Loaded API Key (length={apiKey.Length})");
        if (practiceImageUI != null) {
            var rt = practiceImageUI.rectTransform;
            _defaultSizeDelta     = rt.sizeDelta;
            _defaultScale         = rt.localScale;
            _defaultPreserveAspect= practiceImageUI.preserveAspect;
        }
    }

    void Start() {
        completeButton.onClick.AddListener(OnCompleteClicked);
    }

    public void ResetResultUI(Sprite practiceSprite = null)
    {
        // 결과 텍스트 숨김
        if (resultText) { resultText.gameObject.SetActive(false); resultText.text = ""; }
        if (practiceImageUI)
        {
            // ✅ 크기/스케일/옵션 원상복구
            var rt = practiceImageUI.rectTransform;
            rt.sizeDelta = _defaultSizeDelta;
            rt.localScale = _defaultScale;
            practiceImageUI.preserveAspect = _defaultPreserveAspect;
            
            if (practiceSprite) practiceImageUI.sprite = practiceSprite;

        }

    }

    async void OnCompleteClicked()
    {
        Debug.Log("[Vision] 비교 시작");

        // 1) 획 텍스처 → OCR용 흑백 합성
        Texture2D strokeTex = drawing.GetStrokeTexture();
        int w = strokeTex.width, h = strokeTex.height;

        Texture2D ocrTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] strokePx = strokeTex.GetPixels();
        for (int i = 0; i < strokePx.Length; i++)
            strokePx[i] = (strokePx[i].a > 0.1f) ? Color.black : Color.white;
        ocrTex.SetPixels(strokePx);
        ocrTex.Apply();

        // 2) PNG → Base64
        byte[] pngBytes = ocrTex.EncodeToPNG();
        string base64Image = Convert.ToBase64String(pngBytes);

        // 3) 요청 JSON
        var vr = new VisionRequest {
            requests = new[] {
                new ImageRequest {
                    image = new ImageContent { content = base64Image },
                    features = new[]{ new Feature { type = "TEXT_DETECTION", maxResults = 1 } }
                }
            }
        };
        string requestJson = JsonUtility.ToJson(vr);

        // 4) HTTP POST
        string url = $"https://vision.googleapis.com/v1/images:annotate?key={apiKey}";
        using var uwr = new UnityWebRequest(url, "POST");
        uwr.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        await uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success) {
            Debug.LogError($"[Vision] 호출 실패: {uwr.error}");
            Debug.LogError($"[Vision] 응답: {uwr.downloadHandler.text}");
            if (resultText) { resultText.gameObject.SetActive(true); resultText.text = "API 호출 실패"; }
            return;
        }

        var response = JsonUtility.FromJson<VisionResponse>(uwr.downloadHandler.text);
        string detected = "";
        try {
            detected = response.responses[0].textAnnotations[0].description;
        } catch {}

        // ✅ OCR 결과 정리: 개행/스페이스 제거하고 첫 글자만 사용
        if (!string.IsNullOrEmpty(detected))
        {
            detected = detected.Trim();
            detected = detected.Replace("\n","").Replace("\r","");
            if (detected.Length > 1) detected = detected.Substring(0, 1);
        }

        string[] allowed = database.allowedHanja.Select(k => k.character).ToArray();

        if (string.IsNullOrEmpty(detected))
        {
            if (resultText) { resultText.gameObject.SetActive(true); resultText.text = "글자를 인식하지 못했어요."; }
            return;
        }

        if (allowed.Contains(detected))
        {
            var data = database.allowedHanja.FirstOrDefault(k => k.character == detected);
            if (data != null && data.completeImage != null)
            {
                // ✅ 핵심: 같은 슬롯에 그대로 완성 스프라이트로 교체
                if (practiceImageUI != null)
                {
                    practiceImageUI.sprite = data.completeImage;
                    practiceImageUI.preserveAspect = true;
                    practiceImageUI.SetNativeSize();
                }

                if (resultText) { resultText.gameObject.SetActive(true); resultText.text = "정답!"; }
                if (drawing != null) drawing.ClearAllStrokes();
            }
            else
            {
                if (resultText) { resultText.gameObject.SetActive(true); resultText.text = "완성 이미지를 찾을 수 없어요."; }
            }
        }
        else
        {
            if (resultText) { resultText.gameObject.SetActive(true); resultText.text = "틀렸어요"; }
        }
    }

    // ===== DTO =====
    [Serializable] class VisionRequest { public ImageRequest[] requests; }
    [Serializable] class ImageRequest { public ImageContent image; public Feature[] features; }
    [Serializable] class ImageContent { public string content; }
    [Serializable] class Feature { public string type; public int maxResults; }
    [Serializable] class VisionResponse { public Response[] responses; }
    [Serializable] class Response { public TextAnnotation[] textAnnotations; }
    [Serializable] class TextAnnotation { public string description; }
}
