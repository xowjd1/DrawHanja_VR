using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class Pickup : MonoBehaviour, IInteractable
{
    public GameObject axeInHand;

    public void Interact()
    {
        axeInHand.SetActive(true);
        Destroy(gameObject);
    }
}
