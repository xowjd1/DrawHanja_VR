using UnityEngine;

public class Saber : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private AudioParticle audioParticle;

    private void Awake()
    {
        audioParticle.Visual += audioParticle.PlayLeftVibration;
    }








}
