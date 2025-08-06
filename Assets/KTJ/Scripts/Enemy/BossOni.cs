using System.Collections;
using UnityEngine;

// 1) Intro State
public class BossIntroState : OniBossState
{
    private float timer;
    private float delay;

    public BossIntroState(OniBossStateMachine m) : base(m)
    {
        delay = m.delay;
    }

    public override void Enter()
    {
        timer = 0f;
        _oniBossStateMachine.animator.SetTrigger("Idle");
        Debug.Log("Intro: Idle");
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        if (timer >= delay)
            _oniBossStateMachine.ChangeState(_oniBossStateMachine.CreateMoveState());
    }
}

// 2) Move State
public class MoveToPlayerState : OniBossState
{
    private float startY;
    bool skipCheck;    
    public MoveToPlayerState(OniBossStateMachine m) : base(m) { }

    public override void Enter()
    {
        startY = _oniBossStateMachine.transform.position.y;
        skipCheck = true;
        _oniBossStateMachine.animator.SetBool("isWalking", true);
        Debug.Log("Move: Walking");
    }

    public override void Update()
    {
        if (skipCheck)
        {
            skipCheck = false;
            return;
        }
        
        var pos = _oniBossStateMachine.transform.position;
        pos.y = startY;
        _oniBossStateMachine.transform.position = pos;
        
        // Rotate toward player
        Vector3 toPlayer = _oniBossStateMachine.PlayerTransform.position - _oniBossStateMachine.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0f)
        {
            Quaternion target = Quaternion.LookRotation(toPlayer.normalized);
            _oniBossStateMachine.transform.rotation = Quaternion.Slerp(_oniBossStateMachine.transform.rotation, target, _oniBossStateMachine.RotationSpeed * Time.deltaTime);
        }

        // Move forward
        _oniBossStateMachine.transform.position += _oniBossStateMachine.transform.forward * _oniBossStateMachine.MoveSpeed * Time.deltaTime;

        // If in attack range, pick one of two attacks
        if (toPlayer.magnitude <= _oniBossStateMachine.AttackRange)
        {
            _oniBossStateMachine.animator.SetBool("isWalking", false);
            if (UnityEngine.Random.value < 0.5f)
                _oniBossStateMachine.ChangeState(_oniBossStateMachine.CreateAttack1State());
            else
                _oniBossStateMachine.ChangeState(_oniBossStateMachine.CreateAttack2State());
        }
    }

    public override void Exit()
    {
        _oniBossStateMachine.animator.SetBool("isWalking", false);
    }
}

// 3) Attack1 State
public class Boss1NorAttackState : OniBossState
{
    private float startY;
    
    public Boss1NorAttackState(OniBossStateMachine m) : base(m) { }

    public override void Enter()
    {
        startY = _oniBossStateMachine.transform.position.y;
        
        _oniBossStateMachine.animator.SetTrigger("normalPunch");
        _oniBossStateMachine.EnableLeftAttack();
        _oniBossStateMachine.EnableRightAttack();
        Debug.Log("Attack1: normalPunch");
    }

    public override void Update()
    {
        var pos = _oniBossStateMachine.transform.position;
        pos.y = startY;
        _oniBossStateMachine.transform.position = pos;
    }
}

// 4) Attack2 State
public class Boss1NorAttack2State : OniBossState
{
    private float startY;
    public Boss1NorAttack2State(OniBossStateMachine m) : base(m) { }

    public override void Enter()
    {
        startY = _oniBossStateMachine.transform.position.y;
        
        _oniBossStateMachine.animator.SetTrigger("bigPunch");
        _oniBossStateMachine.EnableLeftAttack();
        Debug.Log("Attack2: bigPunch");
    }
    public override void Update()
    {
        var pos = _oniBossStateMachine.transform.position;
        pos.y = startY;
        _oniBossStateMachine.transform.position = pos;
    }
}

// 5) Die State (stub)

// 2페이즈 시작
public class Boss2PhaseStartState : OniBossState
{
    public Boss2PhaseStartState(OniBossStateMachine m) : base(m) { }
    public override void Enter()
    {
        _oniBossStateMachine.animator.SetTrigger("Phase2Start");
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
    private float startY;
    bool skipCheck;    
    float r = Random.value;
    public MoveToPlayer2PhaseState(OniBossStateMachine m) : base(m) { }

    public override void Enter()
    {
        startY = -0.02f;
        skipCheck = true;
        _oniBossStateMachine.animator.SetBool("isWalking2Phase", true);
        Debug.Log("Move: Walking2");
    }

    public override void Update()
    {
        if (skipCheck)
        {
            skipCheck = false;
            return;
        }
        
        var pos = _oniBossStateMachine.transform.position;
        pos.y = startY;
        _oniBossStateMachine.transform.position = pos;
        
        // Rotate toward player
        Vector3 toPlayer = _oniBossStateMachine.PlayerTransform.position - _oniBossStateMachine.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0f)
        {
            Quaternion target = Quaternion.LookRotation(toPlayer.normalized);
            _oniBossStateMachine.transform.rotation = Quaternion.Slerp(_oniBossStateMachine.transform.rotation, target, _oniBossStateMachine.RotationSpeed * Time.deltaTime);
        }

        // Move forward
        _oniBossStateMachine.transform.position += _oniBossStateMachine.transform.forward * _oniBossStateMachine.MoveSpeed * Time.deltaTime;

        // If in attack range, pick one of two attacks
        if (toPlayer.magnitude <= _oniBossStateMachine.AttackRange)
        {
            _oniBossStateMachine.animator.SetBool("isWalking2Phase", false);
            if (r < 0.33f)
            {
                // 50%
                _oniBossStateMachine.ChangeState(_oniBossStateMachine.CreateBoss2NorAttack());
            }
            else if (r < 0.67f)
            {
                // 30%
                _oniBossStateMachine.ChangeState(_oniBossStateMachine.CreateBoss2ComboAttack());
            }
            else
            {
                // 20%
                _oniBossStateMachine.ChangeState(_oniBossStateMachine.CreateBoss2SmashAttack());
            }
        }
    }

    public override void Exit()
    {
        _oniBossStateMachine.animator.SetBool("isWalking2Phase", false);
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
        Debug.Log("Boss Die");
    }
}