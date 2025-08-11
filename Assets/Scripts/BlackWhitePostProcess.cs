using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlackWhitePostProcess : MonoBehaviour
{
    private Volume volume;
    private ColorAdjustments colorAdjustments;
    private bool isBlackAndWhite = false;

    void Awake()
    {
        volume = GetComponent<Volume>();
        if (volume == null)
        {
            Debug.LogError("[BlackWhitePostProcess] 이 스크립트는 Volume이 있는 GameObject에 붙어야 합니다.");
            return;
        }

        if (!volume.profile.TryGet(out colorAdjustments))
        {
            Debug.LogError("[BlackWhitePostProcess] Volume 프로필에 ColorAdjustments가 없습니다.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isBlackAndWhite = !isBlackAndWhite;
            SetBlackAndWhite(isBlackAndWhite);
        }
    }

    private void SetBlackAndWhite(bool enable)
    {
        if (colorAdjustments == null) return;

        if (enable)
        {
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = -100f;
        }
        else
        {
            colorAdjustments.saturation.overrideState = false;
            colorAdjustments.saturation.value = 0f;
        }
    }
}
