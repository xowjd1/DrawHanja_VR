using UnityEngine;

public class HanjaRecognizer : MonoBehaviour
{
    public HanjaDataBase hanjaDatabase;

    public void TryMatchHanja(string recognizedReading)
    {
        if (hanjaDatabase == null || hanjaDatabase.allowedHanja == null) return;
        if (string.IsNullOrWhiteSpace(recognizedReading)) return;

        string raw   = recognizedReading.Trim();
        string canon = JapaneseLexicon.NormalizeForMatching(raw); // ← 정규화(히/ヒ/hi/火 → ひ)
        Debug.Log($"매칭용 텍스트: raw='{raw}', canon='{canon}'");

        foreach (var hanja in hanjaDatabase.allowedHanja)
        {
            if (hanja == null) continue;

            // 1) 한자 그대로
            if (hanja.character == raw || hanja.character == canon)
            {
                ShowHanja(hanja);
                return;
            }

            // 2) 대표 읽기
            if (hanja.japaneseReading == raw || hanja.japaneseReading == canon)
            {
                ShowHanja(hanja);
                return;
            }
            
        }

        Debug.LogWarning($"매칭된 한자가 없습니다: \"{raw}\" / canon=\"{canon}\"");
    }

    private void ShowHanja(HanjaData hanja)
    {
        Debug.Log($"매칭된 한자: {hanja.character} ({hanja.japaneseReading})");

        if (hanja.prefabToSpawn != null)
        {
            Vector3   spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;
            Transform  parent   = null;
            Instantiate(hanja.prefabToSpawn, spawnPos, spawnRot, parent);
        }
        else
        {
            Debug.LogWarning($"소환할 Prefab 미지정: {hanja.character}");
        }
    }
}
