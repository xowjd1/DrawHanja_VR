using UnityEngine;
public class OniBossStateMachine : MonoBehaviour
{
    private OniBossState currentState;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    public float currentHealth;
    private bool  phase2Triggered = false;
    
    [Header("Weapon Mounts")]
    [SerializeField] private Transform backMount; 
    [SerializeField] private Transform handMount;
    [SerializeField] private GameObject weapon;
    
    [Header("References")]
    public Animator animator;
    [SerializeField] private GameObject player;
    [SerializeField] private SphereCollider leftHand;
    [SerializeField] private SphereCollider rightHand;

    [Header("Settings")]
    public float delay              = 3f;
    [SerializeField] private float detectionRange    = 10f;
    [SerializeField] private float attackRange       = 2f;
    [SerializeField] private float moveSpeed         = 6f;
    [SerializeField] private float rotationSpeed     = 5f;

    // 1페이즈
    public OniBossState CreateIntroState()      => new BossIntroState(this);
    public OniBossState CreateMoveState()       => new MoveToPlayerState(this);
    public OniBossState CreateAttack1State()    => new Boss1NorAttackState(this);
    public OniBossState CreateAttack2State()    => new Boss1NorAttack2State(this);
    
    
    // 2페이즈
    public OniBossState CreatePhase2Start()     => new Boss2PhaseStartState(this);
    public OniBossState CreateDieState()        => new BossDieState(this);

    // Exposed properties
    public Transform PlayerTransform => player.transform;
    public float     MoveSpeed       => moveSpeed;
    public float     RotationSpeed   => rotationSpeed;
    public float     AttackRange     => attackRange;
    public float     DetectionRange  => detectionRange;

    private void Awake()
    {
        currentHealth = maxHealth;
    }
    
    private void Start()
    {
        ChangeState(CreateIntroState());
    }

    private void Update()
    {
        currentState?.Update();
    }

    public void ChangeState(OniBossState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    // Animation Event: 호출 시점에만 한 번 실행
    public void OnAttackFinished()
    {
        if (currentState is Boss1NorAttackState || currentState is Boss1NorAttack2State)
        {
            DisableLeftAttack();
            DisableRightAttack();
            ChangeState(CreateMoveState());
        }
    }
    
    public void ApplyDamage(float dmg)
    {
        if (currentHealth <= 0) return;
        currentHealth = Mathf.Max(0, currentHealth - dmg);

        // 체력 50% 이하 & 아직 2페 진입 안 했으면
        if (!phase2Triggered && currentHealth <= maxHealth * 0.5f)
        {
            phase2Triggered = true;
            ChangeState(CreatePhase2Start());
        }

        if (currentHealth == 0)
            ChangeState(CreateDieState());
    }
    
    public void EquipWeaponToHand()
    {
        // 무기를 등에서 손으로 옮기기
        weapon.transform.SetParent(handMount, false);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }

    // (필요시) 다시 등에 거는 함수
    public void StoreWeaponOnBack()
    {
        weapon.transform.SetParent(backMount, false);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }

    public void EnableLeftAttack()  => leftHand.enabled = true;
    public void EnableRightAttack() => rightHand.enabled = true;
    public void DisableLeftAttack() => leftHand.enabled = false;
    public void DisableRightAttack()=> rightHand.enabled = false;
}