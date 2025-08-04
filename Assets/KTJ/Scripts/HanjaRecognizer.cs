using UnityEngine;

public class HanjaRecognizer : MonoBehaviour
{
    public HanjaDataBase hanjaDatabase;

    public void TryMatchHanja(string recognizedReading)
    {
        recognizedReading = recognizedReading.Trim();
        Debug.Log($"매칭용 텍스트: '{recognizedReading}'");

        foreach (var hanja in hanjaDatabase.allowedHanja)
        {
            if (hanja == null) continue;

            // 1) STT가 문자 그대로 한자를 내보냈을 때
            if (hanja.character == recognizedReading)
            {
                ShowHanja(hanja);
                return;
            }

            // 2) STT가 일본어 읽기를 내보냈을 때
            if (hanja.japaneseReading == recognizedReading)
            {
                ShowHanja(hanja);
                return;
            }
        }

        Debug.LogWarning($"매칭된 한자가 없습니다: \"{recognizedReading}\"");
    }

    private void ShowHanja(HanjaData hanja)
    {
        Debug.Log($"매칭된 한자: {hanja.character} ({hanja.japaneseReading})");
        
        if (hanja.prefabToSpawn != null)
        {
            // 원하는 위치/부모 설정
            Vector3 spawnPos = Vector3.zero;               
            Quaternion spawnRot = Quaternion.identity;  
            Transform parent    = null;            
            Instantiate(hanja.prefabToSpawn, spawnPos, spawnRot, parent);
        }
        else
        {
            Debug.LogWarning($"소환할 Prefab 미지정: {hanja.character}");
        }
        
    }

}
