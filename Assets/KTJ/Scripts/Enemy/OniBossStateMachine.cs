using System.Collections;
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
    [SerializeField] private BoxCollider bossWeapon;

    [Header("Settings")]
    public float delay              = 3f;
    [SerializeField] private float detectionRange    = 10f;
    [SerializeField] private float attackRange       = 2f;
    [SerializeField] private float moveSpeed         = 6f;
    [SerializeField] private float rotationSpeed     = 5f;
    
    [SerializeField] private GameObject smashVFXPrefab;
    [SerializeField] private SphereCollider smashCollider;
    [SerializeField] private Transform smashVFXSpawnPoint;

    // 1페이즈
    public OniBossState CreateIntroState()      => new BossIntroState(this);
    public OniBossState CreateMoveState()       => new MoveToPlayerState(this);
    public OniBossState CreateAttack1State()    => new Boss1NorAttackState(this);
    public OniBossState CreateAttack2State()    => new Boss1NorAttack2State(this);
    
    
    // 2페이즈
    public OniBossState CreatePhase2Start()     => new Boss2PhaseStartState(this);
    public OniBossState CreateMoveToPlayer2Phase()     => new MoveToPlayer2PhaseState(this);
    public OniBossState CreateBoss2NorAttack()     => new Boss2NorAttackState(this);
    public OniBossState CreateBoss2ComboAttack()     => new Boss2ComboAttackState(this);
    public OniBossState CreateBoss2SmashAttack()     => new Boss2SmashAttackState(this);
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
    public void OnPhase2StartFinished()
    {
        // 2페 추적 스테이트로 넘어간다
        ChangeState(CreateMoveToPlayer2Phase());
    }
    
    public void OnAttackFinished()
    {
        if (currentState is Boss1NorAttackState || currentState is Boss1NorAttack2State)
        {
            DisableLeftAttack();
            DisableRightAttack();
            ChangeState(CreateMoveState());
        }
    }
    public void On2PhaseAttackFinished()
    {
        if (currentState is Boss2NorAttackState || currentState is Boss2ComboAttackState || currentState is Boss2SmashAttackState)
        {

            ChangeState(CreateMoveToPlayer2Phase());
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
        weapon.transform.localScale = Vector3.one;
    }
    
    public void SmashVFX()
    {
        if (smashVFXPrefab != null && smashVFXSpawnPoint != null)
        {
            var vfxInstance = Instantiate(
                smashVFXPrefab,
                smashVFXSpawnPoint.position,
                Quaternion.Euler(0f, -90f, 0f)
            );

            EnableWeaponAttack();
            EnableSmashAttack();

            StartCoroutine(DisableAfter(vfxInstance, 1f));
        }
    }

    private IEnumerator DisableAfter(GameObject vfxInstance, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (vfxInstance != null)
            Destroy(vfxInstance);

        DisableWeaponAttack();
        DisableSmashAttack();
    }


    public void EnableLeftAttack()  => leftHand.enabled = true;
    public void EnableRightAttack() => rightHand.enabled = true;
    public void DisableLeftAttack() => leftHand.enabled = false;
    public void DisableRightAttack() => rightHand.enabled = false;
    public void EnableWeaponAttack() => bossWeapon.enabled = true;
    public void DisableWeaponAttack() => bossWeapon.enabled = false;
    public void EnableSmashAttack() => smashCollider.enabled = true;
    public void DisableSmashAttack() => smashCollider.enabled = false;

}