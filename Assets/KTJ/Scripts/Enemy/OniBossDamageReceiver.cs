using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 피격 수신기.
/// - 트리거 콜라이더 기준 (중복 방지 위해 충돌 콜백은 사용하지 않음)
/// - VRMeleeDealer(무기)에서 게이트(CanHit/MarkHit)를 확인하여 한 스윙당 1회 타격
/// - 무기/이펙트 오브젝트를 파괴하지 않음
/// - (옵션) 일반 DamageDealer도 지원하려면 하단 확장 경로 사용
/// </summary>
[RequireComponent(typeof(Collider))]
public class OniBossDamageReceiver : MonoBehaviour
{
    public OniBossStateMachine owner;

    [Header("Optional Layer Filter")]
    public LayerMask acceptLayers = ~0;
    public bool useLayerFilter = false;

    [Header("옵션: 일반 DamageDealer 지원 시(프로젝타일 등) 중복 가드")]
    [Tooltip("DamageDealer 경로에서 같은 딜러의 재타격 최소 간격(초)")]
    public float dealerCooldown = 0.1f;

    // DamageDealer(옵션) 경로용 가드
    readonly Dictionary<int, int>   _lastHitFrame = new();
    readonly Dictionary<int, float> _lastHitTime  = new();

    void Reset()
    {
        owner = GetComponentInParent<OniBossStateMachine>();
        var col = GetComponent<Collider>();
        col.isTrigger = true; // 트리거 기준으로 사용
    }

    void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    // ⚠️ 이중호출 방지 위해 충돌 콜백은 사용하지 않습니다.
    // void OnCollisionEnter(Collision c) => TryHit(c.collider);

    void TryHit(Collider other)
    {
        if (owner == null || other == null) return;

        if (useLayerFilter && ((1 << other.gameObject.layer) & acceptLayers) == 0)
            return;

        // 1) VR 무기 우선 지원
        var vr = other.GetComponentInParent<VRMeleeDealer>();
        if (vr != null)
        {
            if (!vr.CanHit(owner.gameObject)) return;

            owner.ApplyDamage(vr.GetDamage());
            vr.MarkHit(owner.gameObject);
            // 무기/이펙트 파괴 없음
            return;
        }

        // 2) (옵션) 일반 DamageDealer도 지원하려면 아래 경로 활성화
        var dealer = other.GetComponentInParent<DamageDealer>(); // 있으면 사용
        if (dealer != null)
        {
            int key = dealer.GetInstanceID();

            // 같은 프레임 중복 방지
            int f = Time.frameCount;
            if (_lastHitFrame.TryGetValue(key, out var lastF) && lastF == f) return;

            // 쿨다운
            if (dealerCooldown > 0f &&
                _lastHitTime.TryGetValue(key, out var t) &&
                Time.time - t < dealerCooldown) return;

            owner.ApplyDamage(dealer.damage);

            _lastHitFrame[key] = f;
            _lastHitTime[key]  = Time.time;

            // ❌ 여기서 딜러 오브젝트를 파괴하지 않습니다.
            // (투사체라면 딜러 쪽에서 자체 처리/풀 반환 권장)
            return;
        }

        // 기타 타입은 무시
    }
}
