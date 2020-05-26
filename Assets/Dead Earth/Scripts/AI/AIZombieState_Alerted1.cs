using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
    public class AIZombieState_Alerted1 : AIZombieState
    {
        // Inspector Assigned
        [SerializeField] [Range(1f, 60f)] private float _maxDuration = 10f;
        [SerializeField] private float _waypointAngleThreshold = 90f;
        [SerializeField] private float _threatAngleThreshold = 10f;
        [SerializeField] float	_directionChangeTime	=	1.5f;

        // Private Fields
        private float _timer;
        float   _directionChangeTimer;
        
        /// <summary>
        /// Returns the type of the state
        /// </summary>
        /// <returns> AIStateType representing the state type </returns>
        public override AIStateType GetStateType()
        {
            return AIStateType.Alerted;
        }

        public override void OnEnterState()
        {
            Debug.Log("Entering Alerted State");
            base.OnEnterState();

            if (_zombieStateMachine == null)
            {
                return;
            }
            
            
            // Configure State Machine 
            _zombieStateMachine.NavAgentControl(true, false);
            _zombieStateMachine.Speed = 0;
            _zombieStateMachine.Seeking = 0;
            _zombieStateMachine.Feeding = false;
            _zombieStateMachine.AttackType = 0;

            _timer = _maxDuration;
            _directionChangeTimer = 0.0f;
        }

        /// <summary>
        /// The Engine of this state
        /// </summary>
        /// <returns> The AIStateType to transition to </returns>
        public override AIStateType OnUpdate()
        {
            // Reduce Timer
            _timer -= Time.deltaTime;
            _directionChangeTimer += Time.deltaTime;

			// Transition into a patrol state if available
            if (_timer <= 0f)
            {
                _zombieStateMachine.NavAgent.SetDestination(_zombieStateMachine.GetWaypointPosition (false));
                _zombieStateMachine.NavAgent.isStopped = false;
                _timer = _maxDuration;
            }
            
			// Do we have a visual threat that is the player. These take priority over audio threats
            if (_zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Player)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Pursuit;
            }

            // Is the threat an audio emitter
            if (_zombieStateMachine.AudioThreat.Type == AITargetType.Audio)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                _timer = _maxDuration;
            }
            
            // Is the threat a flashlight
            if (_zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Light)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                _timer = _maxDuration;
            }

			// Is the threat food
            if (_zombieStateMachine.AudioThreat.Type == AITargetType.None &&
                _zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Food)
            {
                _zombieStateMachine.SetTarget(_stateMachine.VisualThreat);
                return AIStateType.Pursuit;
            }

            float angle;

            if ((_zombieStateMachine.TargetType == AITargetType.Audio || 
                _zombieStateMachine.TargetType == AITargetType.Visual_Light) &&
                !_zombieStateMachine.IsTargetReached)
            {
                var zombieTransform = _zombieStateMachine.transform;
                angle = FindSignedAngle(zombieTransform.forward,
                                                _zombieStateMachine.TargetPosition - zombieTransform.position);
                
                if (_zombieStateMachine.TargetType == AITargetType.Audio && 
                    Mathf.Abs(angle) < _threatAngleThreshold)
                {
                    return AIStateType.Pursuit;
                }

                if (_directionChangeTimer > _directionChangeTime)
                {
                    if (Random.value < _zombieStateMachine.Intelligence)
                    {
                        _zombieStateMachine.Seeking = (int) Mathf.Sign(angle);
                    }
                    else
                    {
                        _zombieStateMachine.Seeking = (int) Mathf.Sign(Random.Range(-1f, 1f)); 
                    }
                    
                    _directionChangeTimer = 0f;
                }

            }
            else if (_zombieStateMachine.TargetType == AITargetType.Waypoint && !_zombieStateMachine.NavAgent.pathPending)
            {
                var zombieTransform = _zombieStateMachine.transform;
                angle = FindSignedAngle(zombieTransform.forward,
                                        _zombieStateMachine.NavAgent.steeringTarget - zombieTransform.position);

                if (Mathf.Abs(angle) < _waypointAngleThreshold)
                {
                    return AIStateType.Patrol;
                }

                if (_directionChangeTimer > _directionChangeTime)
                {
                    _zombieStateMachine.Seeking = (int) Mathf.Sign(angle);
                    _directionChangeTimer = 0.0f;
                }
            }
            
            return AIStateType.Alerted;
            
        }
        
    }
}
