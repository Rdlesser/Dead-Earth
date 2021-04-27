using System.Collections;
using System.Collections.Generic;
using Dead_Earth.Scripts.AI;
using UnityEngine;

public class AIZombieState_Attack1 : AIZombieState
{

    [SerializeField] [Range(0, 10)] private float _speed = 0f;
    [SerializeField] [Range(0f, 1f)] private float _lookAtWeight = 0.7f;
    [SerializeField] [Range(0f, 90f)] private float _lookAtAngleThreshold = 15.0f;
    [SerializeField] private float _slerpSpeed = 5f;
    
    // Private Variables
    private float _currentLookAtWeight = 0f;
    
    // Mandatory Overrides
    public override AIStateType GetStateType()
    {
        return AIStateType.Attack;
    }

    // Default Handlers
    public override void OnEnterState()
    {
        Debug.Log("Entering Attack State");
        
        base.OnEnterState();
        if (_zombieStateMachine == null)
        {
            return;
        }
        
        // Configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.Seeking = 0;
        _zombieStateMachine.Feeding = false;
        _zombieStateMachine.AttackType = Random.Range(1, 100);
        _zombieStateMachine.Speed = _speed;
        _currentLookAtWeight = 0f;
    }

    public override void OnExitState()
    {
        _zombieStateMachine.AttackType = 0;
    }

    /// <summary>
    /// The Engine of this state
    /// </summary>
    /// <returns> The AIStateType to transition to </returns>
    public override AIStateType OnUpdate()
    {
        Vector3 targetPosition;
        Quaternion newRotation;
        
        // Do we have a visual threat that is the player
        if (_zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Player)
        {
            // Set new target
            _zombieStateMachine.SetTarget(_stateMachine.VisualThreat);
            
            // If we are not in melee range anymore then go back to pursuit mode
            if (!_zombieStateMachine.InMeleeRange)
            {
                return AIStateType.Pursuit;
            }

            if (!_zombieStateMachine.UseRootRotation)
            {
                // Keep the zombie facing the player at all times
                targetPosition = _zombieStateMachine.TargetPosition;
                var zombiePosition = _zombieStateMachine.transform.position;
                targetPosition.y = zombiePosition.y;
                newRotation = Quaternion.LookRotation(targetPosition - zombiePosition);
                _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation,
                                                                          newRotation,
                                                                          Time.deltaTime * _slerpSpeed);
            }

            _zombieStateMachine.AttackType = Random.Range(1, 100);

            return AIStateType.Attack;
        }
        
        // Player has stepped outside of FOV or hidden so face in his/her direction and then
        // drop back to Alerted mode to give the AI a chance to re-acquire target
        if (!_zombieStateMachine.UseRootRotation)
        {
            targetPosition = _zombieStateMachine.TargetPosition;
            var zombieTransform = _zombieStateMachine.transform;
            var zombieTransformPosition = zombieTransform.position;
            targetPosition.y = zombieTransformPosition.y;
            newRotation = Quaternion.LookRotation(targetPosition - zombieTransformPosition);
            _zombieStateMachine.transform.rotation = newRotation;
        }

        return AIStateType.Alerted;
    }

    /// <summary>
    /// Override IK Goals
    /// </summary>
    public override void OnAnimatorIKUpdated()
    {
        if (_zombieStateMachine == null)
        {
            return;
        }

        if (Vector3.Angle(_zombieStateMachine.transform.forward, 
                          _zombieStateMachine.TargetPosition - _zombieStateMachine.transform.position) < _lookAtAngleThreshold)
        {
            _zombieStateMachine.Animator.SetLookAtPosition(_zombieStateMachine.TargetPosition + Vector3.up);
            _currentLookAtWeight = Mathf.Lerp(_currentLookAtWeight, _lookAtWeight, Time.deltaTime);
            _zombieStateMachine.Animator.SetLookAtWeight(_currentLookAtWeight);
        }
        else
        {
            _currentLookAtWeight = Mathf.Lerp(_currentLookAtWeight, 0f, Time.deltaTime);
            _zombieStateMachine.Animator.SetLookAtWeight(_currentLookAtWeight);
        }
    }
}
