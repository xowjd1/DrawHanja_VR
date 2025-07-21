using UnityEngine;
using UnityEngine.UI;

public class HanjaNotebookUI : MonoBehaviour
{
    public Button openNotebookButton; 
    public GameObject notebookPanel;
    public Button xButton;
    
    
    void Awake()
    {
        openNotebookButton.onClick.AddListener(OpenNotebook);
        xButton.onClick.AddListener(CloseNotebook);
    }

    void OpenNotebook()
    {
        notebookPanel.SetActive(true);
    }

    void CloseNotebook()
    {
        notebookPanel.SetActive(false);
    }
}
