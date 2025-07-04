using UnityEngine;

[CreateAssetMenu(fileName = "HanjaDatabase", menuName = "Hanja/Database")]
public class HanjaData : ScriptableObject
{
    [Header("한자 문자")]
    public string character;
}
