using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogUI : MonoBehaviour
{
    public static DialogUI Instance;

    public GameObject dialogPanel;
    public TextMeshProUGUI dialogText; 
    public Button nextButton;

    private string[] lines;
    private int index = 0;
    
    private System.Action onDialogComplete;

    void Awake()
    {
        Instance = this;
        dialogPanel.SetActive(false);
        nextButton.onClick.AddListener(NextLine);
    }

    public void StartDialog(string[] dialogLines, System.Action onComplete = null)
    {
        lines = dialogLines;
        index = 0;
        onDialogComplete = onComplete;
        
        dialogPanel.SetActive(true);
        dialogText.text = lines[index];
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void NextLine()
    {
        index++;
        if (index >= lines.Length)
        {
            dialogPanel.SetActive(false);
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            onDialogComplete?.Invoke();
            onDialogComplete = null;
        }
        else
        {
            dialogText.text = lines[index];
        }
    }
}
