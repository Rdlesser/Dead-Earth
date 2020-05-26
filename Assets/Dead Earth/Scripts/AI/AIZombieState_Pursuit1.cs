using UnityEngine;
using UnityEngine.AI;

namespace Dead_Earth.Scripts.AI
{
    /// <summary>
    /// A Zombie state used for pursuing a target
    /// </summary>
    public class AIZombieState_Pursuit1 : AIZombieState
    {
        [SerializeField] [Range(0, 10)] private float _speed = 3f;
        [SerializeField] private float _slerpSpeed = 5f;
        [SerializeField] private float _repathDistanceMultiplier = 0.035f;
        [SerializeField] private float _repathVisualMinDuration = 0.05f;
        [SerializeField] private float _repathVisualMaxDuration = 5f;
        [SerializeField] private float _repathAudioMinDuration = 0.25f;
        [SerializeField] private float _repathAudioMaxDuration = 5f;
        [SerializeField] private float _maxDuration = 40f;
        
        // Private fields
        private float _timer;
        private float _repathTimer;

        // Mandatory Overrides
        public override AIStateType GetStateType()
        {
            return AIStateType.Pursuit;
        }
        
        // Default Handlers 
        public override void OnEnterState()
        {
            Debug.Log("Entering Pursuit State");
            
            base.OnEnterState();
            if (_zombieStateMachine == null)
            {
                return;
            }
            
            // Configure State Machine
            _zombieStateMachine.NavAgentControl(true, false);
            _zombieStateMachine.Speed = _speed;
            _zombieStateMachine.Seeking = 0;
            _zombieStateMachine.Feeding = false;
            _zombieStateMachine.AttackType = 0;
            
            // Zombies will only pursue for so long before breaking off
            _timer = 0f;
            _repathTimer = 0f;

            // Set Path
            _zombieStateMachine.NavAgent.SetDestination(_zombieStateMachine.TargetPosition);
            _zombieStateMachine.NavAgent.isStopped = false;
        }
        
        /// <summary>
        /// The Engine of this state
        /// </summary>
        /// <returns> The AIStateType to transition to </returns>
        public override AIStateType OnUpdate()
        {
            _timer += Time.deltaTime;
            _repathTimer += Time.deltaTime;

            if (_timer > _maxDuration)
            {
                return AIStateType.Patrol;
            }
            
            // If we are chasing the player and have entered the melee trigger then attack
            if (_stateMachine.TargetType == AITargetType.Visual_Player && _zombieStateMachine.InMeleeRange)
            {
                return AIStateType.Attack;
            }
            
            // Otherwise this is navigation to areas of interest so use the standard target threshold 
            if (_zombieStateMachine.IsTargetReached)
            {
                switch (_stateMachine.TargetType)
                {
                    // If we have reached the source
                    case AITargetType.Audio:
                    case AITargetType.Visual_Light:
                        _stateMachine.ClearTarget(); // Clear the threat
                        return AIStateType.Alerted; // Become alert and search for targets
                    
                    case AITargetType.Visual_Food:
                        return AIStateType.Feeding;

                }
            }
            
            // If for any reason the nav agent has lost its path then call then drop into alerted state
            // so it will try to re-acquire the target or eventually give up and resume patrolling
            if (_zombieStateMachine.NavAgent.isPathStale || 
                !_zombieStateMachine.NavAgent.hasPath || 
                _zombieStateMachine.NavAgent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                return AIStateType.Alerted;
            }
            
            // If we are close to the target that was a player and we still have the player
            // in our vision then keep facing right at the player
            if (!_zombieStateMachine.UseRootRotation && 
                _zombieStateMachine.TargetType == AITargetType.Visual_Player &&
                _zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Player && 
                _zombieStateMachine.IsTargetReached)
            {
                Vector3 targetPosition = _zombieStateMachine.TargetPosition;
                var zombiePosition = _zombieStateMachine.transform.position;
                targetPosition.y = zombiePosition.y;
                Quaternion newRotation = 
                    Quaternion.LookRotation(targetPosition - zombiePosition);
                _zombieStateMachine.transform.rotation = newRotation;
            }
            // SLowly update our rotation to match the nav agents desired rotation BUT
            // only if we are not persuing the player and are really close to him
            else if (!_stateMachine.UseRootRotation && !_zombieStateMachine.IsTargetReached)
            {
                // Generate a new Quaternion representing the rotation we should have 
                Quaternion newRotation = Quaternion.LookRotation(_zombieStateMachine.NavAgent.desiredVelocity);
                
                // Smoothly rotate to the new rotation over time
                _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, 
                                                                          newRotation, 
                                                                          Time.deltaTime * _slerpSpeed);
            }
            else if (_zombieStateMachine.IsTargetReached)
            {
                return AIStateType.Alerted;
            }
            
