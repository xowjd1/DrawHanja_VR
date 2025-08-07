using UnityEngine;
using UnityEngine.XR;

public class HanjaSTTController : MonoBehaviour
{
    public SpeechToText sttManager;
    public HanjaRecognizer hanjaRecognizer;
    
    public XRNode inputSource = XRNode.LeftHand;  
    private InputDevice device;

    private bool prevPrimaryPressed = false;
    
    void Awake()
    {
        device = InputDevices.GetDeviceAtXRNode(inputSource);
    }
    
    void Start()
    {
        sttManager.OnTranscriptionComplete = OnSpeechRecognized;
    }
    void Update()
    {
        // 디바이스 유효성 재확인
        if (!device.isValid)
            device = InputDevices.GetDeviceAtXRNode(inputSource);

        // 왼손 컨트롤러 X 버튼(primaryButton) 상태 읽기
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed))
        {
            // 눌러진 순간에만 StartListening 호출
            if (primaryPressed && !prevPrimaryPressed)
            {
                StartListening();
            }
            prevPrimaryPressed = primaryPressed;
        }
    }
    

    public void StartListening()
    {
        sttManager.StartRecording();
    }

    void OnSpeechRecognized(string recognizedText)
    {
        Debug.Log($"[Debug] RecognizedReading: '{recognizedText}'");
        hanjaRecognizer.TryMatchHanja(recognizedText);
    }
}