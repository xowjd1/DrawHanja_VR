using UnityEngine;

public class OniStateMachine : MonoBehaviour
{
    private OniState currentState;
    
    public GameObject player;
    public Transform jarThrowPoint;
    public GameObject jar;
    public Animator animator;
    [HideInInspector] public bool playerHitFightLine;
    public string fightLineTag = "FightLine";
    
    public float detectionRange = 30f;
    [SerializeField] private float maxHealth = 100f;
    public float currentHealth;
    [SerializeField] private SphereCollider rightHand;
    [SerializeField] private float attackRange       = 2f;
    [SerializeField] private float moveSpeed         = 4f;
    [SerializeField] private float rotationSpeed     = 5f;
    
    public Transform PlayerTransform => player.transform;
    public float     AttackRange     => attackRange;
    public float     MoveSpeed       => moveSpeed;
    public float     RotationSpeed   => rotationSpeed;
    
    void Awake()
    {
        currentHealth = maxHealth;
    }
    private void Start()
    {
        ChangeState(CreateOniDanceState());
        currentState.Enter();
    }

    private void Update()
    {
        currentState?.Update();
    }

    public OniState CreateMoveToPlayer()
    {
    return new NOMoveToPlayerState(this);
    }
    
    public void ChangeState(OniState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
    
    public OniState CreateOniDanceState()
    {
        return new OniDanceState(
            this
            
        );
    }

    public OniState CreateOniIntroState()
    {
        return new OniIntroState(
            this,
            player
        );
    }

    public OniState CreateOniThrowJarState()
    {
        return new OniThrowJarState(
            this,
            player,
            jarThrowPoint,
            jar
        );
    }

    public OniState CreateOniPunchState() => new OniPunchState(this);

    public OniState CreateOniDieState()
    {
        return new OniDieState(
            this
        );
    }
    
    public void OnThrowJarEvent()
    {
        if (currentState is OniThrowJarState throwState)
            throwState.ThrowJarEvent();
    }
    public void OnThrowingFinished()
    {
        ChangeState(CreateMoveToPlayer());
    }

    public void OnAttackFinished()
    {
        ChangeState(CreateMoveToPlayer());
    }

    public void ApplyDamage(float dmg)
    {
        if (currentHealth <= 0) return;
        currentHealth = Mathf.Max(0, currentHealth - dmg);

        if (currentHealth == 0)
            ChangeState(CreateOniDieState());
    }
    
    
    
    public void EnableRightAttack() => rightHand.enabled = true;
    public void DisableRightAttack() => rightHand.enabled = false;
    
}
