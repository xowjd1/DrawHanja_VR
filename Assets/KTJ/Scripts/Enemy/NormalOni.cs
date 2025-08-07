using System.Collections;
using UnityEngine;

//플레이어 조우 전 춤추는 상태
public class OniDanceState : OniState
{
    public OniDanceState(OniStateMachine oniStateMachine) : base(oniStateMachine)
    {
        
    }
    public override void Enter()
    {
        _oniStateMachine.animator.Play("Dance");
    }

    public override void Update()
    {
        // 플레이어와의 거리 계산
        float dist = Vector3.Distance(
            _oniStateMachine.transform.position,
            _oniStateMachine.player.transform.position
        );
        Debug.Log($"[DanceState] Distance to player: {dist:F2}");

        // 범위 안으로 들어오면 던지기 상태로 전환
        if (dist <= _oniStateMachine.detectionRange)
        {
            Debug.Log("[DanceState] In range! Switching to ThrowJarState");
            _oniStateMachine.ChangeState(
                _oniStateMachine.CreateOniThrowJarState()
            );
        }
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
    private float rotSpeed  = 10f;
    private bool hasThrown = false;
    
    private float _trackTime  = 1f;
    private Vector3 _lockedTarget;
    
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
        _oniStateMachine.StartCoroutine(TrackThenThrow());
       //_oniStateMachine.animator.applyRootMotion = false;
    }

    public override void Update()
    {

    }

    IEnumerator TrackThenThrow()
    {
        float t = 0f;
        while (t < _trackTime)
        {
            t += Time.deltaTime;
            // 부드럽게만 플레이어 바라보기
            var dir = _oniStateMachine.player.transform.position
                      - _oniStateMachine.transform.position;
            dir.y = 0;
            _oniStateMachine.transform.rotation = Quaternion.Slerp(
                _oniStateMachine.transform.rotation,
                Quaternion.LookRotation(dir.normalized),
                8f * Time.deltaTime
            );
            yield return null;
        }

        // 1초 뒤, 그 위치를 고정
        _lockedTarget = _oniStateMachine.player.transform.position;
        // 애니메이션 트리거
        _oniStateMachine.animator.SetTrigger("ThrowJar");
    }
    
    private void ThrowJar(Vector3 targetPos)
    {
        // 1) 던질 지점(손 위치)과 술병 참조
        Vector3 spawnPos = _oniStateMachine.jarThrowPoint.position;
        GameObject jar   = _oniStateMachine.jar;

        // 2) 부모 해제
        jar.transform.SetParent(null);
        jar.transform.position = spawnPos;

        // 3) Rigidbody 준비
        Rigidbody rb = jar.GetComponent<Rigidbody>() 
                       ?? jar.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity  = true;

        // 4) 방향 및 초기 속도 계산
        Vector3 toTarget   = (targetPos - spawnPos).normalized;
        float   speed      = throwForce;
        float   upStrength = speed * 0.5f;

        // 5) 속도 적용 (포물선 궤적)
        rb.linearVelocity = toTarget * speed
                      + Vector3.up * upStrength;
    }

    public void ThrowJarEvent()
    {
        if (hasThrown) return;
        hasThrown = true;

        // 고정된 위치로만 던지기
        ThrowJar(_lockedTarget);

    }
    
    public override void Exit()
    {
        
    }
}

// 플레이어한테 뛰어가서 주먹공격 패턴
public class OniPunchState : OniState
{
    private float startY;
    
    public OniPunchState(OniStateMachine m) : base(m) { }

    public override void Enter()
    {
        startY = _oniStateMachine.transform.position.y;
        
        _oniStateMachine.animator.SetTrigger("Punch");
        _oniStateMachine.EnableRightAttack();
        Debug.Log("Attack1: normalPunch");
    }

    public override void Update()
    {
        var pos = _oniStateMachine.transform.position;
        pos.y = startY;
        _oniStateMachine.transform.position = pos;
    }
    

    public override void Exit()
    {
        _oniStateMachine.DisableRightAttack();
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
        _oniStateMachine.animator.SetTrigger("Die");
        Debug.Log("oni Die");
    }

    public override void Update()
    {
        
    }

    public override void Exit()
    {
        
    }
}

public class NOMoveToPlayerState : OniState
{
    private float startY;
    bool skipCheck;

    public NOMoveToPlayerState(OniStateMachine s) : base(s)
    {
    }

    public override void Enter()
    {
        startY = _oniStateMachine.transform.position.y;
        skipCheck = true;
        _oniStateMachine.animator.SetBool("isWalking", true);
        Debug.Log("Move: Walking");
    }

    public override void Update()
    {
        if (skipCheck)
        {
            skipCheck = false;
            return;
        }

        var pos = _oniStateMachine.transform.position;
        pos.y = startY;
        _oniStateMachine.transform.position = pos;

        // Rotate toward player
        Vector3 toPlayer = _oniStateMachine.PlayerTransform.position - _oniStateMachine.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0f)
        {
            Quaternion target = Quaternion.LookRotation(toPlayer.normalized);
            _oniStateMachine.transform.rotation = Quaternion.Slerp(_oniStateMachine.transform.rotation, target,
                _oniStateMachine.RotationSpeed * Time.deltaTime);
        }

        // Move forward
        _oniStateMachine.transform.position +=
            _oniStateMachine.transform.forward * _oniStateMachine.MoveSpeed * Time.deltaTime;

        // If in attack range, pick one of two attacks
        if (toPlayer.magnitude <= _oniStateMachine.AttackRange)
        {
            _oniStateMachine.animator.SetBool("isWalking", false);
            _oniStateMachine.ChangeState(_oniStateMachine.CreateOniPunchState());

        }
    }
}