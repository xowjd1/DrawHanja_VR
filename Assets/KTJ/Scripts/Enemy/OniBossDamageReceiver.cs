using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OniBossDamageReceiver : MonoBehaviour
{
    public OniBossStateMachine owner;

    [Header("Optional Filter")]
    public LayerMask acceptLayers = ~0;      // 전부 허용
    public bool useLayerFilter = false;      // 필요하면 켠다

    void Reset()
    {
        // 자동 세팅
        owner = GetComponentInParent<OniBossStateMachine>();
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        TryHit(other.gameObject);
    }

    void OnCollisionEnter(Collision c)
    {
        TryHit(c.collider.gameObject);
    }

    void TryHit(GameObject hitter)
    {
        if (owner == null) return;

        // 레이어 필터(선택)
        if (useLayerFilter && ((1 << hitter.layer) & acceptLayers) == 0) return;

        var dealer = hitter.GetComponent<DamageDealer>();
        if (dealer == null) return;

        Debug.Log($"[Boss Hit] {owner.name} <- {hitter.name} dmg={dealer.damage}");
        owner.ApplyDamage(dealer.damage);

        if (dealer.destroyOnHit)
            Destroy(hitter.gameObject);
    }
}