            // Do we have a visual threat that is the player
            if (_zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Player)
            {
                // The position is different - maybe same threat but it has moved so re-path periodically 
                if (_zombieStateMachine.TargetPosition != _zombieStateMachine.VisualThreat.Position)
                {
                    // Re-path more frequently as we get closer to the target (try and save some CPU cycles)
                    if (Mathf.Clamp(_zombieStateMachine.VisualThreat.Distance * _repathDistanceMultiplier, 
                                    _repathVisualMinDuration, 
                                    _repathVisualMaxDuration) < _repathTimer)
                    {
                        // Re-path the agent
                        _zombieStateMachine.NavAgent.SetDestination(_zombieStateMachine.VisualThreat.Position);
                        _repathTimer = 0f;
                    }
                }
                
                // Make sure this is the current target
                _stateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                
                // Remain in pursuit state
                return AIStateType.Pursuit;
            }
            
            // If our target is the last sighting of a player then remain
            // in pursuit as nothing else can override
            if (_zombieStateMachine.TargetType == AITargetType.Visual_Player)
            {
                return AIStateType.Pursuit;
            }
            
            // If we have a visual threat that is the player's light
            if (_zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Light)
            {
                // And we currently have a lower priority target the drop into alerted
                // mode and try to find the source of light
                if (_zombieStateMachine.TargetType == AITargetType.Audio || 
                    _zombieStateMachine.TargetType == AITargetType.Visual_Food)
                {
                    _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                    return AIStateType.Alerted; 
                }

                if (_zombieStateMachine.TargetType == AITargetType.Visual_Light)
                {
                    // Get unique ID of the collider of our target
                    int currentID = _zombieStateMachine.TargetColliderID;
                    
                    // If this is the same light
                    if (currentID == _zombieStateMachine.VisualThreat.Collider.GetInstanceID())
                    {
                        // The position is different - maybe same threat but it has moved so re-path periodically
                        if (_zombieStateMachine.TargetPosition != _zombieStateMachine.VisualThreat.Position)
                        {
                            // Re-path more frequently as we get closer to the target (try and save some CPU cycles
                            if (Mathf.Clamp(_zombieStateMachine.VisualThreat.Distance * _repathDistanceMultiplier, 
                                            _repathVisualMinDuration, 
                                            _repathVisualMaxDuration) < _repathTimer)
                            {
                                // Re-path the agent
                                _zombieStateMachine.NavAgent.SetDestination(_zombieStateMachine.VisualThreat.Position);
                                _repathTimer = 0f;
                            }
                        }

                        _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                        return AIStateType.Pursuit;
                    }
                    else
                    {
                        _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                        return AIStateType.Alerted;
                    }
                }
            }
            else if (_zombieStateMachine.AudioThreat.Type == AITargetType.Audio)
            {
                if (_zombieStateMachine.TargetType == AITargetType.Visual_Food)
                {
                    _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                    return AIStateType.Alerted;
                }
                else if (_zombieStateMachine.TargetType == AITargetType.Audio)
                {
                    // Get unique ID of the collider of our target
                    int currentID = _zombieStateMachine.TargetColliderID;
                    
                    // If this is the same light
                    if (currentID == _zombieStateMachine.AudioThreat.Collider.GetInstanceID())
                    {
                        // The position is different - maybe same threat but it has moved so re-path periodically
                        if (_zombieStateMachine.TargetPosition != _zombieStateMachine.AudioThreat.Position)
                        {
                            // Re-path more frequently as we get closer to the target (try and save some CPU cycles
                            if (Mathf.Clamp(_zombieStateMachine.AudioThreat.Distance * _repathDistanceMultiplier, 
                                            _repathAudioMinDuration, 
                                            _repathAudioMaxDuration) < _repathTimer)
                            {
                                // Re-path the agent
                                _zombieStateMachine.NavAgent.SetDestination(_zombieStateMachine.AudioThreat.Position);
                                _repathTimer = 0f;
                            }
                        }

                        _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                        return AIStateType.Pursuit;
                    }
                    else
                    {
                        _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                        return AIStateType.Alerted;
                    }
                }
            }

            // Default
            return AIStateType.Pursuit;
        }
        
    }
}
