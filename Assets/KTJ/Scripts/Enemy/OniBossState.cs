public abstract class OniBossState
{
    protected OniBossStateMachine _oniBossStateMachine;

    public OniBossState(OniBossStateMachine oniBossStateMachine)
    {
        _oniBossStateMachine = oniBossStateMachine;
    }
    
    public virtual void Enter() {}
    public virtual void Update() {}
    public virtual void Exit() {}
}