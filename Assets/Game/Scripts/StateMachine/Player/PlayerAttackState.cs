public class PlayerAttackState : PlayerBaseState
{
    private float _previousFrameTime;
    private bool _alreadApllyForce;
    private Attack _currentAttack;

    public PlayerAttackState(PlayerStateMachine playerStateMachine, int attackIndex) : base(playerStateMachine) 
    {
        _currentAttack = playerStateMachine.Attacks[attackIndex];
    }

    public override void Enter()
    {
        var currentAttack = playerStateMachine.CurrentAttack;
        playerStateMachine.CurrentAttack = currentAttack == playerStateMachine.Attacks.Length -1 ? 0 : currentAttack;
        playerStateMachine.WeaponDamage.SetDamage(_currentAttack.Damage);
        playerStateMachine.EquippedWeapon.GetWeapon();
        playerStateMachine.Animator.CrossFadeInFixedTime(_currentAttack.AttackName, _currentAttack.TransitionDuration);
    }

    public override void Exit()
    {
        playerStateMachine.EquippedWeapon.SaveWeapon();
        playerStateMachine.CurrentAttack++;
    }

    public override void Tick(float deltaTime)
    {
        Move(deltaTime);
        FaceTarget();

        float normalizedTime = GetNormalizedTime();

        if(normalizedTime >= _previousFrameTime && normalizedTime < 1f)
        {
            if(normalizedTime >= _currentAttack.ForceTime)
            {
                TryApplyForce();
            }

            if(playerStateMachine.InputReader.IsAttacking)
            {
                TryComboAttack(normalizedTime);
            }
        }
        else
        {
            if(playerStateMachine.Targeter.CurrentTarget == null)
            {
                playerStateMachine.SwitchState(new PlayerGroundedState(playerStateMachine));    
            }
            else
            {
                playerStateMachine.SwitchState(new PlayerTargetingState(playerStateMachine));    
            }
        }

        _previousFrameTime = normalizedTime;
    }

    float GetNormalizedTime()
    {
        var currentInfo = playerStateMachine.Animator.GetCurrentAnimatorStateInfo(0);
        var nextInfo = playerStateMachine.Animator.GetNextAnimatorStateInfo(0);
        
        if(playerStateMachine.Animator.IsInTransition(0) && nextInfo.IsTag("Attack"))
        {
            return nextInfo.normalizedTime;
        }
        else if(!playerStateMachine.Animator.IsInTransition(0) && currentInfo.IsTag("Attack"))
        {
            return currentInfo.normalizedTime;
        }

        return 0;
    }

    void TryApplyForce()
    {
        if(_alreadApllyForce) return;

        playerStateMachine.ForceReceiver.AddForce(
            playerStateMachine.transform.forward * _currentAttack.Force
        );

        _alreadApllyForce = true;
    }

    void TryComboAttack(float normalizedTime)
    {
        if(_currentAttack.ComboAttackTime == -1) return;

        if(normalizedTime < _currentAttack.ComboAttackTime) return;

        playerStateMachine.SwitchState(
            new PlayerAttackState(
                playerStateMachine, 
                _currentAttack.ComboStateIndex
            )
        );
    }
}
