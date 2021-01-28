using System.Collections;
using System.Collections.Generic;
using Dead_Earth.Scripts;
using Dead_Earth.Scripts.AI;
using UnityEngine;

public class AIZombieState_Feeding1 : AIZombieState
{
    // Inspector assigned Variables
    [SerializeField] private float _slerpSpeed = 5.0f;
    [SerializeField] private Transform _bloodParticlesMount;
    [SerializeField] [Range(0.01f, 1.0f)] float _bloodParticlesBurstTime = 0.1f ;
    [SerializeField] [Range(1, 100)] private int _bloodParticlesBurstAmount = 10; 
    
    // Private Fields
    private int _eatingStateHash = Animator.StringToHash("Feeding State");
    private int _eatingLayerIndex = -1;
    private float _timer = 0.0f;

    public override AIStateType GetStateType()
    {
        return AIStateType.Feeding;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entering feeding State");
        
        // Base class processing
        base.OnEnterState();
        if (_zombieStateMachine == null)
        {
            return;
        }
        
        // Get layer index
        if (_eatingLayerIndex == -1)
        {
            _eatingLayerIndex = _zombieStateMachine.Animator.GetLayerIndex("Cinematic");
        }
        
        // Reset Blood Particles Timer
        _timer = 0.0f;
        
        // Configure the State Machine's Animator
        _zombieStateMachine.Feeding = true;
        _zombieStateMachine.Seeking = 0;
        _zombieStateMachine.Speed = 0;
        _zombieStateMachine.AttackType = 0;
        
        // Agent updates position but not rotation
        _zombieStateMachine.NavAgentControl(true, false);
    }

    public override void OnExitState()
    {
        if (_zombieStateMachine != null)
        {
            _zombieStateMachine.Feeding = false;
        }
    }

    /// <summary>
    /// The engine of this state
    /// </summary>
    /// <returns> The AIStateType to transition to </returns>
    public override AIStateType OnUpdate()
    {
        _timer += Time.deltaTime; 
        
        if (_zombieStateMachine.Satisfaction > 0.9f)
        {
            _zombieStateMachine.GetWaypointPosition(false);
            return AIStateType.Alerted;
        }
        
        // If Visual Threat then drop into alert mode
        if (_zombieStateMachine.VisualThreat.Type != AITargetType.None &&
            _zombieStateMachine.VisualThreat.Type != AITargetType.Visual_Food)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }
        
        // If Audio Threat then drop into alert mode
        if (_zombieStateMachine.AudioThreat.Type == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }
        
        // Is the feeding animation playing now
        if (_zombieStateMachine.Animator.GetCurrentAnimatorStateInfo(_eatingLayerIndex).shortNameHash == _eatingStateHash)
        {
            _zombieStateMachine.Satisfaction =
                Mathf.Min(_zombieStateMachine.Satisfaction + Time.deltaTime * _zombieStateMachine.ReplenishRate / 100f, 
                          1.0f);
            if (GameSceneManager.Instance && GameSceneManager.Instance.BloodParticles && _bloodParticlesMount)
            {
                if (_timer > _bloodParticlesBurstTime)
                {
                    ParticleSystem system = GameSceneManager.Instance.BloodParticles;
                    var particleSystemTransform = system.transform;
                    var bloodParticlesMountTransform = _bloodParticlesMount.transform;
                    particleSystemTransform.position = bloodParticlesMountTransform.position;
                    particleSystemTransform.rotation = bloodParticlesMountTransform.rotation;
                    var particleSystemMain = system.main;
                    particleSystemMain.simulationSpace = ParticleSystemSimulationSpace.World;
                    system.Emit(_bloodParticlesBurstAmount);
                    _timer = 0.0f;
                }
            }
        }

        if (!_zombieStateMachine.UseRootRotation)
        {
            // Keep the zombie facing the player at all times
            Vector3 targetPosition = _zombieStateMachine.TargetPosition;
            var zombiePosition = _zombieStateMachine.transform.position;
            targetPosition.y = zombiePosition.y;
            Quaternion newRotation = Quaternion.LookRotation(targetPosition - zombiePosition);
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation,
                                                                      newRotation,
                                                                      Time.deltaTime * _slerpSpeed);
        }
        
        
        // Stay in Feeding state
        return AIStateType.Feeding;
    }
}
