using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OniDamageReceiver : MonoBehaviour
{
    public OniStateMachine owner;

    [Header("Optional Filter")]
    public LayerMask acceptLayers = ~0;
    public bool useLayerFilter = false;

    [Header("Duplicate Guard")]
    [Tooltip("같은 딜러로부터 재타격 최소 간격(초). 0이면 비활성.")]
    public float perDealerCooldown = 0.0f;

    private readonly Dictionary<int, int> _lastHitFrame = new();
    private readonly Dictionary<int, float> _lastHitTime = new();

    void Reset()
    {
        owner = GetComponentInParent<OniStateMachine>();
        var col = GetComponent<Collider>();
        col.isTrigger = true; // 트리거 기준
    }

    void OnTriggerEnter(Collider other) => TryHit(other);

    void TryHit(Collider other)
    {
        if (!owner || !other) return;
        if (useLayerFilter && ((1 << other.gameObject.layer) & acceptLayers) == 0) return;

        // 1) VR 근접 무기: 게이트, 콜라이더는 건드리지 않음
        var vr = other.GetComponentInParent<VRMeleeDealer>();
        if (vr != null)
        {
            if (!vr.CanHit(owner.gameObject)) return;
            owner.ApplyDamage(vr.GetDamage());
            vr.MarkHit(owner.gameObject);
            return;
        }

        // 2) 일반 DamageDealer: 가드만
        var dealer = other.GetComponentInParent<DamageDealer>();
        if (dealer == null) return;

        int key = dealer.GetInstanceID();
        int f   = Time.frameCount;
        if (_lastHitFrame.TryGetValue(key, out var lastF) && lastF == f) return;
        if (perDealerCooldown > 0f &&
            _lastHitTime.TryGetValue(key, out var lastT) &&
            Time.time - lastT < perDealerCooldown) return;

        owner.ApplyDamage(dealer.damage);
        _lastHitFrame[key] = f;
        _lastHitTime[key]  = Time.time;

        // ✅ 여기서도 콜라이더/리짓바디/파괴 일절 없음
    }
}