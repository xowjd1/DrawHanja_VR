using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartScene : MonoBehaviour
{
    [Header("Button & Scene")]
    public Button startButton;          // 클릭할 버튼
    public string sceneToLoad = "Game"; // 전환할 씬 이름

    bool loading;

    void Awake()
    {
        if (startButton) startButton.onClick.AddListener(OnClickStart);
    }

    // 버튼 OnClick에 이 함수 연결해도 됨
    public void OnClickStart()
    {
        if (loading || string.IsNullOrEmpty(sceneToLoad)) return;
        loading = true;
        if (startButton) startButton.interactable = false;
        StartCoroutine(LoadSceneRoutine());
    }

    IEnumerator LoadSceneRoutine()
    {
        // 있으면 글로벌 볼륨 페이드아웃 사용(없으면 건너뜀)
        var fader = FindObjectOfType<VolumeFader>();
        if (fader) yield return fader.FadeOutRoutine(0.5f);

        yield return SceneManager.LoadSceneAsync(sceneToLoad);
    }
}