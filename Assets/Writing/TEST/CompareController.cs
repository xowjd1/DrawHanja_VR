using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CompareController : MonoBehaviour
{
    public TransparentUIDrawing drawing;   // 획만 담긴 투명 텍스처
    public Texture2D            referenceTex;
    public Button               completeButton;
    public TMP_Text                 resultText;
    [Range(0f,1f)] public float acceptableError = 0.1f;

    void Start()
    {
        completeButton.onClick.AddListener(OnCompleteClicked);
    }

    void OnCompleteClicked()
    {
        var userTex = drawing.GetStrokeTexture();
        var up = userTex.GetPixels();
        var rp = referenceTex.GetPixels();
        int len = up.Length;

        //––– 1) 레퍼런스 임계값으로 “shape 영역” 뽑기 –––––––––––––––––––––––––––––––––––
        float refThreshold = 0.9f;  
        // (배경 흰색=1, 진한 회색 ‘永’은 ~0.7이므로 0.9 이하를 shape로 간주)
        
        //––– 2) 비교할 픽셀 인덱스(ROI) 모으기 –––––––––––––––––––––––––––––––––––––––––
        var roi = new List<int>(len);
        for (int i = 0; i < len; i++)
        {
            bool u = up[i].a > 0.5f;            // 획을 그린 픽셀?
            bool r = rp[i].grayscale < refThreshold;  // 레퍼런스 shape 영역?
            if (u || r) roi.Add(i);
        }

        //––– 3) ROI 안에서 불일치 카운트 –––––––––––––––––––––––––––––––––––––––––––––––
        int mismatches = 0;
        foreach (int i in roi)
        {
            bool u = up[i].a > 0.5f;
            bool r = rp[i].grayscale < refThreshold;
            if (u != r) mismatches++;
        }

        //––– 4) 에러율 = 불일치 / ROI 크기 –––––––––––––––––––––––––––––––––––––––––––––
        float errorRate = roi.Count > 0 ? (float)mismatches / roi.Count : 1f;

        //––– 5) 결과 표시 ––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
        if (errorRate <= acceptableError)
            resultText.text = $"GOOD! (오차율: {errorRate:F2})";
        else
            resultText.text = $"TRY AGAIN (오차율: {errorRate:F2})";
    }
}