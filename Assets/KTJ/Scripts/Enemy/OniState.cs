public abstract class OniState
{
    protected OniStateMachine _oniStateMachine;

    public OniState(OniStateMachine oniStateMachine)
    {
        _oniStateMachine = oniStateMachine;
    }
    
    public virtual void Enter() {}
    public virtual void Update() {}
    public virtual void Exit() {}
}