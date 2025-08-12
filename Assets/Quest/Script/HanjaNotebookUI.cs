using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HanjaNotebookUI : MonoBehaviour
{
    public Button openNotebookButton; 
    public GameObject notebookPanel;
    public Button xButton;
    
    public string allowedSceneName = "Forest";
    
    void Awake()
    {
        if (SceneManager.GetActiveScene().name != allowedSceneName)
        {
            gameObject.SetActive(false);
            return;
        }
        
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
