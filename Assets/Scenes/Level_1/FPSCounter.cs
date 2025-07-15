using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public float updateInterval = 0.5f; // FPS 갱신 주기 (초 단위)

    private float accum = 0f; // 누적 FPS
    private int frames = 0;   // 프레임 수
    private float timeLeft;   // 남은 시간
    private float fps;        // 계산된 FPS

    private GUIStyle textStyle;

    void Start()
    {
        timeLeft = updateInterval;
        textStyle = new GUIStyle();
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = Color.white;
    }

    void Update()
    {
        timeLeft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime; // Time.timeScale 적용된 FPS 누적
        frames++;

        if (timeLeft <= 0.0f)
        {
            fps = accum / frames; // 평균 FPS 계산
            timeLeft = updateInterval;
            accum = 0f;
            frames = 0;
        }
    }

    void OnGUI()
    {
        // 화면 좌측 상단에 FPS 표시 (소수점 둘째 자리까지)
        GUI.Label(new Rect(5, 5, 100, 25), fps.ToString("F2") + " FPS", textStyle);
    }
}
