using UnityEngine;
using UnityEngine.AI;

namespace Dead_Earth.Scripts.AI
{
    /// <summary>
    /// Generic Patrolling Behaviour for a Zombie
    /// </summary>
    public class AIZombieState_Patrol1 : AIZombieState
    {
    
        // Inspector Assigned
        [SerializeField] private float _turnOnSpotThreshold = 80f;
        [SerializeField] private float _slerpSpeed = 5.0f;
    
        [SerializeField] [Range(0f, 3f)] private float _speed = 1f;
    

        /// <summary>
        /// Called by parent State Machine to get this state's <br/>
        /// type
        /// </summary>
        /// <returns> The state type of type (AIStateType) </returns>
        public override AIStateType GetStateType()
        {
            return AIStateType.Patrol;
        }
    
        /// <summary>
        /// Called by the State Machine when first transitioned into <br/>
        /// this state. It initializes a timer and configures the <br/>
        /// state machine
        /// </summary>
        public override void OnEnterState()
        {
            Debug.Log("Entering Patrol State");
            base.OnEnterState();

            if (_zombieStateMachine == null)
            {
                return;
            }

            // Configure State Machine 
            _zombieStateMachine.NavAgentControl(true, false);
            _zombieStateMachine.Seeking = 0;
            _zombieStateMachine.Feeding = false;
            _zombieStateMachine.AttackType = 0;
        
            // Set Destination
            _zombieStateMachine.NavAgent.SetDestination( _zombieStateMachine.GetWaypointPosition( false ) );
        
            // Make sure NavAgent is switched on
            _zombieStateMachine.NavAgent.isStopped = false;

        }

        /// <summary>
        /// Called by the State Machine each frame to give this <br/>
        /// state a time-slice to update itself. It processes <br/>
        /// threats and handles transitions as well as keeping <br/>
        /// the zombie aligned with its proper direction in the <br/>
        /// case where root rotation isn't being used.
        /// </summary>
        /// <returns> AIStateType to transition to at the end of the update </returns>
        public override AIStateType OnUpdate()
        {
            // Do we have a visual threat that is the player
            if (_zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Player)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Pursuit;
            }

            if (_zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Light)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Alerted;
            }
        
            // Sound is the third highest priority
            if (_zombieStateMachine.AudioThreat.Type == AITargetType.Audio)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                return AIStateType.Alerted;
            }
        
            // We have seen a dead body so let's pursue that if we are hungry enough
            if (_zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Food)
            {
                // If the distance to hunger ratio means we are hungry enough to stray off the path that far
                if (1.0f - _zombieStateMachine.Satisfaction > 
                    _zombieStateMachine.VisualThreat.Distance / _zombieStateMachine.SensorRadius)
                {
                    _stateMachine.SetTarget(_stateMachine.VisualThreat);
                    return AIStateType.Pursuit;
                }
            }

            if (_zombieStateMachine.NavAgent.pathPending)
            {
                _zombieStateMachine.Speed = 0;
                return AIStateType.Patrol;
            }
            else
            {
                _zombieStateMachine.Speed = _speed;
            }
            
            // Calculate angle we need to turn through to be facing our target
            var zombieTransform = _zombieStateMachine.transform;
            float angle = Vector3.Angle(zombieTransform.forward,
                                        _zombieStateMachine.NavAgent.steeringTarget - zombieTransform.position);

            // If its too big then drop out of Patrol and into Altered
            if (angle > _turnOnSpotThreshold) 
            {
                return AIStateType.Alerted;
            }

            // If root rotation is not being used then we are responsible for keeping zombie rotated
            // and facing in the right direction. 
            if (!_zombieStateMachine.UseRootRotation)
            {
                // Generate a new Quaternion representing the rotation we should have
                Quaternion newRotation = Quaternion.LookRotation(_zombieStateMachine.NavAgent.desiredVelocity);
            
                // Smoothly rotate to that new rotation over time
                _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation,
                                                                          newRotation,
                                                                          Time.deltaTime * _slerpSpeed);
            }

            // If for any reason the nav agent has lost its path then call the NextWaypoint function
            // so a new waypoint is selected and a new path assigned to the nav agent.
            if (_zombieStateMachine.NavAgent.isPathStale ||
                !_zombieStateMachine.NavAgent.hasPath ||
                _zombieStateMachine.NavAgent.pathStatus != NavMeshPathStatus.PathComplete )
            {
                _zombieStateMachine.NavAgent.SetDestination(_zombieStateMachine.GetWaypointPosition (true));
            }

            // Stay in Patrol State
            return AIStateType.Patrol;
        }

        /// <summary>
        /// Called by the parent StateMachine when the zombie has reached <br/>
        /// its target (entered its target trigger
        /// </summary>
        /// <param name="isReached"> Was the target reached </param>
        public override void OnDestinationReached(bool isReached)
        {
            // Only interesting in processing arrivals not departures
            if (_zombieStateMachine == null || !isReached)
            {
                return;
            }

            // Select the next waypoint in the waypoint network
            if (_zombieStateMachine.TargetType == AITargetType.Waypoint)
            {
                _zombieStateMachine.NavAgent.SetDestination (_zombieStateMachine.GetWaypointPosition (true ));
            }
        }

        /// <summary>
        /// Override IK Goals
        /// </summary>
        // public override void OnAnimatorIKUpdated()
        // {
        //     if (_zombieStateMachine == null)
        //     {
        //         return;
        //     }
        //     
        //     _zombieStateMachine.Animator.SetLookAtPosition(_zombieStateMachine.NavAgent.desiredVelocity + 
        //                                                    Vector3.up);
        //     _zombieStateMachine.Animator.SetLookAtWeight(0.55f);
        // }
    }
}
