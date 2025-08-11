using UnityEngine;

public class OniDamageReceiver : MonoBehaviour
{
    public OniStateMachine owner;
    private void Reset() { owner = GetComponentInParent<OniStateMachine>(); }

    private void OnTriggerEnter(Collider other)
    {
        var dealer = other.GetComponent<DamageDealer>();
        if (dealer == null || owner == null) return;

        owner.ApplyDamage(dealer.damage);

        if (dealer.destroyOnHit)
            Destroy(dealer.gameObject);
    }
}
