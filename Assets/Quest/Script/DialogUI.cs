using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogUI : MonoBehaviour
{
    public static DialogUI Instance;

    public GameObject dialogPanel;
    public TextMeshProUGUI dialogText; 
    public Button nextButton;
    public Button choiceButton;
    public TextMeshProUGUI choiceText;

    private string[] lines;
    private int index = 0;
    
    private System.Action onDialogComplete;

    void Awake()
    {
        Instance = this;
        dialogPanel.SetActive(false);
        nextButton.onClick.AddListener(NextLine);
        choiceButton.onClick.AddListener(NextLine);
    }

    public void StartDialog(string[] dialogLines, System.Action onComplete = null)
    {
        lines = dialogLines;
        index = 0;
        onDialogComplete = onComplete;
        
        dialogPanel.SetActive(true);
        ShowCurrentLine();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void NextLine()
    {
        index++;
        
        if (index < lines.Length && lines[index].StartsWith("P:"))
        {
            index++;
        }
        
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
            ShowCurrentLine(); 
        }
    }
    
    void ShowCurrentLine()
    {
        string line = lines[index];
        string nextLine = (index + 1 < lines.Length) ? lines[index + 1] : null;

        if (nextLine != null && nextLine.StartsWith("P:"))
        {
            dialogText.text = line;
            choiceText.text = nextLine.Substring(2).Trim();

            nextButton.gameObject.SetActive(false);
            choiceButton.gameObject.SetActive(true);
        }
        else
        {
            nextButton.gameObject.SetActive(true);
            choiceButton.gameObject.SetActive(false);
            dialogText.text = line;
        }
    }
}
