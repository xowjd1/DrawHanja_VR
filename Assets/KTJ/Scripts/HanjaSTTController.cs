using UnityEngine;

public class HanjaSTTController : MonoBehaviour
{
    public SpeechToText sttManager;
    public HanjaRecognizer hanjaRecognizer;

    void Start()
    {
        sttManager.OnTranscriptionComplete = OnSpeechRecognized;
    }

    public void StartListening()
    {
        sttManager.StartRecording();
    }

    void OnSpeechRecognized(string recognizedText)
    {
        hanjaRecognizer.TryMatchHanja(recognizedText);
    }
}
