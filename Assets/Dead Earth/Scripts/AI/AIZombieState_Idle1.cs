using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
    /// <summary>
    /// An AIState that implements a zombies Idle Behaviour
    /// </summary>
    public class AIZombieState_Idle1 : AIZombieState
    {
        // Inspector Assigned
        [SerializeField] private Vector2 _idleTimeRange = new Vector2(10.0f, 60.0f); 

        // Private
        private float _idleTime;
        private float _timer;
        
        /// <summary>
        /// Returns the type of the state
        /// </summary>
        /// <returns> The type of the state </returns>
        public override AIStateType GetStateType()
        {
            return AIStateType.Idle;
        }

        /// <summary>
        /// Called by the State Machine when first transitioned into <br/>
        /// this state. It initializes a timer and configures the <br/>
        /// state machine
        /// </summary>
        public override void OnEnterState()
        {
            Debug.Log("Entering Idle State");
            base.OnEnterState();

            if (_zombieStateMachine == null)
            {
                return;
            }
            
            // Set Idle Time
            _idleTime = Random.Range(_idleTimeRange.x, _idleTimeRange.y);
            _timer = 0.0f;
            
            // Configure State Machine 
            _zombieStateMachine.NavAgentControl(true, false);
            _zombieStateMachine.Speed = 0;
            _zombieStateMachine.Seeking = 0;
            _zombieStateMachine.Feeding = false;
            _zombieStateMachine.AttackType = 0;
            _zombieStateMachine.ClearTarget();

        }

        /// <summary>
        /// Called by the state machine each frame
        /// </summary>
        /// <returns> The AIStateType to transition to </returns>
        public override AIStateType OnUpdate()
        {
            // No state machine then bail
            if (_zombieStateMachine == null)
            {
                return AIStateType.Idle;
            }

            // Is the player visible
            if (_zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Player)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Pursuit;
            }

            // Is the threat a flashlight
            if (_zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Light)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Alerted;
            }

            // Is the threat an audio emitter
            if (_zombieStateMachine.AudioThreat.Type == AITargetType.Audio)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                return AIStateType.Alerted;
            }

            // Is the threat food
            if (_zombieStateMachine.VisualThreat.Type == AITargetType.Visual_Food)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Pursuit;
            }

            // Update the idle timer
            _timer += Time.deltaTime;

            // Patrol if idle time has been exceeded
            if (_timer > _idleTime)
            {
                Debug.Log("Going into Patrol");
                return AIStateType.Patrol;
            }

            // No state change required
            return AIStateType.Idle;
        }
    }
}
