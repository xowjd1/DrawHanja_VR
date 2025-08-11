// Assets/KTJ/Scripts/SpeechToText.cs
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;
using System.Text;

public class SpeechToText : MonoBehaviour
{
    [Header("녹음")]
    [Tooltip("마이크 샘플레이트(고정 권장)")]
    public int sampleRate = 48000;
    [Tooltip("마이크 내부 버퍼 길이(초)")]
    public float recordDuration = 2f;

    [Tooltip("최소 유효 녹음(초) — 단음절 안정화")]
    public float minRecordSec = 0.6f;

    [Tooltip("버튼 뗀 뒤 꼬리(초) — 끝자락 유실 방지")]
    public float tailSec = 0.20f;

    [Header("무음 트림(VAD)")]
    [Range(0.0001f, 0.05f)] public float vadThreshold = 0.005f; // RMS 임계
    public int vadWindow = 256;                                 // RMS 윈도우
    [Tooltip("전송 전 살짝 증폭(1.2~1.8 권장)")]
    public float preGain = 1.4f;

    [Header("Google STT")]
    public string languageCode = "ja-JP";
    [Tooltip("후보 최대 개수")]
    public int maxAlternatives = 5;
    [Tooltip("프레이즈 힌트(정확도↑)")]
    public string[] phrases = new[] { "ひ", "火", "きる", "斬る", "ちから", "力", "黄色" };
    [Range(0f, 20f)] public float phraseBoost = 20f;

    [Header("키/리소스")]
    [Tooltip("Resources/<name>.txt 에 API 키 1줄")]
    public string apiKeyFileName = "google_api_key";

    [Header("플로우 가드")]
    [Tooltip("시작~다음 시작 최소 간격(초)")]
    public float minStartInterval = 0.8f;

    [Header("디버그")]
    public bool playRecordedForDebug = true;    // 캡처한 오디오 재생
    public bool saveDebugWav = true;           // WAV 저장
    public bool logPayloadVerbose = false;     // 요청 전체 JSON 로그

    [Header("콜백")]
    public Action<string> OnTranscriptionComplete;

    // ---- 내부상태 ----
    private string googleApiKey;
    private AudioClip recordedClip;
    private Coroutine recordRoutine;
    private bool sttBusy = false;
    private float lastStartTime = -10f;

    void Awake()
    {
        LoadApiKeyFromResources();
    }

    void OnDisable()
    {
        SafeMicEnd();
        sttBusy = false;
        recordRoutine = null;
    }

    // ====== Public API ======
    public void StartRecording()
    {
        // 디바운스/재진입 방지
        if (sttBusy)
        {
            Debug.LogWarning("[STT] Busy. Ignored StartRecording.");
            return;
        }
        if (Time.unscaledTime - lastStartTime < minStartInterval)
        {
            Debug.LogWarning("[STT] Debounced. Try a bit later.");
            return;
        }
        lastStartTime = Time.unscaledTime;
        sttBusy = true;

        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogError("[STT] 마이크 장치를 찾을 수 없음");
            sttBusy = false;
            return;
        }

        if (recordedClip != null) Destroy(recordedClip);

        // 샘플레이트 고정(48000 권장)
        recordedClip = Microphone.Start(null, false, Mathf.CeilToInt(recordDuration), sampleRate);
        if (recordedClip == null)
        {
            Debug.LogError("[STT] Microphone.Start 실패");
            sttBusy = false;
            return;
        }

