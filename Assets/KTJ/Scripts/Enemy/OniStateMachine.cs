using UnityEngine;

public class OniStateMachine : MonoBehaviour
{
    private OniState currentState;
    [SerializeField] private GameObject player;
    [SerializeField] private Transform jarThrowPoint;
    [SerializeField] private GameObject jar;
    

    private void Awake()
    {
        
        
    }

    private void Start()
    {
        ChangeState(CreateOniThrowJarState());
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
    
}
