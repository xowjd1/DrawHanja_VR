using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OniBossDamageReceiver : MonoBehaviour
{
    public OniBossStateMachine owner;

    [Header("Optional Layer Filter")]
    public LayerMask acceptLayers = ~0;
    public bool useLayerFilter = false;

    [Header("DamageDealer 중복 가드(프로젝타일 등)")]
    public float dealerCooldown = 0.1f;

    readonly Dictionary<int, int>   _lastHitFrame = new();
    readonly Dictionary<int, float> _lastHitTime  = new();

    void Reset()
    {
        owner = GetComponentInParent<OniBossStateMachine>();
        var col = GetComponent<Collider>();
        col.isTrigger = true; // 트리거 기준
    }

    void OnTriggerEnter(Collider other) => TryHit(other);

    void TryHit(Collider other)
    {
        if (!owner || !other) return;
        if (useLayerFilter && ((1 << other.gameObject.layer) & acceptLayers) == 0) return;

        // 1) VR 근접 무기: 한 스윙 1히트 게이트, 콜라이더 손대지 않음
        var vr = other.GetComponentInParent<VRMeleeDealer>();
        if (vr != null)
        {
            if (!vr.CanHit(owner.gameObject)) return;
            owner.ApplyDamage(vr.GetDamage());
            vr.MarkHit(owner.gameObject);
            return;
        }

        // 2) 일반 DamageDealer(프로젝타일 등): 프레임/쿨다운 가드만
        var dealer = other.GetComponentInParent<DamageDealer>();
        if (dealer == null) return;

        int key = dealer.GetInstanceID();
        int f   = Time.frameCount;
        if (_lastHitFrame.TryGetValue(key, out var lastF) && lastF == f) return;
        if (dealerCooldown > 0f &&
            _lastHitTime.TryGetValue(key, out var lastT) &&
            Time.time - lastT < dealerCooldown) return;

        owner.ApplyDamage(dealer.damage);
        _lastHitFrame[key] = f;
        _lastHitTime[key]  = Time.time;

        // ✅ 콜라이더/리짓바디/파괴 아무것도 건드리지 않음
    }
}