        recordRoutine = StartCoroutine(WaitAndSend());
        Debug.Log($"녹음 시작 (주파수: {sampleRate}Hz, duration: {recordDuration}s)");
    }

    // ====== Core Flow ======
    private IEnumerator WaitAndSend()
    {
        try
        {
            // 마이크 시작 대기
            yield return new WaitUntil(() => Microphone.GetPosition(null) > 0);

            // 최소 유효 길이 확보
            float t0 = Time.unscaledTime;
            while (Microphone.GetPosition(null) < sampleRate * Mathf.Max(minRecordSec, 0.4f))
            {
                if (Time.unscaledTime - t0 > recordDuration) break;
                yield return null;
            }

            // 꼬리 확보
            yield return new WaitForSeconds(tailSec);

            // 읽고 종료
            Microphone.End(null);
            Debug.Log("녹음 종료");

            // 모노 배열화 → 무음 트림
            var mono = ClipToMonoArray(recordedClip);
            var trimmed = TrimSilence(mono, sampleRate, vadThreshold, vadWindow);

            if (trimmed.Length < Mathf.RoundToInt(sampleRate * 0.15f))
            {
                Debug.LogWarning("[STT] 무음/너무 짧은 입력 → 재시도 권장");
                OnTranscriptionComplete?.Invoke(string.Empty);
                yield break;
            }

            // 프리게인
            if (Mathf.Abs(preGain - 1f) > 0.001f)
            {
                for (int i = 0; i < trimmed.Length; i++)
                    trimmed[i] = Mathf.Clamp(trimmed[i] * preGain, -1f, 1f);
            }

            // 디버그 재생/저장
            if (playRecordedForDebug)
            {
                if (!TryGetComponent<AudioSource>(out var src))
                    src = gameObject.AddComponent<AudioSource>();
                src.clip = ArrayToClip(trimmed, sampleRate);
                src.loop = false;
                src.Play();
                Debug.Log("▶ 디버그용 오디오 재생중… (볼륨 ↑)");
            }

            if (saveDebugWav)
            {
                string path = Path.Combine(Application.persistentDataPath, "debug.wav");
                File.WriteAllBytes(path, BuildWavBytes(trimmed, sampleRate));
                Debug.Log($"▶ 디버그용 WAV 저장: {path}");
            }

            // Google STT 전송 (RAW PCM LINEAR16)
            yield return StartCoroutine(SendPcmToGoogle(trimmed, sampleRate));
        }
        finally
        {
            recordRoutine = null;
            sttBusy = false; // 반드시 해제
        }
    }

    // ====== Google Speech-to-Text ======
    [Serializable] public class SpeechContext { public string[] phrases; public float boost; }
    [Serializable] public class RecognitionConfig
    {
        public string encoding;                // "LINEAR16"
        public int sampleRateHertz;            // 48000
        public string languageCode;            // "ja-JP"
        public int maxAlternatives;            // e.g., 5
        public SpeechContext[] speechContexts; // 힌트
    }
    [Serializable] public class RecognitionAudio { public string content; }
    [Serializable] public class RecognizeRequest { public RecognitionConfig config; public RecognitionAudio audio; }

    [Serializable] public class Alternative { public string transcript; public float confidence; }
    [Serializable] public class SpeechResult { public Alternative[] alternatives; }
    [Serializable] public class SpeechResponse { public SpeechResult[] results; }

    private IEnumerator SendPcmToGoogle(float[] mono, int sr)
    {
        if (string.IsNullOrEmpty(googleApiKey))
        {
            Debug.LogError("[STT] API Key 비어있음. Resources/" + apiKeyFileName + ".txt 확인");
            yield break;
        }

        // RAW 16-bit little-endian PCM
        byte[] pcm = FloatToPCM16(mono);
        string b64 = Convert.ToBase64String(pcm);

        var req = new RecognizeRequest
        {
            config = new RecognitionConfig
            {
                encoding = "LINEAR16",
                sampleRateHertz = sr,
                languageCode = languageCode,
                maxAlternatives = maxAlternatives,
                speechContexts = (phrases != null && phrases.Length > 0)
                    ? new[] { new SpeechContext { phrases = phrases, boost = phraseBoost } }
                    : null
            },
            audio = new RecognitionAudio { content = b64 }
        };

        string json = JsonUtility.ToJson(req);
        if (logPayloadVerbose) Debug.Log("STT 요청 JSON:\n" + json);
        else Debug.Log($"STT 요청: enc={req.config.encoding}, sr={sr}, bytes={pcm.Length}, hints={(phrases?.Length ?? 0)}");

        var uwr = new UnityWebRequest($"https://speech.googleapis.com/v1/speech:recognize?key={googleApiKey}", "POST");
        uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json; charset=utf-8");

        yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        bool isError = uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError;
#else
        bool isError = uwr.isNetworkError || uwr.isHttpError;
#endif
        if (isError)
        {
            Debug.LogError($"[STT] HTTP {(int)uwr.responseCode} {uwr.error}\n{uwr.downloadHandler.text}");
            OnTranscriptionComplete?.Invoke(string.Empty);
            yield break;
        }

        string respText = uwr.downloadHandler.text;
        string transcript = ExtractBestTranscript(respText);
        transcript = NormalizeShortKana(transcript);

        Debug.Log("인식 결과: " + transcript);
        OnTranscriptionComplete?.Invoke(transcript ?? string.Empty);
    }

    private string ExtractBestTranscript(string json)
    {
        var resp = JsonUtility.FromJson<SpeechResponse>(json);
        if (resp?.results == null || resp.results.Length == 0) return "";

        // 1) 힌트에 정확히 일치하는 후보 우선
        var pref = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        if (phrases != null)
            foreach (var p in phrases)
                if (!string.IsNullOrEmpty(p)) pref.Add(p.Trim());

        foreach (var r in resp.results)
        {
            if (r?.alternatives == null) continue;
            foreach (var alt in r.alternatives)
            {
                var t = (alt?.transcript ?? "").Trim();
                if (pref.Contains(t)) return t;
            }
        }

        // 2) confidence 최댓값(없으면 첫 후보)
        string best = "";
        float bestConf = -1f;
        foreach (var r in resp.results)
        {
            if (r?.alternatives == null) continue;
            foreach (var alt in r.alternatives)
            {
                if (alt == null) continue;
                float c = alt.confidence;
                string t = (alt.transcript ?? "").Trim();
                if (best == "" || c > bestConf) { best = t; bestConf = c; }
            }
        }
        return best;
    }

    // ====== 정규화/유틸 ======
    private string NormalizeShortKana(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        // 카타카나 → 히라가나
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (ch >= 0x30A1 && ch <= 0x30F6) sb.Append((char)(ch - 0x60));
            else sb.Append(ch);
        }
        s = sb.ToString();

        // 장음/변종 흡수 (ひい/ひー/火 → ひ)
        s = s.Replace("ひい", "ひ").Replace("ひー", "ひ").Replace("火", "ひ");

        // 공백/구두점/장음부호 제거
        s = s.Replace(" ", "").Replace("　", "").Replace("。", "").Replace("、", "").Replace("-", "").Replace("ー", "");

        return s.Trim();
    }

    private void LoadApiKeyFromResources()
    {
        try
        {
            TextAsset keyFile = Resources.Load<TextAsset>(apiKeyFileName);
            if (keyFile != null)
            {
                googleApiKey = keyFile.text.Trim();
                Debug.Log("API 키 로드 완료");
            }
            else
            {
                Debug.LogError("API 키 파일을 Resources에서 찾을 수 없습니다: " + apiKeyFileName);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("API 키 로드 실패: " + e);
        }
    }

    private void SafeMicEnd()
    {
        if (Microphone.IsRecording(null))
            Microphone.End(null);
    }

    private float[] ClipToMonoArray(AudioClip c)
    {
        if (c == null) return Array.Empty<float>();
        int len = c.samples * c.channels;
        var data = new float[len];
        c.GetData(data, 0);
        if (c.channels == 1) return data;

        int frames = c.samples;
        var mono = new float[frames];
        for (int i = 0; i < frames; i++)
        {
            double sum = 0;
            for (int ch = 0; ch < c.channels; ch++) sum += data[i * c.channels + ch];
            mono[i] = (float)(sum / c.channels);
        }
        return mono;
    }

    private float[] TrimSilence(float[] s, int sr, float thr, int win)
    {
        if (s == null || s.Length == 0) return Array.Empty<float>();
        int n = s.Length, L = 0, R = n - 1;

        // 왼쪽
        for (int i = 0; i < n; i += win)
        {
            if (Rms(s, i, Mathf.Min(win, n - i)) > thr) { L = Mathf.Max(0, i - win); break; }
        }
        // 오른쪽
        for (int i = n - win; i >= 0; i -= win)
        {
            if (Rms(s, i, Mathf.Min(win, n - i)) > thr) { R = Mathf.Min(n - 1, i + win); break; }
        }

        if (R <= L) return Array.Empty<float>();
        int len = R - L + 1;
        var cut = new float[len];
        Array.Copy(s, L, cut, 0, len);
        return cut;
    }

    private float Rms(float[] s, int off, int len)
    {
        double acc = 0;
        for (int i = 0; i < len; i++) { double v = s[off + i]; acc += v * v; }
        return (float)Math.Sqrt(acc / Math.Max(1, len));
    }

    private AudioClip ArrayToClip(float[] samples, int sr)
    {
        var c = AudioClip.Create("trimmed", samples.Length, 1, sr, false);
        c.SetData(samples, 0);
        return c;
    }

    private byte[] FloatToPCM16(float[] samples)
    {
        if (samples == null) return Array.Empty<byte>();
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);
        for (int i = 0; i < samples.Length; i++)
        {
            float f = Mathf.Clamp(samples[i], -1f, 1f);
            short s = (short)Mathf.RoundToInt(f * 32767f);
            bw.Write(s);
        }
        return ms.ToArray();
    }

    private byte[] BuildWavBytes(float[] samples, int sr)
    {
        // WAV 헤더 + PCM16
        byte[] pcm = FloatToPCM16(samples);
        const int headerSize = 44;
        int subchunk2Size = pcm.Length;
        int chunkSize = 36 + subchunk2Size;

        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(chunkSize);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));

            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);               // Subchunk1Size (PCM)
            bw.Write((ushort)1);        // AudioFormat = PCM
            bw.Write((ushort)1);        // NumChannels = 1
            bw.Write(sr);               // SampleRate
            bw.Write(sr * 2);           // ByteRate (sr * ch * bits/8)
            bw.Write((ushort)2);        // BlockAlign
            bw.Write((ushort)16);       // BitsPerSample

            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(subchunk2Size);
            bw.Write(pcm);

            return ms.ToArray();
        }
    }
}
