using UnityEngine;

public class OniStateMachine : MonoBehaviour
{
    private OniState currentState;
    
    public GameObject player;
    public Transform jarThrowPoint;
    public GameObject jar;
    public Animator animator;

    public float detectionRange = 30f;
    

    private void Awake()
    {
        
        
    }

    private void Start()
    {
        ChangeState(CreateOniDanceState());
    }

    private void Update()
    {
        currentState?.Update();
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

    public OniState CreateOniPunchState()
    {
        return new OniPunchState(
            this,
            player
        );
    }

    public OniState CreateOniDieState()
    {
        return new OniDieState(
            this
        );
    }
    
    public void OnThrowJarEvent()
    {
        // 현재 state가 ThrowJarState라면 호출
        if (currentState is OniThrowJarState throwState)
            throwState.ThrowJarEvent();
    }
    
}
