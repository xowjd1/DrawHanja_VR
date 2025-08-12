using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 1) Intro State
public class BossIntroState : OniBossState
{

    public BossIntroState(OniBossStateMachine m) : base(m)
    {

    }

    public override void Enter()
    {

        _oniBossStateMachine.animator.SetTrigger("Idle");
        _oniBossStateMachine.PlaySfx(_oniBossStateMachine.sfxIntro);
        Debug.Log("Intro: Idle");
    }

    public override void Update()
    {
        var normals = Object.FindObjectsOfType<OniStateMachine>();
        if (normals.Length == 0)
        {
            Debug.Log("Intro: All Normal Onis defeated! Switching to MoveState");
            _oniBossStateMachine.ChangeState(
                _oniBossStateMachine.CreateMoveState()
            );
        }
    }
}

// 2) Move State
public class MoveToPlayerState : OniBossState
{
    private NavMeshAgent _agent;
    private bool          _skipFirstCheck;

    public MoveToPlayerState(OniBossStateMachine m) : base(m)
    {
        _agent = m.GetComponent<NavMeshAgent>();
    }

    public override void Enter()
    {
        _skipFirstCheck = true;
        _agent.isStopped        = false;
        _agent.speed            = _oniBossStateMachine.MoveSpeed;
        _agent.angularSpeed     = _oniBossStateMachine.RotationSpeed;
        _agent.stoppingDistance = _oniBossStateMachine.AttackRange;
        _oniBossStateMachine.animator.SetBool("isWalking", true);

        // 바로 한번 목적지 설정
        _agent.SetDestination(_oniBossStateMachine.PlayerTransform.position);
    }

    public override void Update()
    {
        if (_skipFirstCheck)
        {
            _skipFirstCheck = false;
            return;
        }

        _agent.SetDestination(_oniBossStateMachine.PlayerTransform.position);

        if (!_agent.pathPending &&
            _agent.hasPath &&
            _agent.remainingDistance > _agent.stoppingDistance)
        {
            // 아직 이동 중
            return;
        }
        if (!_agent.pathPending &&
            _agent.hasPath &&
            _agent.remainingDistance <= _agent.stoppingDistance)
        {
            _agent.isStopped = true;
            _oniBossStateMachine.animator.SetBool("isWalking", false);
          
            // 두 가지 공격 중 랜덤 선택
            if (Random.value < 0.5f)
                _oniBossStateMachine.ChangeState(
                    _oniBossStateMachine.CreateAttack1State()
                );
            else
                _oniBossStateMachine.ChangeState(
                    _oniBossStateMachine.CreateAttack2State()
                );
        }
    }
    public override void Exit()
    {

            _agent.isStopped = true;
        _oniBossStateMachine.animator.SetBool("isWalking", false);
        Debug.Log("NavMesh Move: Stop Walking");
    }
}

// 3) Attack1 State
public class Boss1NorAttackState : OniBossState
{
    
    public Boss1NorAttackState(OniBossStateMachine m) : base(m) { }

    public override void Enter()
    {
        
        _oniBossStateMachine.animator.SetTrigger("normalPunch");
        _oniBossStateMachine.EnableLeftAttack();
        _oniBossStateMachine.EnableRightAttack();
        Debug.Log("Attack1: normalPunch");
    }

    public override void Update()
    {
        var pos = _oniBossStateMachine.transform.position;
        _oniBossStateMachine.transform.position = pos;
    }
}

// 4) Attack2 State
public class Boss1NorAttack2State : OniBossState
{
    public Boss1NorAttack2State(OniBossStateMachine m) : base(m) { }

    public override void Enter()
    {
        _oniBossStateMachine.animator.SetTrigger("bigPunch");
        _oniBossStateMachine.EnableLeftAttack();
        Debug.Log("Attack2: bigPunch");
    }
    public override void Update()
    {
        var pos = _oniBossStateMachine.transform.position;
        _oniBossStateMachine.transform.position = pos;
    }
}


// 2페이즈 시작
public class Boss2PhaseStartState : OniBossState
{
    public Boss2PhaseStartState(OniBossStateMachine m) : base(m) { }
    public override void Enter()
    {
        _oniBossStateMachine.animator.SetTrigger("Phase2Start");
        _oniBossStateMachine.PlaySfx(_oniBossStateMachine.sfxPhase2Start); 
        Debug.Log("Phase2: Start!");
    }

    public override void Update()
    {
        
    }

    public override void Exit()
    {
        
    }
}

// 2페이즈 플레이어 추적
public class MoveToPlayer2PhaseState : OniBossState
{
    private NavMeshAgent _agent;
    private bool  _skipFirstCheck;

