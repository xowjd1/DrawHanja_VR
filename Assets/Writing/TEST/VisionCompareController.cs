using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class VisionCompareController : MonoBehaviour
{
    [Header("드로잉 스크립트")]
    public TransparentUIDrawing drawing; // 획만 담긴 투명 텍스처

    [Header("UI 컴포넌트")]
    public Button completeButton;
    public TMP_Text   resultText;

    string apiKey;

    void Awake() {
        apiKey = Resources.Load<TextAsset>("vision_api_key").text.Trim();
        Debug.Log($"[Vision] Loaded API Key (length={apiKey.Length})");
    }

    void Start() {
        completeButton.onClick.AddListener(OnCompleteClicked);
    }

    async void OnCompleteClicked()
{
    Debug.Log("[Vision] 비교 시작");

    // 1) 획만 있는 투명 텍스처 가져오기
    Texture2D strokeTex = drawing.GetStrokeTexture();
    int w = strokeTex.width, h = strokeTex.height;

    // 2) 흰 배경 + 검정 획 composite 생성
    Texture2D ocrTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
    Color[] strokePx = strokeTex.GetPixels();
    Color[] ocrPx    = new Color[strokePx.Length];
    for (int i = 0; i < strokePx.Length; i++)
    {
        // α가 충분히 있으면 검정, 아니면 흰색
        ocrPx[i] = strokePx[i].a > 0.1f 
                    ? Color.black 
                    : Color.white;
    }
    ocrTex.SetPixels(ocrPx);
    ocrTex.Apply();
    Debug.Log($"[Vision] OCR용 이미지 생성 완료 ({w}×{h})");

    // (디버그) 로컬에 저장해 보고 싶다면
    // var path = Application.persistentDataPath + "/ocr.png";
    // System.IO.File.WriteAllBytes(path, ocrTex.EncodeToPNG());
    // Debug.Log($"[Vision] OCR 이미지 저장: {path}");

    // 3) PNG → Base64
    byte[] pngBytes = ocrTex.EncodeToPNG();
    Debug.Log($"[Vision] OCR PNG 크기: {pngBytes.Length}");

    string base64Image = Convert.ToBase64String(pngBytes);
    Debug.Log($"[Vision] Base64 길이: {base64Image.Length}");

    // 4) 요청 JSON 생성
    var vr = new VisionRequest {
        requests = new[] {
            new ImageRequest {
                image = new ImageContent { content = base64Image },
                features = new[]{ new Feature { type = "TEXT_DETECTION", maxResults = 1 } }
            }
        }
    };
    string requestJson = JsonUtility.ToJson(vr);
    Debug.Log($"[Vision] Request JSON 부분: {requestJson.Substring(0,200)}...");

    // 5) HTTP POST
    string url = $"https://vision.googleapis.com/v1/images:annotate?key={apiKey}";
    using var uwr = new UnityWebRequest(url, "POST");
    uwr.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
    uwr.downloadHandler = new DownloadHandlerBuffer();
    uwr.SetRequestHeader("Content-Type", "application/json");
    Debug.Log("[Vision] 요청 전송...");
    await uwr.SendWebRequest();

    if (uwr.result != UnityWebRequest.Result.Success) {
        Debug.LogError($"[Vision] 호출 실패: {uwr.error}");
        Debug.LogError($"[Vision] 응답: {uwr.downloadHandler.text}");
        resultText.text = "API 호출 실패";
        return;
    }

    Debug.Log($"[Vision] 응답 본문:\n{uwr.downloadHandler.text}");

    // 6) 응답 파싱 & 판정
    var response = JsonUtility.FromJson<VisionResponse>(uwr.downloadHandler.text);
    string detected = "";
    try {
        detected = response.responses[0].textAnnotations[0].description.Trim();
    } catch {}
    Debug.Log($"[Vision] 인식된 문자열: '{detected}'");

    if (detected == "火") {
        resultText.text = $"정답! (인식된: {detected})";
        Debug.Log("[Vision] 판정: 정답");
    }
    else if (string.IsNullOrEmpty(detected)) {
        resultText.text = "글자를 인식하지 못했어요.";
        Debug.Log("[Vision] 판정: 인식실패");
    }
    else {
        resultText.text = $"틀렸어요 (인식된: {detected})";
        Debug.Log("[Vision] 판정: 오답");
    }
}


    [Serializable]
    class VisionRequest {
        public ImageRequest[] requests;
    }
    [Serializable]
    class ImageRequest {
        public ImageContent image;
        public Feature[]    features;
    }
    [Serializable]
    class ImageContent {
        public string content;
    }
    [Serializable]
    class Feature {
        public string type;
        public int    maxResults;
    }

    [Serializable]
    class VisionResponse {
        public Response[] responses;
    }
    [Serializable]
    class Response {
        public TextAnnotation[] textAnnotations;
    }
    [Serializable]
    class TextAnnotation {
        public string description;
    }
}
