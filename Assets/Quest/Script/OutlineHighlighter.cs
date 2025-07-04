using UnityEngine;

public class OutlineHighlighter : MonoBehaviour
{
    public Material outlineMaterial;

    void Start()
    {
        outlineMaterial.SetFloat("_Scale", 0f);
    }

    void OnMouseEnter()
    {
        outlineMaterial.SetFloat("_Scale", 1.05f);
    }

    void OnMouseExit()
    {
        outlineMaterial.SetFloat("_Scale", 0f);
    }

    void SetOutlineVisible(bool visible)
    {
        float targetScale = visible ? 1.05f : 0f;
        outlineMaterial.SetFloat("_Scale", targetScale);
    }
}