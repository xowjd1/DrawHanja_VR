using UnityEngine;

public class TestDamage : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private OniBossStateMachine boss;  // 데미지 입힐 보스
    [SerializeField] private float damageAmount = 10f;  // 입힐 데미지 양

    void Update()
    {
        Debug.Log("▸ TestDamage.Update");                // ① 반드시 찍히는지
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("▸ TestDamage: A키 눌림!");       // ② 이 로그도 찍히는지
            if (boss == null)
            {
                Debug.LogError("TestDamage ▶ boss null!");
                return;
            }
            boss.ApplyDamage(damageAmount);
            Debug.Log($"TestDamage ▶ 보스 체력: {boss.currentHealth}");
        }
    }
}