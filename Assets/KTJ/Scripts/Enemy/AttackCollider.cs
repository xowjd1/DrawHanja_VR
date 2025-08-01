using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackCollider : MonoBehaviour
{
    public int damage = 10;
    private HashSet<Collider> _hitTargets = new HashSet<Collider>();

    void OnEnable()
    {
        _hitTargets.Clear();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_hitTargets.Contains(other)) return;
        _hitTargets.Add(other);

        // 예: 적에게 IDamageable 인터페이스가 있으면 호출
        var dmgable = other.GetComponent<IDamageable>();
        if (dmgable != null)
            dmgable.TakeDamage(damage);
    }
}
