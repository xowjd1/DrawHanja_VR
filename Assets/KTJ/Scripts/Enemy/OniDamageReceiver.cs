using System.Collections.Generic;
using UnityEngine;

public class OniDamageReceiver : MonoBehaviour
{
    public OniStateMachine owner;
       [Header("Optional Filter")]
    public LayerMask acceptLayers = ~0;
    public bool useLayerFilter = false;

    [Header("Duplicate Guard")]
    public float perDealerCooldown = 0.0f;

    // dealerInstanceID -> last hit frame/time
    private readonly Dictionary<int, int> _lastHitFrame = new();
    private readonly Dictionary<int, float> _lastHitTime = new();

    void Reset()
    {
        owner = GetComponentInParent<OniStateMachine>();
        var col = GetComponent<Collider>();
        col.isTrigger = true; // 이 스크립트는 트리거 기준으로만 사용
    }

    void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    void TryHit(Collider other)
    {
        if (owner == null || other == null) return;

        // 레이어 필터 (선택)
        if (useLayerFilter && ((1 << other.gameObject.layer) & acceptLayers) == 0)
            return;

        // 무기/스킬 루트의 DamageDealer를 취득 (자식 콜라이더 대응)
        var dealer = other.GetComponentInParent<DamageDealer>();
        if (dealer == null) return;

        int key = dealer.GetInstanceID();

        // 같은 프레임 중복 방지
        int f = Time.frameCount;
        if (_lastHitFrame.TryGetValue(key, out var lastF) && lastF == f) return;

        // 쿨다운(선택) – 필요 없으면 0으로 유지
        if (perDealerCooldown > 0f &&
            _lastHitTime.TryGetValue(key, out var lastT) &&
            Time.time - lastT < perDealerCooldown) return;

        // 실제 적용
        Debug.Log($"[Boss Hit] {owner.name} <- {dealer.name} dmg={dealer.damage}");
        owner.ApplyDamage(dealer.damage);

        _lastHitFrame[key] = f;
        _lastHitTime[key]  = Time.time;

    }
}
