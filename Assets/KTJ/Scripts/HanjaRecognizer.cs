using UnityEngine;

public class HanjaRecognizer : MonoBehaviour
{
    public HanjaDataBase hanjaDatabase;

    public void TryMatchHanja(string recognizedText)
    {
        foreach (var hanja in hanjaDatabase.allowedHanja)
        {
            if (hanja == null) continue;
            if (hanja.japaneseReading == recognizedText)  // 예: "いぬ"
            {
                Debug.Log($"매칭된 한자: {hanja.character}");
                // 한자 UI 표시 등
                return;
            }
        }
        Debug.Log("매칭된 한자가 없음");
    }
}
