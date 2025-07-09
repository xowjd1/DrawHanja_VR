using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomDetailsOnMultipleTextures : MonoBehaviour
{
    [Header("Settings")]
    public Terrain targetTerrain;

    [Tooltip("디테일을 뿌릴 대상 텍스처 인덱스들 (Terrain Layers 순서)")]
    public List<int> targetTextureIndices = new List<int>();  // 다중 인덱스

    [Range(0, 16)] public int maxDensity = 4;
    [Range(0f, 1f)] public float textureThreshold = 0.5f;

    [ContextMenu("🌿 Apply Random Details to Multiple Textures & Clear Others")]
    public void ApplyDetails()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("❌ 타겟 Terrain이 지정되지 않았습니다.");
            return;
        }

        if (targetTextureIndices.Count == 0)
        {
            Debug.LogWarning("⚠️ 최소 하나 이상의 텍스처 인덱스를 지정하세요.");
            return;
        }

        TerrainData data = targetTerrain.terrainData;

        int detailWidth = data.detailWidth;
        int detailHeight = data.detailHeight;
        int alphaWidth = data.alphamapWidth;
        int alphaHeight = data.alphamapHeight;
        int detailCount = data.detailPrototypes.Length;
        int textureLayerCount = data.alphamapLayers;

        float[,,] splatmap = data.GetAlphamaps(0, 0, alphaWidth, alphaHeight);

        // 초기화할 디테일 배열 준비
        int[][,] newDetailLayers = new int[detailCount][,];
        for (int i = 0; i < detailCount; i++)
            newDetailLayers[i] = new int[detailWidth, detailHeight];

        for (int y = 0; y < detailHeight; y++)
        {
            for (int x = 0; x < detailWidth; x++)
            {
                int mapX = Mathf.FloorToInt((float)x / detailWidth * alphaWidth);
                int mapY = Mathf.FloorToInt((float)y / detailHeight * alphaHeight);

                bool matchesAny = false;
                foreach (int texIndex in targetTextureIndices)
                {
                    if (texIndex >= 0 && texIndex < textureLayerCount)
                    {
                        float weight = splatmap[mapY, mapX, texIndex];
                        if (weight >= textureThreshold)
                        {
                            matchesAny = true;
                            break;
                        }
                    }
                }

                if (matchesAny)
                {
                    int randomDetail = Random.Range(0, detailCount);
                    int density = Random.Range(1, maxDensity + 1);
                    newDetailLayers[randomDetail][x, y] = density;
                }
                // else: 아무 것도 안 넣음 (초기화 상태 유지)
            }
        }

        // 디테일 레이어 반영
        for (int i = 0; i < detailCount; i++)
            data.SetDetailLayer(0, 0, i, newDetailLayers[i]);

        Debug.Log($"✅ 텍스처 인덱스 {string.Join(", ", targetTextureIndices)} 위에만 디테일 랜덤 배치 완료!");
    }
}