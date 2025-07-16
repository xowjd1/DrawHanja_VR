using UnityEngine;

//플레이어 조우 전 춤추는 상태
public class OniDanceState : OniState
{
    public OniDanceState(OniStateMachine oniStateMachine) : base(oniStateMachine)
    {
        
    }
    public override void Enter()
    {
        
    }

    public override void Update()
    {
        
    }

    public override void Exit()
    {
        
    }
    
}

// 플레이어 조우 상태
public class OniIntroState : OniState
{
    private GameObject _player;
    public OniIntroState(OniStateMachine oniStateMachine, GameObject player) : base(oniStateMachine)
    {
        _player = player;
    }

    public override void Enter()
    {
        
    }

    public override void Update()
    {
        
    }

    public override void Exit()
    {
        
    }
}

// 술병 던지기 패턴
public class OniThrowJarState : OniState
{
    private GameObject _player;
    private Transform _jarThrowPoint;
    private GameObject _jar;
    private float throwForce = 10f;
    private bool hasThrown = false;
    
    public OniThrowJarState(OniStateMachine oniStateMachine, GameObject player,
        Transform jarThrowPoint, GameObject jar) : base(oniStateMachine)
    {
        _player = player;
        _jarThrowPoint = jarThrowPoint;
        _jar = jar;
    }

    public override void Enter()
    {
        hasThrown = false;
    }

    public override void Update()
    {
        if (hasThrown)
            return;

        ThrowJar();
        hasThrown = true;

    }

    private void ThrowJar()
    {
        if(_jar == null || _jarThrowPoint == null)
            return;

        _jar.transform.SetParent(null);
        
        Rigidbody rb = _jar.GetComponent<Rigidbody>();
        if(rb == null)
            rb = _jar.AddComponent<Rigidbody>();
        
        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 dir = (_player.transform.position - _jarThrowPoint.position).normalized;
        rb.linearVelocity = dir * throwForce + Vector3.up * 2f;
    }

    public override void Exit()
    {
        
    }
}

// 플레이어한테 뛰어가서 주먹공격 패턴
public class OniPunchState : OniState
{
    private GameObject _player;
    private float _moveSpeed = 5f;
    private float _stopDistance = 1f;
    public OniPunchState(OniStateMachine oniStateMachine, GameObject player) : base(oniStateMachine)
    {
        _player = player;
    }

    public override void Enter()
    {
        
    }

    public override void Update()
    {
        if (_player == null) return;

        Vector3 direction = (_player.transform.position - _oniStateMachine.transform.position);
        float distance = direction.magnitude;

        if (distance > _stopDistance)
        {
            // 방향 정규화 후 이동
            Vector3 moveDir = direction.normalized;
            _oniStateMachine.transform.forward = moveDir; // 오니가 플레이어 쪽 보도록
            _oniStateMachine.transform.position += moveDir * _moveSpeed * Time.deltaTime;
        }
        else
        {

        }
    }

    public override void Exit()
    {
        
    }
}

// 죽음 상태
public class OniDieState : OniState
{
    public OniDieState(OniStateMachine oniStateMachine) : base(oniStateMachine)
    {
        
    }

    public override void Enter()
    {
        
    }

    public override void Update()
    {
        
    }

    public override void Exit()
    {
        
    }
}