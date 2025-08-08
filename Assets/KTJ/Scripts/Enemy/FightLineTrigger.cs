// FightLineTrigger.cs
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FightLineTrigger : MonoBehaviour
{
    private OniStateMachine[] _oni;
    void Awake()
    {
        // 반드시 Trigger 로 설정
        GetComponent<BoxCollider>().isTrigger = true;
        
        // 씬에 있는 모든 OniStateMachine 컴포넌트 수집
        _oni = FindObjectsOfType<OniStateMachine>();
        if (_oni.Length == 0)
            Debug.LogWarning("씬에 OniStateMachine 인스턴스를 찾을 수 없습니다!");
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[FightLineTrigger] Player crossed fight line → notifying all bosses");

        // 모든 보스에 플래그 세팅 (플래그 기반 전환 방식)
        foreach (var oni in _oni)
        {
            oni.playerHitFightLine = true;
        }
    }
}