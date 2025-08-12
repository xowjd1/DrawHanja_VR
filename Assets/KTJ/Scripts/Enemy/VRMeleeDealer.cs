using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // 새 입력 시스템 사용 시

/// <summary>
/// VR로 직접 휘두르는 근접 무기용 데미지 딜러.
/// - 콜라이더는 항상 ON 상태로 유지
/// - 무기 속도가 임계값을 넘으면 공격창 ON, 떨어지면 OFF (히스테리시스)
/// - 스윙(공격창) 동안 같은 타겟은 1회만 타격
/// </summary>
[RequireComponent(typeof(Collider))]
public class VRMeleeDealer : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 10;

    [Header("중복타 제어")]
    public bool oneHitPerWindow = true;    // 창당 대상당 1회
    public float perTargetCooldown = 0f;   // 0이면 쿨다운 없음

    [Header("입력")]
    public InputActionReference attackButton;   // 트리거 등
    public bool whileHeld = true;               // 누르는 동안만 ON
    public float tapWindowSeconds = 0.35f;      // whileHeld=false 일 때 창 길이

    bool windowOpen;
    HashSet<int> hitThisWindow = new();
    Dictionary<int,float> lastHitTime = new();

    void OnEnable()  { attackButton?.action.Enable(); }
    void OnDisable() { attackButton?.action.Disable(); windowOpen=false; }

    void Update()
    {
        if (attackButton == null) return;
        var a = attackButton.action;
        float val = a.ReadValue<float>();

        if (whileHeld)
        {
            if (val > 0.5f && !windowOpen) OpenWindow();
            else if (val <= 0.3f && windowOpen) CloseWindow();
        }
        else
        {
            if (val > 0.5f && !windowOpen) StartCoroutine(OpenTapWindow());
        }
    }

    IEnumerator OpenTapWindow()
    {
        OpenWindow();
        yield return new WaitForSeconds(tapWindowSeconds);
        CloseWindow();
    }

    void OpenWindow()  { windowOpen = true; hitThisWindow.Clear(); }
    void CloseWindow() { windowOpen = false; }

    // === Receiver에서 호출할 API ===
    public bool CanHit(GameObject target)
    {
        if (!windowOpen) return false;
        int id = target.GetInstanceID();

        if (oneHitPerWindow && hitThisWindow.Contains(id)) return false;
        if (perTargetCooldown > 0f &&
            lastHitTime.TryGetValue(id, out var t) &&
            Time.time - t < perTargetCooldown) return false;

        return true;
    }

    public void MarkHit(GameObject target)
    {
        int id = target.GetInstanceID();
        hitThisWindow.Add(id);
        lastHitTime[id] = Time.time;
    }

    public int GetDamage() => damage;
}
