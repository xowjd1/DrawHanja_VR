#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ParticleHueAdjusterEditor : EditorWindow
{
    private GameObject targetObject;
    private float hueShift = 0f;
    private float saturation = 1f;
    private float emissionMultiplier = 1f;

    [MenuItem("Tools/Particle Hue Adjuster")]
    public static void ShowWindow()
    {
        GetWindow<ParticleHueAdjusterEditor>("Particle Hue Adjuster");
    }

    private void OnGUI()
    {
        GUILayout.Label("Particle Hue Adjuster", EditorStyles.boldLabel);

        targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);
        hueShift = EditorGUILayout.Slider("Hue Shift", hueShift, 0f, 1f);
        saturation = EditorGUILayout.Slider("Saturation", saturation, 0f, 1f);
        emissionMultiplier = EditorGUILayout.FloatField("Emission Multiplier", emissionMultiplier);

        if (GUILayout.Button("Apply Color Adjustments"))
        {
            if (targetObject == null)
            {
                Debug.LogWarning("No target object selected.");
                return;
            }

            ApplyColorAdjustments();
        }
    }

    private void ApplyColorAdjustments()
    {
        var psList = targetObject.GetComponentsInChildren<ParticleSystem>(true);

        foreach (var ps in psList)
        {
            var main = ps.main;
            var colorOverLifetime = ps.colorOverLifetime;

            // 메인 색상 조정
            if (main.startColor.mode == ParticleSystemGradientMode.Color)
            {
                Color original = ((ParticleSystem.MinMaxGradient)main.startColor).color;
                main.startColor = AdjustColor(original);
            }

            // 색상 오버 라이프타임 조정
            if (colorOverLifetime.enabled)
            {
                ParticleSystem.MinMaxGradient gradient = colorOverLifetime.color;
                if (gradient.mode == ParticleSystemGradientMode.Gradient)
                {
                    Gradient old = gradient.gradient;
                    Gradient newGrad = new Gradient();

                    GradientColorKey[] newKeys = new GradientColorKey[old.colorKeys.Length];
                    for (int i = 0; i < old.colorKeys.Length; i++)
                    {
                        newKeys[i].time = old.colorKeys[i].time;
                        newKeys[i].color = AdjustColor(old.colorKeys[i].color);
                    }

                    newGrad.SetKeys(newKeys, old.alphaKeys);
                    colorOverLifetime.color = new ParticleSystem.MinMaxGradient(newGrad);
                }
            }

            // 머티리얼이 있다면 이미션 조정
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Color baseColor = renderer.sharedMaterial.GetColor("_Color");
                Color emissionColor = AdjustColor(baseColor) * emissionMultiplier;
                renderer.sharedMaterial.SetColor("_EmissionColor", emissionColor);
            }
        }

        Debug.Log("Color adjustments applied to all ParticleSystems in: " + targetObject.name);
    }

    private Color AdjustColor(Color color)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);

        h = (h + hueShift) % 1f;
        s = saturation;
        // v = value;  // 밝기 조정 부분 삭제

        // 기존 밝기 유지하여 반환
        return Color.HSVToRGB(h, s, v);
    }
}
#endif