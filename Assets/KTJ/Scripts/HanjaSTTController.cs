using UnityEngine;
using UnityEngine.XR;

public class HanjaSTTController : MonoBehaviour
{
    public SpeechToText sttManager;        // 기존 사용 중인 STT 매니저
    public HanjaRecognizer hanjaRecognizer;

    [Header("XR Input")]
    public XRNode inputSource = XRNode.LeftHand;
    private InputDevice device;
    private bool prevPrimaryPressed = false;

    [Header("Capture Options")]
    [Tooltip("버튼을 너무 짧게 눌러도 최소 이 시간(초)만큼은 녹음되도록 보장")]
    public float minHoldSeconds = 0.6f;
    [Tooltip("버튼을 뗀 뒤 꼬리(후행 무음)로 붙여줄 시간(초)")]
    public float tailSeconds = 0.25f;

    private float pressTime = -1f;
    private bool isRecording = false;

    void Awake()
    {
        device = InputDevices.GetDeviceAtXRNode(inputSource);
    }

    void Start()
    {
        // 기존 콜백 유지, 단 정규화 거쳐서 전달
        sttManager.OnTranscriptionComplete = OnSpeechRecognizedNormalized;
        // (선택) 쓰는 STT가 언어 설정 지원하면 일본어 고정
        TrySetLanguageToJapanese();
    }

    void Update()
    {
        if (!device.isValid) device = InputDevices.GetDeviceAtXRNode(inputSource);

        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed))
        {
            // 눌린 순간
            if (primaryPressed && !prevPrimaryPressed)
            {
                pressTime = Time.time;
                StartListening();
            }
            // 뗀 순간
            if (!primaryPressed && prevPrimaryPressed)
            {
                float held = (pressTime > 0f) ? (Time.time - pressTime) : 0f;
                float needMore = Mathf.Max(0f, minHoldSeconds - held);

                // 최소 길이 보장 + 꼬리시간 부여 후 정지
                if (isRecording)
                    Invoke(nameof(StopListening), needMore + tailSeconds);
            }
            prevPrimaryPressed = primaryPressed;
        }
    }

    public void StartListening()
    {
        if (isRecording) return;
        isRecording = true;
        sttManager.StartRecording();
    }

    public void StopListening()
    {
        if (!isRecording) return;
        isRecording = false;

        // sttManager에 StopRecording()이 있다면 호출
        var m = sttManager.GetType().GetMethod("StopRecording",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (m != null) m.Invoke(sttManager, null);
        // 만약 없으면, sttManager가 내부 타이머로 자동 종료하더라도 위의 minHoldSeconds/tailSeconds가
        // 너무 짧게 끊기는 위험을 많이 줄여줍니다.
    }

    void OnSpeechRecognizedNormalized(string recognizedText)
    {
        // 1) 원문 로깅
        Debug.Log($"[STT Raw] \"{recognizedText}\"");

        // 2) 정규화(히/hi/ひ/ヒ/火 → ひ 등)
        string canon = JapaneseLexicon.NormalizeForMatching(recognizedText);
        Debug.Log($"[STT Canon] \"{canon}\"");

        // 3) 매칭 시도(정규화 우선, 실패 시 원문도 한 번 더)
        hanjaRecognizer.TryMatchHanja(canon);
        if (!string.Equals(canon, recognizedText))
            hanjaRecognizer.TryMatchHanja(recognizedText);
    }

    // (선택) STT 매니저가 언어/작업 설정 프로퍼티를 노출한다면 여기서 강제
    void TrySetLanguageToJapanese()
    {
        var langProp = sttManager.GetType().GetProperty("Language");
        if (langProp != null && langProp.CanWrite)
        {
            try { langProp.SetValue(sttManager, "ja-JP"); Debug.Log("[STT] Language=ja-JP"); } catch { }
        }

        // 구두점/자동 언어감지 끄기 옵션이 있으면 끄는 게 짧은 음절 인식에 유리한 경우가 많습니다.
        var autoProp = sttManager.GetType().GetProperty("AutoDetectLanguage");
        if (autoProp != null && autoProp.CanWrite)
        {
            try { autoProp.SetValue(sttManager, false); Debug.Log("[STT] AutoDetectLanguage=false"); } catch { }
        }
    }
}
