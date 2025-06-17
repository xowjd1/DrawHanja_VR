using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Text talkText;
    public GameObject scanObject;

    public void Action(GameObject target)
    {
        scanObject = target;
        talkText.text = "이것의 이름은" + scanObject.name;
    }
}
