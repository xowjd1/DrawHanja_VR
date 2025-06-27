using UnityEditor;
using UnityEngine;
using System.IO;

public class OilPaintGPUExportTool : EditorWindow
{
    int radius = 2;
    int samplingQuality = 1; // 0: Low (1 sample), 1: Medium (4 samples), 2: High (9 samples)
    Material oilPaintMaterial;

    [MenuItem("Tools/Oil Paint/GPU Export with Filter Fix")]
    static void ShowWindow() => GetWindow<OilPaintGPUExportTool>("Oil Paint GPU Export");

    void OnEnable()
    {
        Shader shader = Shader.Find("Hidden/OilPaintEffect");
        if (shader != null)
            oilPaintMaterial = new Material(shader);
    }

    void OnGUI()
    {
        if (oilPaintMaterial == null)
        {
            EditorGUILayout.HelpBox("OilPaintEffect shader not found!", MessageType.Error);
            return;
        }

        radius = EditorGUILayout.IntSlider("Radius", radius, 1, 10);
        samplingQuality = EditorGUILayout.IntPopup("Sampling Quality", samplingQuality, 
            new[] { "Low", "Medium", "High" }, new[] { 0, 1, 2 });

        if (GUILayout.Button("Process with GPU and Save"))
        {
            string saveFolder = "Assets/OilPaintedTextures";
            if (!AssetDatabase.IsValidFolder(saveFolder))
                AssetDatabase.CreateFolder("Assets", "OilPaintedTextures");

            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D tex)
                {
                    string path = AssetDatabase.GetAssetPath(tex);
                    Texture2D result = RunOilPaintShader(tex, radius, samplingQuality);
                    string savePath = Path.Combine(saveFolder, tex.name + "_oilpaint.png");

                    File.WriteAllBytes(savePath, result.EncodeToPNG());
                    Debug.Log("Saved: " + savePath);
                }
            }

            AssetDatabase.Refresh();
        }
    }

    Texture2D RunOilPaintShader(Texture2D source, int radius, int quality)
    {
        int w = 4096;
        int h = 4096;

        RenderTexture rtSource = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        rtSource.useMipMap = true;
        rtSource.autoGenerateMips = true;
        rtSource.filterMode = FilterMode.Bilinear;
        rtSource.Create();
        Graphics.Blit(source, rtSource);

        oilPaintMaterial.SetFloat("_Radius", radius);
        oilPaintMaterial.SetVector("_TexelSize", new Vector2(1.0f / w, 1.0f / h));
        oilPaintMaterial.SetFloat("_SubSampleLevel", quality); // NEW

        RenderTexture rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Bilinear;
        rt.Create();

        Graphics.Blit(rtSource, rt, oilPaintMaterial);

        RenderTexture.active = rt;
        Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        rt.Release();
        rtSource.Release();

        return result;
    }
}
