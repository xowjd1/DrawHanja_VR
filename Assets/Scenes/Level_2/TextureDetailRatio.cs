using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomDetailsOnMultipleTextures : MonoBehaviour
{
    [Header("Settings")]
    public Terrain targetTerrain;
    public List<int> targetTextureIndices = new List<int> { 1 };
    [Range(0, 16)] public int minDensity = 1;
    [Range(0, 16)] public int maxDensity = 4;
    [Range(0f, 1f)] public float textureThreshold = 0.5f;

    [Header("Detail 설정")]
    public List<int> allowedDetailIndices = new List<int> { 0, 1, 2 };
    [Tooltip("디테일 인덱스별 밀도 확률 (%)")]
    public List<float> densityWeights = new List<float> { 50f, 30f, 20f };

    private const bool flipY = true;

    [ContextMenu("🌿 Apply Details (좌표계/회전 보정 고정)")]
    public void ApplyDetails()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("❌ 타겟 Terrain이 지정되지 않았습니다.");
            return;
        }

        if (allowedDetailIndices.Count != densityWeights.Count || allowedDetailIndices.Count == 0)
        {
            Debug.LogError("❌ allowedDetailIndices와 densityWeights의 길이가 다릅니다.");
            return;
        }

        TerrainData data = targetTerrain.terrainData;
        int detailWidth = data.detailWidth;
        int detailHeight = data.detailHeight;
        int alphaWidth = data.alphamapWidth;
        int alphaHeight = data.alphamapHeight;
        int detailCount = data.detailPrototypes.Length;
        float[,,] splatmap = data.GetAlphamaps(0, 0, alphaWidth, alphaHeight);

        int[][,] newDetailLayers = new int[detailCount][,];
        for (int i = 0; i < detailCount; i++)
            newDetailLayers[i] = new int[detailWidth, detailHeight];

        for (int y = 0; y < detailHeight; y++)
        {
            for (int x = 0; x < detailWidth; x++)
            {
                int ty = flipY ? detailHeight - 1 - y : y;
                int tx = x;

                int rx = detailHeight - 1 - ty;
                int ry = tx;

                float normX = (rx + 0.5f) / detailWidth;
                float normY = (ry + 0.5f) / detailHeight;
                int mapX = Mathf.Clamp(Mathf.RoundToInt(normX * (alphaWidth - 1)), 0, alphaWidth - 1);
                int mapY = Mathf.Clamp(Mathf.RoundToInt(normY * (alphaHeight - 1)), 0, alphaHeight - 1);

                float totalWeight = 0f;
                foreach (int texIndex in targetTextureIndices)
                    totalWeight += splatmap[mapY, mapX, texIndex];

                if (totalWeight >= textureThreshold)
                {
                    int randomDetail = PickWeightedRandomDetail();
                    int density = Random.Range(minDensity, maxDensity + 1);
                    newDetailLayers[randomDetail][x, y] = density;
                }
                else
                {
                    for (int d = 0; d < detailCount; d++)
                        newDetailLayers[d][x, y] = 0;
                }
            }
        }

        for (int i = 0; i < detailCount; i++)
            data.SetDetailLayer(0, 0, i, newDetailLayers[i]);

        targetTerrain.Flush();
        Debug.Log("✅ 디테일이 고정된 좌표계/회전(세로 대칭+90도 반시계)으로 배치되었습니다!");
    }

    [ContextMenu("🎲 랜덤 퍼센트 생성")]
    public void GenerateRandomWeights()
    {
        densityWeights.Clear();
        int count = allowedDetailIndices.Count;
        if (count == 0) return;

        float[] raw = new float[count];
        float total = 0f;
        for (int i = 0; i < count; i++)
        {
            raw[i] = Random.Range(1f, 100f);
            total += raw[i];
        }

        for (int i = 0; i < count; i++)
        {
            densityWeights.Add((raw[i] / total) * 100f);
        }

        Debug.Log("🎲 랜덤 퍼센트 분배 완료: " + string.Join(", ", densityWeights));
    }

    private int PickWeightedRandomDetail()
    {
        float total = 0f;
        foreach (float w in densityWeights)
            total += w;

        float rand = Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < allowedDetailIndices.Count; i++)
        {
            cumulative += densityWeights[i];
            if (rand <= cumulative)
                return allowedDetailIndices[i];
        }

        return allowedDetailIndices[allowedDetailIndices.Count - 1]; // fallback
    }

    [ContextMenu("🧹 Clear All Details")]
    public void ClearAllDetails()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("❌ 타겟 Terrain이 지정되지 않았습니다.");
            return;
        }

        TerrainData data = targetTerrain.terrainData;
        int detailWidth = data.detailWidth;
        int detailHeight = data.detailHeight;
        int detailCount = data.detailPrototypes.Length;

        for (int i = 0; i < detailCount; i++)
        {
            int[,] emptyLayer = new int[detailWidth, detailHeight];
            data.SetDetailLayer(0, 0, i, emptyLayer);
        }

        targetTerrain.Flush();
        Debug.Log("🧹 Terrain 위 모든 디테일 오브젝트 초기화 완료!");
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (targetTerrain == null || targetTextureIndices.Count == 0)
            return;

        TerrainData data = targetTerrain.terrainData;
        int detailWidth = data.detailWidth;
        int detailHeight = data.detailHeight;
        int alphaWidth = data.alphamapWidth;
        int alphaHeight = data.alphamapHeight;
        float[,,] splatmap = data.GetAlphamaps(0, 0, alphaWidth, alphaHeight);

        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 scale = data.size;
        Vector3 cellSize = new Vector3(scale.x / detailWidth, 0.1f, scale.z / detailHeight);

        for (int y = 0; y < detailHeight; y++)
        {
            for (int x = 0; x < detailWidth; x++)
            {
                int ty = flipY ? detailHeight - 1 - y : y;
                int tx = x;

                int rx = detailHeight - 1 - ty;
                int ry = tx;

                float normX = (rx + 0.5f) / detailWidth;
                float normY = (ry + 0.5f) / detailHeight;
                int mapX = Mathf.Clamp(Mathf.RoundToInt(normX * (alphaWidth - 1)), 0, alphaWidth - 1);
                int mapY = Mathf.Clamp(Mathf.RoundToInt(normY * (alphaHeight - 1)), 0, alphaHeight - 1);

                float totalWeight = 0f;
                foreach (int texIndex in targetTextureIndices)
                    totalWeight += splatmap[mapY, mapX, texIndex];

                if (totalWeight >= textureThreshold)
                {
                    float height = data.GetInterpolatedHeight(normX, normY);
                    Vector3 worldPos = terrainPos + new Vector3(normX * scale.x, height, normY * scale.z);
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(worldPos + Vector3.up * 0.05f, cellSize * 0.5f);
                }
            }
        }
    }
#endif

}
