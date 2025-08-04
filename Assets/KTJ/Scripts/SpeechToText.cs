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

        // (A) 디바이스가 지원하는 최소/최대 주파수 가져오기
        int minFreq, maxFreq;
        Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);
        // maxFreq가 0이면 제한 없음이므로 44100Hz로 가정
        int recordHz = (maxFreq == 0) ? 44100 : maxFreq;

        // (B) 지원 주파수로 녹음 시작
        recordedClip = Microphone.Start(
            null,
            false,
            Mathf.CeilToInt(recordDuration),
            recordHz
        );
        Debug.Log($"녹음 시작 (주파수: {recordHz}Hz, duration: {recordDuration}s)");
        StartCoroutine(WaitAndSend(recordDuration));
    }


    private IEnumerator WaitAndSend(float duration)
    {
        int startPos = Microphone.GetPosition(null);
        yield return new WaitUntil(() => Microphone.GetPosition(null) > 0);
        Debug.Log($"녹음 포지션 스타트: {startPos}, 현재: {Microphone.GetPosition(null)}");

        
        yield return new WaitForSeconds(duration);

        Microphone.End(null);
        Debug.Log("녹음 종료");
        
        var src = gameObject.AddComponent<AudioSource>();
        src.clip = recordedClip;
        src.Play();
        Debug.Log("▶ 녹음된 오디오 재생중… (볼륨 ↑)");

        // 파일로도 잠깐 저장해 보기
        string debugPath = Path.Combine(Application.persistentDataPath, "debug.wav");
        File.WriteAllBytes(debugPath, ConvertToWavBytes(recordedClip));
        Debug.Log($"▶ 디버그용 WAV 저장: {debugPath}");

        StartCoroutine(SendClipToGoogle(recordedClip));
    }

    private IEnumerator SendClipToGoogle(AudioClip clip)
    {
        byte[] wavBytes  = ConvertToWavBytes(clip);
        string base64Wav = Convert.ToBase64String(wavBytes);

        // ① 녹음된 clip의 실제 주파수를 사용
        int sampleRate = clip.frequency;

        var req = new RecognizeRequest {
            config = new RecognitionConfig {
                encoding        = "LINEAR16",
                sampleRateHertz = sampleRate,
                languageCode    = "ja-JP"
            },
            audio = new RecognitionAudio { content = base64Wav }
        };

        string json = JsonUtility.ToJson(req);
        Debug.Log("STT 요청 JSON:\n" + json);

        var uwr = new UnityWebRequest(
            $"https://speech.googleapis.com/v1/speech:recognize?key={googleApiKey}",
            "POST"
        );
        byte[] body = Encoding.UTF8.GetBytes(json);
        uwr.uploadHandler   = new UploadHandlerRaw(body);
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
            Debug.LogError($"STT 오류: HTTP/{uwr.responseCode}");
            Debug.LogError("응답 본문: " + uwr.downloadHandler.text);
        }
    }


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
    public class RecognizeRequest
    {
        public RecognitionConfig config;
        public RecognitionAudio  audio;
    }

    [Serializable]
    public class RecognitionConfig
    {
        public string encoding;
        public int    sampleRateHertz;
        public string languageCode;
    }

    [Serializable]
    public class RecognitionAudio
    {
        public string content;
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
