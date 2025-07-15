using System;
using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    public GameObject worldSpaceUI; // UI 프리팹 또는 자식 Canvas

    private void Update()
    {
        worldSpaceUI.transform.LookAt(Camera.main.transform);

        // 회전 보정: 정면이 아닌 뒷면이 보이면 뒤집기
        worldSpaceUI.transform.rotation = Quaternion.Euler(0,
            worldSpaceUI.transform.eulerAngles.y + 180, 0);
    }

    public void ShowUI()
    {
        if (worldSpaceUI != null)
            worldSpaceUI.SetActive(true);
        
       
    }

    public void HideUI()
    {
        if (worldSpaceUI != null)
            worldSpaceUI.SetActive(false);
    }
}