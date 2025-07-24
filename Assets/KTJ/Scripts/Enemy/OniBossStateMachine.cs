using UnityEngine;

public class OniBossStateMachine : MonoBehaviour
{
    private OniBossState currentState;
    [SerializeField] private GameObject player;
    private void Start()
    {
        
    }

    private void Update()
    {
        
    }

    public void ChangeState(OniBossState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public OniBossState CreateBossIntroState()
    {
        return new BossIntroState(
            this
        );
    }
}