    public MoveToPlayer2PhaseState(OniBossStateMachine m) : base(m)
    {
        _agent = m.GetComponent<NavMeshAgent>();
    }

    public override void Enter()
    {
        _skipFirstCheck = true;
        _agent.isStopped        = false;
        _agent.speed            = _oniBossStateMachine.MoveSpeed;
        _agent.angularSpeed     = _oniBossStateMachine.RotationSpeed;
        _agent.stoppingDistance = _oniBossStateMachine.AttackRange;
        _oniBossStateMachine.animator.SetBool("isWalking2Phase", true);

        // 바로 한번 목적지 설정
        _agent.SetDestination(_oniBossStateMachine.PlayerTransform.position);
    }

    public override void Update()
    {
        if (_skipFirstCheck)
        {
            _skipFirstCheck = false;
            // 그 다음부터 매 프레임 목적지 갱신
            return;
        }
        // 플레이어 위치를 목적지로 설정
        _agent.SetDestination(_oniBossStateMachine.PlayerTransform.position);
        
        if (!_agent.pathPending &&
            _agent.hasPath &&
            _agent.remainingDistance > _agent.stoppingDistance)
        {

            return;
        }

        if (!_agent.pathPending && 
            _agent.remainingDistance <= _oniBossStateMachine.AttackRange)
        {
            _agent.isStopped = true;
            _oniBossStateMachine.animator.SetBool("isWalking2Phase", false);
            if (Random.value < 0.33f)
            {
                _oniBossStateMachine.ChangeState(_oniBossStateMachine.CreateBoss2NorAttack());
            }
            else if (Random.value < 0.67f)
            {
                _oniBossStateMachine.ChangeState(_oniBossStateMachine.CreateBoss2ComboAttack());
            }
            else
            {
                _oniBossStateMachine.ChangeState(_oniBossStateMachine.CreateBoss2SmashAttack());
            }
        }
    }
    public override void Exit()
    {
        _agent.isStopped = true;
        _oniBossStateMachine.animator.SetBool("isWalking", false);
        Debug.Log("NavMesh Move: Stop Walking");
    }
}

// 2페 기본 공격1
public class Boss2NorAttackState : OniBossState
{
    private float startY;
    
    public Boss2NorAttackState(OniBossStateMachine m) : base(m) { }

    public override void Enter()
    {
        startY = _oniBossStateMachine.transform.position.y;
        
        _oniBossStateMachine.animator.SetTrigger("2Attack");
        _oniBossStateMachine.EnableWeaponAttack();
        Debug.Log("2Phase Nor Attack");
    }

    public override void Update()
    {
        var pos = _oniBossStateMachine.transform.position;
        pos.y = startY;
        _oniBossStateMachine.transform.position = pos;
    }

    public override void Exit()
    {
        _oniBossStateMachine.DisableWeaponAttack();
    }
}

// 2페 기본 공격2
public class Boss2ComboAttackState : OniBossState
{
    private float startY;
    
    public Boss2ComboAttackState(OniBossStateMachine m) : base(m) { }

    public override void Enter()
    {
        startY = _oniBossStateMachine.transform.position.y;
        
        _oniBossStateMachine.animator.SetTrigger("Combo");
        _oniBossStateMachine.EnableWeaponAttack();
        Debug.Log("2Phase Combo Attack");
    }

    public override void Update()
    {
        var pos = _oniBossStateMachine.transform.position;
        pos.y = startY;
        _oniBossStateMachine.transform.position = pos;
    }

    public override void Exit()
    {
        _oniBossStateMachine.DisableWeaponAttack();
    }
}

public class Boss2SmashAttackState : OniBossState
{
    private float startY;
    
    public Boss2SmashAttackState(OniBossStateMachine m) : base(m) { }

    public override void Enter()
    {
        startY = _oniBossStateMachine.transform.position.y;
        
        _oniBossStateMachine.animator.SetTrigger("Smash");
        _oniBossStateMachine.EnableWeaponAttack();
        Debug.Log("2Phase Smash Attack");
    }

    public override void Update()
    {
        var pos = _oniBossStateMachine.transform.position;
        pos.y = startY;
        _oniBossStateMachine.transform.position = pos;
    }

    public override void Exit()
    {
        _oniBossStateMachine.DisableWeaponAttack();
    }
}
public class BossDieState : OniBossState
{
    public BossDieState(OniBossStateMachine m) : base(m) { }
    public override void Enter()
    {
        _oniBossStateMachine.animator.SetTrigger("Die");
        _oniBossStateMachine.StartLoadNextSceneAfterDeath("Die", 0);
        Debug.Log("Boss Die");
    }
}