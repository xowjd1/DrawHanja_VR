using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PrefabSpawnerOnMultipleTextures : MonoBehaviour
{
    [Header("Terrain 설정")]
    public Terrain targetTerrain;
    public List<int> targetTextureIndices = new List<int> { 1 };
    [Range(0, 16)] public int minDensity = 1;
    [Range(0, 16)] public int maxDensity = 4;
    [Range(0f, 1f)] public float textureThreshold = 0.5f;
    public Transform prefabParent;

    [Header("프리팹 설정")]
    public List<GameObject> prefabs = new List<GameObject>();
    public List<int> prefabIndices = new List<int> { 0, 1, 2 };
    public List<float> densityWeights = new List<float> { 50f, 30f, 20f };

    [Header("랜덤 스케일")]
    [Range(0.1f, 5f)] public float minScale = 0.8f;
    [Range(0.1f, 5f)] public float maxScale = 1.2f;

    [Header("프리팹 간 최소 거리")]
    public float samePrefabMinDistance = 3f;

    private Dictionary<int, List<Vector3>> spawnedPositions = new Dictionary<int, List<Vector3>>();

    private const bool flipY = true;

    [ContextMenu("🌱 Spawn Prefabs")]
    public void SpawnPrefabs()
    {
        if (targetTerrain == null || prefabs.Count == 0)
        {
            Debug.LogError("❌ Terrain 또는 Prefab이 지정되지 않았습니다.");
            return;
        }

        if (prefabIndices.Count != densityWeights.Count || prefabIndices.Count == 0)
        {
            Debug.LogError("❌ prefabIndices와 densityWeights의 길이가 다릅니다.");
            return;
        }

        ClearSpawnedPrefabs();
        spawnedPositions.Clear();

        TerrainData data = targetTerrain.terrainData;
        int detailWidth = data.detailWidth;
        int detailHeight = data.detailHeight;
        int alphaWidth = data.alphamapWidth;
        int alphaHeight = data.alphamapHeight;
        float[,,] splatmap = data.GetAlphamaps(0, 0, alphaWidth, alphaHeight);

        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 size = data.size;

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
                    int density = Random.Range(minDensity, maxDensity + 1);
                    for (int i = 0; i < density; i++)
                    {
                        int prefabIndex = PickWeightedRandomPrefab();
                        if (prefabIndex < 0 || prefabIndex >= prefabs.Count) continue;

                        GameObject prefab = prefabs[prefabIndex];
                        if (prefab == null) continue;

                        Vector3 pos = new Vector3(normX * size.x, 0f, normY * size.z);
                        float height = data.GetInterpolatedHeight(normX, normY);
                        pos.y = height;

                        Vector3 worldPos = terrainPos + pos + Random.insideUnitSphere * 0.5f;
                        worldPos.y = height;

                        // 최소 거리 검사
                        if (!CanPlacePrefab(prefabIndex, worldPos, samePrefabMinDistance))
                            continue;

                        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        float randomScale = Random.Range(minScale, maxScale);

                        GameObject instance;
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        else
#endif
                            instance = Instantiate(prefab);

                        instance.transform.position = worldPos;
                        instance.transform.rotation = rot;
                        instance.transform.localScale = Vector3.one * randomScale;

                        if (prefabParent != null)
                            instance.transform.parent = prefabParent;

                        // 기록
                        if (!spawnedPositions.ContainsKey(prefabIndex))
                            spawnedPositions[prefabIndex] = new List<Vector3>();
                        spawnedPositions[prefabIndex].Add(worldPos);
                    }
                }
            }
        }

        Debug.Log("✅ 프리팹 배치 완료");
    }

    private bool CanPlacePrefab(int prefabIndex, Vector3 pos, float minDist)
    {
        if (!spawnedPositions.ContainsKey(prefabIndex))
            return true;

        foreach (var existingPos in spawnedPositions[prefabIndex])
        {
            if (Vector3.Distance(existingPos, pos) < minDist)
                return false;
        }
        return true;
    }

    [ContextMenu("🧹 Clear Spawned Prefabs")]
    public void ClearSpawnedPrefabs()
    {
        if (prefabParent != null)
        {
#if UNITY_EDITOR
            while (prefabParent.childCount > 0)
                DestroyImmediate(prefabParent.GetChild(0).gameObject);
#else
            foreach (Transform child in prefabParent)
                Destroy(child.gameObject);
#endif
        }
        else
        {
#if UNITY_EDITOR
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
#endif
            foreach (var obj in allObjects)
            {
#if UNITY_EDITOR
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(obj);
#else
                GameObject source = null;
#endif
                if (source != null && prefabs.Contains(source))
                {
#if UNITY_EDITOR
                    DestroyImmediate(obj);
#else
                    Destroy(obj);
#endif
                }
            }
        }

        Debug.Log("🧹 프리팹 제거 완료");
    }

    private int PickWeightedRandomPrefab()
    {
        float total = 0f;
        foreach (float w in densityWeights)
            total += w;

        float rand = Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < prefabIndices.Count; i++)
        {
            cumulative += densityWeights[i];
            if (rand <= cumulative)
                return prefabIndices[i];
        }

        return prefabIndices[prefabIndices.Count - 1];
    }

    [ContextMenu("🎲 랜덤 퍼센트 생성")]
    public void GenerateRandomWeights()
    {
        densityWeights.Clear();
        int count = prefabIndices.Count;
        if (count == 0) return;

        float[] raw = new float[count];
        float total = 0f;
        for (int i = 0; i < count; i++)
        {
            raw[i] = Random.Range(1f, 100f);
            total += raw[i];
        }

        for (int i = 0; i < count; i++)
            densityWeights.Add((raw[i] / total) * 100f);

        Debug.Log("🎲 랜덤 퍼센트 분배 완료: " + string.Join(", ", densityWeights));
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
