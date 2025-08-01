using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;
using System.Text;

public class SpeechToText : MonoBehaviour
{
   [Header("설정")]
    public string apiKeyFileName = "google_api_key";     // 구글 API 키 입력
    public float recordDuration = 2f;   // 녹음 시간 (초)

    [Header("콜백")]
    public Action<string> OnTranscriptionComplete;

    private string googleApiKey;
    private AudioClip recordedClip;

    // -------------------- 🔴 마이크 녹음 --------------------

    void Awake()
    {
        LoadApiKeyFromResources();
    }
    
    private void LoadApiKeyFromResources()
    {
        TextAsset keyFile = Resources.Load<TextAsset>(apiKeyFileName);
        if (keyFile != null)
        {
            googleApiKey = keyFile.text.Trim();  // 개행문자 제거
            Debug.Log("API 키 로드 완료");
        }
        else
        {
            Debug.LogError("API 키 파일을 Resources에서 찾을 수 없습니다: " + apiKeyFileName);
        }
    }
    
    public void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("마이크 장치가 없음!");
            return;
        }

        recordedClip = Microphone.Start(null, false, (int)recordDuration, 16000);
        Debug.Log("녹음 시작...");
        StartCoroutine(WaitAndSend(recordDuration));
    }

    private IEnumerator WaitAndSend(float duration)
    {
        yield return new WaitForSeconds(duration);

        Microphone.End(null);
        Debug.Log("녹음 종료");

        StartCoroutine(SendClipToGoogle(recordedClip));
    }

    // -------------------- 🔴 WAV 변환 및 전송 --------------------

    private IEnumerator SendClipToGoogle(AudioClip clip)
    {
        byte[] wavBytes = ConvertToWavBytes(clip);
        string base64Wav = Convert.ToBase64String(wavBytes);

        var requestJson = new
        {
            config = new {
                encoding = "LINEAR16",
                sampleRateHertz = 16000,
                languageCode = "ja-JP"
            },
            audio = new {
                content = base64Wav
            }
        };

        string json = JsonUtility.ToJson(requestJson);
        var uwr = new UnityWebRequest($"https://speech.googleapis.com/v1/speech:recognize?key={googleApiKey}", "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        uwr.uploadHandler = new UploadHandlerRaw(body);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.Success)
        {
            string result = ExtractTranscript(uwr.downloadHandler.text);
            Debug.Log($"인식 결과: {result}");
            OnTranscriptionComplete?.Invoke(result);
        }
        else
        {
            Debug.LogError("STT 오류: " + uwr.error);
        }
    }

    // -------------------- 🔴 결과 추출 --------------------

    private string ExtractTranscript(string json)
    {
        var wrapper = JsonUtility.FromJson<SpeechResponse>(json);
        if (wrapper.results != null && wrapper.results.Length > 0 &&
            wrapper.results[0].alternatives.Length > 0)
        {
            return wrapper.results[0].alternatives[0].transcript;
        }
        return "";
    }

    [Serializable]
    public class SpeechResponse
    {
        public SpeechResult[] results;
    }

    [Serializable]
    public class SpeechResult
    {
        public Alternative[] alternatives;
    }

    [Serializable]
    public class Alternative
    {
        public string transcript;
    }

    // -------------------- 🔴 AudioClip → WAV --------------------

    private byte[] ConvertToWavBytes(AudioClip clip)
    {
        int length = clip.samples * clip.channels;
        float[] data = new float[length];
        clip.GetData(data, 0);

        const int headerSize = 44;
        ushort bitDepth = 16;
        byte[] pcmBytes = ConvertToPCM16(data);
        int fileSize = pcmBytes.Length + headerSize;

        using (MemoryStream stream = new MemoryStream())
        {
            WriteWavHeader(stream, clip, fileSize, bitDepth);
            stream.Write(pcmBytes, 0, pcmBytes.Length);
            return stream.ToArray();
        }
    }

    private void WriteWavHeader(Stream stream, AudioClip clip, int fileSize, ushort bitDepth)
    {
        int hz = clip.frequency;
        int channels = clip.channels;
        int byteRate = hz * channels * bitDepth / 8;

        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(fileSize - 8);
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((ushort)1);
            writer.Write((ushort)channels);
            writer.Write(hz);
            writer.Write(byteRate);
            writer.Write((ushort)(channels * bitDepth / 8));
            writer.Write(bitDepth);
            writer.Write(Encoding.UTF8.GetBytes("data"));
            writer.Write(fileSize - 44);
        }
    }

    private byte[] ConvertToPCM16(float[] data)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        short max = short.MaxValue;
        foreach (var sample in data)
        {
            writer.Write((short)(sample * max));
        }
        return stream.ToArray();
    }
}
