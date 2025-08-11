using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackCollider : MonoBehaviour
{
    public int damage = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Player의 체력 스크립트 참조
            var hp = other.GetComponent<PlayerHealth>();
            if (hp != null)
                hp.TakeDamage(damage);
            Debug.Log("HIT");
        }
    }
}
