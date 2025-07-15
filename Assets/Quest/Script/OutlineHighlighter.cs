using UnityEngine;

public class OutlineHighlighter : MonoBehaviour
{
    public Material outlineMaterial;

    void Start()
    {
        outlineMaterial.SetFloat("_Scale", 0f);
    }

    public void OnMouseEnter()
    {
        outlineMaterial.SetFloat("_Scale", 0.02f);
    }

    public void OnMouseExit()
    {
        outlineMaterial.SetFloat("_Scale", 0f);
    }

    void SetOutlineVisible(bool visible)
    {
        float targetScale = visible ? 1.05f : 0f;
        outlineMaterial.SetFloat("_Scale", targetScale);
    }
}