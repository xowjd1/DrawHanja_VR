using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage);
}

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 100;
    private int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"플레이어가 {damage} 데미지를 받음. 남은 체력: {currentHealth}");
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("플레이어 사망!");
        // 사망 처리 (리스폰, 게임 오버 등)
    }
}

