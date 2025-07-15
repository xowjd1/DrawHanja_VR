// using UnityEngine;
// using System.Collections.Generic;

// #if UNITY_EDITOR
// using UnityEditor;
// #endif

// [System.Serializable]
// public class TextureDetailRatio
// {
//     [Tooltip("Terrain Layers의 텍스처 인덱스")]
//     public int textureIndex;

//     [Tooltip("해당 텍스처 영역에서 디테일(풀)을 뿌릴 확률 (0~1)")]
//     [Range(0f, 1f)]
//     public float detailSpawnRatio = 1f;
// }

// public class RandomDetailsOnMultipleTextures : MonoBehaviour
// {
//     [Header("Settings")]
//     public Terrain targetTerrain;

//     [Tooltip("각 텍스처별 디테일(풀) 뿌릴 확률 설정")]
//     public List<TextureDetailRatio> textureDetailRatios = new List<TextureDetailRatio>();

//     [Tooltip("최대 디테일 밀도 (1~16)")]
//     [Range(1, 16)]
//     public int maxDensity = 4;

//     [Tooltip("텍스처 스플랫맵 가중치 임계값 (0~1)")]
//     [Range(0f, 1f)]
//     public float textureThreshold = 0.5f;

//     [ContextMenu("🌿 Apply Random Details to Multiple Textures & Clear Others")]
//     public void ApplyDetails()
//     {
//         if (targetTerrain == null)
//         {
//             Debug.LogError("❌ 타겟 Terrain이 지정되지 않았습니다.");
//             return;
//         }

//         if (textureDetailRatios.Count == 0)
//         {
//             Debug.LogWarning("⚠️ 최소 하나 이상의 텍스처별 디테일 확률 설정이 필요합니다.");
//             return;
//         }

//         TerrainData data = targetTerrain.terrainData;

//         int detailWidth = data.detailWidth;
//         int detailHeight = data.detailHeight;
//         int alphaWidth = data.alphamapWidth;
//         int alphaHeight = data.alphamapHeight;
//         int detailCount = data.detailPrototypes.Length;
//         int textureLayerCount = data.alphamapLayers;

//         // 스플랫맵 읽기
//         float[,,] splatmap = data.GetAlphamaps(0, 0, alphaWidth, alphaHeight);

//         // 새로운 디테일 배열 초기화
//         int[][,] newDetailLayers = new int[detailCount][,];
//         for (int i = 0; i < detailCount; i++)
//             newDetailLayers[i] = new int[detailWidth, detailHeight];

//         for (int y = 0; y < detailHeight; y++)
//         {
//             for (int x = 0; x < detailWidth; x++)
//             {
//                 // 스플랫맵 좌표 계산
//                 int mapX = Mathf.FloorToInt((float)x / detailWidth * alphaWidth);
//                 int mapY = Mathf.FloorToInt((float)y / detailHeight * alphaHeight);

//                 foreach (var tr in textureDetailRatios)
//                 {
//                     if (tr.textureIndex < 0 || tr.textureIndex >= textureLayerCount) continue;

//                     float weight = splatmap[mapY, mapX, tr.textureIndex];
//                     if (weight >= textureThreshold)
//                     {
//                         if (Random.value <= tr.detailSpawnRatio)
//                         {
//                             // 랜덤 디테일 종류 선택
//                             int randomDetail = Random.Range(0, detailCount);
//                             // 랜덤 밀도 (1~maxDensity)
//                             int density = Random.Range(1, maxDensity + 1);
//                             newDetailLayers[randomDetail][x, y] = density;
//                         }
//                         break; // 한 타일에 여러 텍스처 중복 방지
//                     }
//                 }
//             }
//         }

//         // 디테일 레이어 적용
//         for (int i = 0; i < detailCount; i++)
//             data.SetDetailLayer(0, 0, i, newDetailLayers[i]);

//         Debug.Log($"✅ 텍스처별 디테일 뿌릴 확률 적용 완료!");
//     }
// }
