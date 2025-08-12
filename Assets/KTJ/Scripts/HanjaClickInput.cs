using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HanjaClickInput : MonoBehaviour
{
    [Header("Ray Source")]
    public Transform rayOrigin;                 // Right Controller 트랜스폼
    public XRRayInteractor xrRay;  
    private InputAction _action;

    [Header("Input")]
    public InputActionReference triggerAction;
    [Range(0f,1f)] public float pressThreshold = 0.5f;

    [Header("Filter")]
    public LayerMask interactMask;              // ← Interactable 전용 레이어만 포함하도록
    public float maxDistance = 15f;

    [Header("Target UI")]
    public HanjaUIController ui;

    bool wasPressed;

    void OnEnable()
    {
        if (triggerAction)
        {
            _action = triggerAction.action;
            _action?.Enable();
        }
        Debug.Log($"[HanjaClick] OnEnable on '{name}' (GO.active={gameObject.activeInHierarchy})");
    }

// OnDisable
    void OnDisable() { _action?.Disable(); }
    void Start()
    {
        if (ui == null)
        {
            ui = FindObjectOfType<HanjaUIController>(true);
            if (ui) Debug.Log("[HanjaClick] ui 할당 누락 → 자동 연결 성공");
            else    Debug.LogWarning("[HanjaClick] ui가 null 입니다. Inspector에 연결하거나 씬에 존재하는지 확인!");
        }

        if (xrRay == null) Debug.LogWarning("[HanjaClick] xrRay가 null 입니다. XRRayInteractor 연결 권장");
        if (rayOrigin == null) Debug.LogWarning("[HanjaClick] rayOrigin이 null 입니다. (컨트롤러 Transform)");

        // interactMask 진단
        if (interactMask.value == 0)
            Debug.LogWarning("[HanjaClick] interactMask가 0입니다(아무 레이어도 못 맞춤). Inspector에서 대상 레이어 설정!");
        else
            Debug.Log($"[HanjaClick] interactMask={interactMask.value} (예: {LayerMaskToString(interactMask)})");

        // XR 레이 마스크 동기화(있을 때)
        if (xrRay != null) xrRay.raycastMask = interactMask;
    }
    void Update()
{
    if (_action == null)
    {
        Debug.LogWarning("[HanjaClick] _action null");
        return;
    }

    if (ui == null)
    {
        Debug.LogWarning("[HanjaClick] Update: ui == null (패널 열 대상 없음).");
        return;
    }
    if (triggerAction == null)
    {
        Debug.LogWarning("[HanjaClick] Update: triggerAction == null (InputActionReference 미세팅).");
        return;
    }

    var action = triggerAction.action;
    if (action == null)
    {
        Debug.LogWarning("[HanjaClick] Update: triggerAction.action == null (InputAction asset 연결 확인).");
        return;
    }
    if (!_action.enabled)
    {
        _action.Enable();
        Debug.LogWarning("[HanjaClick] action was disabled → re-enabled");
    }

    float val = 0f;
    try { val = _action.ReadValue<float>(); } catch { return; }

    bool pressed = val > pressThreshold;
    // 상태 로그 (스팸 방지: 눌림 엣지에만 출력)
    if (pressed && !wasPressed)
        Debug.Log($"[HanjaClick] Trigger edge: val={val:0.00} > th={pressThreshold}");

    if (pressed && !wasPressed)
    {
        // 1) XRRayInteractor 우선
        if (xrRay != null && xrRay.enabled)
        {
            if (xrRay.TryGetCurrent3DRaycastHit(out var xrHit))
            {
                Debug.Log($"[HanjaClick] XR hit: {xrHit.collider?.name ?? "null"}");
                if (TryHandleHit(xrHit.collider, xrHit.point)) { wasPressed = pressed; return; }
                else Debug.Log("[HanjaClick] XR hit but no HanjaClickable on target.");
            }
            else
            {
                Debug.Log("[HanjaClick] XRRayInteractor.TryGetCurrent3DRaycastHit = false");
            }
        }
        else if (xrRay == null)
        {
            Debug.Log("[HanjaClick] xrRay == null → 물리 레이로 진행");
        }
        else if (!xrRay.enabled)
        {
            Debug.LogWarning("[HanjaClick] xrRay.enabled == false");
        }

        // 2) 물리 레이캐스트
        if (rayOrigin != null)
        {
            Vector3 p = rayOrigin.position;
            Vector3 f = rayOrigin.forward;
            Debug.DrawRay(p, f * maxDistance, Color.cyan, 0.25f);

            if (Physics.Raycast(p, f, out var hit, maxDistance, interactMask, QueryTriggerInteraction.Collide))
            {
                Debug.Log($"[HanjaClick] Physics hit: {hit.collider.name} (layer {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                if (TryHandleHit(hit.collider, hit.point)) { wasPressed = pressed; return; }
                else Debug.Log("[HanjaClick] Physics hit but no HanjaClickable on target.");
            }
            else
            {
                Debug.Log("[HanjaClick] Physics.Raycast no hit.");
            }
        }
        else
        {
            Debug.LogWarning("[HanjaClick] rayOrigin is null (물리 레이 불가).");
        }
    }

    wasPressed = pressed;
}

    bool TryHandleHit(Collider col, Vector3 point)
    {
        if (col == null) return false;
        var clickable = col.GetComponent<HanjaClickable>() ?? col.GetComponentInParent<HanjaClickable>();
        if (clickable == null) return false;

        Debug.Log($"[HanjaClick] Clicked '{col.name}' -> index {clickable.index}");
        ui.OpenPanelWithIndex(clickable.index);
        return true;
    }
    string LayerMaskToString(LayerMask mask)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < 32; i++)
            if ((mask.value & (1 << i)) != 0)
                sb.Append(LayerMask.LayerToName(i)).Append(',');
        return sb.ToString().TrimEnd(',');
    }
}
