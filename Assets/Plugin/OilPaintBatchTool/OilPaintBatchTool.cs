using UnityEditor;
using UnityEngine;
using System.IO;

public class OilPaintBatchTool : EditorWindow
{
    Texture2D[] textures;
    int radius = 2;

    [MenuItem("Tools/Oil Paint Batch Processor")]
    static void ShowWindow() => GetWindow<OilPaintBatchTool>("Oil Paint Processor");

    void OnGUI()
    {
        SerializedObject so = new SerializedObject(this);
        SerializedProperty texProp = so.FindProperty("textures");
        EditorGUILayout.PropertyField(texProp, true);
        radius = EditorGUILayout.IntSlider("Radius", radius, 1, 10);

        if (GUILayout.Button("Process and Save"))
        {
            foreach (var tex in textures)
            {
                if (tex == null) continue;
                var result = ProcessOilPaint(tex, radius);
                SaveTexture(result, tex.name + "_oilpaint.png");
            }
        }

        so.ApplyModifiedProperties();
    }

    Texture2D ProcessOilPaint(Texture2D source, int radius)
    {
        int w = source.width;
        int h = source.height;
        Texture2D result = new Texture2D(w, h);
        Color[] pixels = source.GetPixels();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color[] quadMean = new Color[4];
                float[] quadVariance = new float[4];

                for (int q = 0; q < 4; q++)
                {
                    Vector2Int start = q switch {
                        0 => new Vector2Int(-radius, -radius),
                        1 => new Vector2Int(-radius, 0),
                        2 => new Vector2Int(0, -radius),
                        _ => new Vector2Int(0, 0)
                    };

                    int count = 0;
                    Vector3 sum = Vector3.zero;
                    Vector3 sqSum = Vector3.zero;

                    for (int dy = 0; dy <= radius; dy++)
                    {
                        for (int dx = 0; dx <= radius; dx++)
                        {
                            int sx = Mathf.Clamp(x + start.x + dx, 0, w - 1);
                            int sy = Mathf.Clamp(y + start.y + dy, 0, h - 1);
                            Color col = pixels[sx + sy * w];
                            Vector3 c = new Vector3(col.r, col.g, col.b);
                            sum += c;
                            sqSum += Vector3.Scale(c, c);
                            count++;
                        }
                    }

                    Vector3 mean = sum / count;
                    Vector3 variance = (sqSum / count) - Vector3.Scale(mean, mean);
                    quadMean[q] = new Color(mean.x, mean.y, mean.z);
                    quadVariance[q] = variance.x + variance.y + variance.z;
                }

                int minIdx = 0;
                float minVar = quadVariance[0];
                for (int i = 1; i < 4; i++)
                {
                    if (quadVariance[i] < minVar)
                    {
                        minVar = quadVariance[i];
                        minIdx = i;
                    }
                }

                result.SetPixel(x, y, quadMean[minIdx]);
            }
        }

        result.Apply();
        return result;
    }

    void SaveTexture(Texture2D tex, string filename)
    {
        string path = EditorUtility.SaveFilePanel("Save Texture", "Assets", filename, "png");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.Refresh();
        }
    }
}
