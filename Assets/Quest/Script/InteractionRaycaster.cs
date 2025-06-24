using UnityEngine;

public class InteractionRaycaster : MonoBehaviour
{
    public Camera mainCamera;
    public LayerMask interactableLayer;
    public float rayLength = 5f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, rayLength, interactableLayer))
            {
                var interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact();
                }
            }
        }
    }
}
