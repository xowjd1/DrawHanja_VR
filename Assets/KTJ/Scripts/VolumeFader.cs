using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(Volume))]
public class VolumeFader : MonoBehaviour
{
    [Header("Fade Settings")]
    public bool useUnscaledTime = true;
    public float defaultDuration = 0.8f;
    [Tooltip("완전히 어두워질 때의 색(보통 Black)")]
    public Color fadeColor = Color.black;

    Volume _volume;
    VolumeProfile _profile;
    ColorAdjustments _colorAdj;

    float _t;                // 0=원래 화면, 1=완전 암전
    Coroutine _co;

    void Awake()
    {
        _volume = GetComponent<Volume>();
        if (_volume.profile == null)
            _volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        _profile = _volume.profile;

        if (!_profile.TryGet(out _colorAdj))
            _colorAdj = _profile.Add<ColorAdjustments>(true);

        _colorAdj.colorFilter.overrideState = true;
        SetImmediate(0f); // 시작은 정상 화면
    }

    // ===== 외부에서 호출 =====
    public void FadeOut(float duration = -1f)  => StartFade(1f, duration);
    public void FadeIn(float duration = -1f)   => StartFade(0f, duration);

    public IEnumerator FadeOutRoutine(float duration = -1f) => FadeTo(1f, duration);
    public IEnumerator FadeInRoutine(float duration = -1f)  => FadeTo(0f, duration);

    public IEnumerator FadeOutIn(float outDur, float hold, float inDur)
    {
        yield return FadeTo(1f, outDur);
        if (hold > 0f) yield return Wait(hold);
        yield return FadeTo(0f, inDur);
    }

    // 즉시 세팅 (0~1)
    public void SetImmediate(float t)
    {
        _t = Mathf.Clamp01(t);
        if (_colorAdj != null)
            _colorAdj.colorFilter.value = Color.Lerp(Color.white, fadeColor, _t);
    }

    // ===== 내부 =====
    void StartFade(float target, float duration)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(FadeTo(target, duration));
    }

    IEnumerator FadeTo(float target, float duration)
    {
        if (duration <= 0f) duration = defaultDuration;

        float start = _t;
        float time  = 0f;

        while (time < duration)
        {
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _t = Mathf.Lerp(start, target, time / duration);
            if (_colorAdj != null)
                _colorAdj.colorFilter.value = Color.Lerp(Color.white, fadeColor, _t);
            yield return null;
        }
        SetImmediate(target);
        _co = null;
    }

    IEnumerator Wait(float sec)
    {
        float t = 0f;
        while (t < sec)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }
}
