using System;
using System.Collections.Generic;
using Dead_Earth.Scripts.AI.State_Machine_Behaviours;
using UnityEngine;
using UnityEngine.AI;

namespace Dead_Earth.Scripts.AI
{

    // Public Enums of the AI System
    public enum AIStateType
    {
        None,
        Idle,
        Alerted,
        Patrol,
        Attack, 
        Feeding,
        Pursuit,
        Dead
    }

    public enum AITargetType
    {
        None,
        Waypoint,
        Visual_Player,
        Visual_Light,
        Visual_Food,
        Audio
    }

    public enum AITriggerEventType
    {
        Enter,
        Stay,
        Exit
    }

    /// <summary>
    /// Describes a potential target to the AI System
    /// </summary>
    public struct AITarget
    {
        // The type of target
        private AITargetType _type;
        // The target's collider
        private Collider _collider;
        // Current position in the world
        private Vector3 _position;
        // Distance from player
        private float _distance;
        // Time the target was last ping'd
        private float _time;
        
        public AITargetType type => _type;
        public Collider collider => _collider;
        public Vector3 position => _position;
        public float distance
        {
            get => _distance;
            set => _distance = value;
        }
        public float time => _time;

        public void Set(AITargetType t, Collider c, Vector3 p, float d)
        {
            _type = t;
            _collider = c;
            _position = p;
            _distance = d;
            _time = Time.time;
        }

        public void Clear()
        {
            _type = AITargetType.None;
            _collider = null;
            _position = Vector3.zero;
            _time = 0.0f;
            _distance = Mathf.Infinity;
        }
    }
    
    /// <summary>
    /// Base class for all AI State Machines
    /// </summary>
    public abstract class AIStateMachine : MonoBehaviour
    {
        
        // Public
        public AITarget _visualThreat;
        public AITarget _audioThreat;
        
        // Protected
        protected AIState _currentState;
        protected Dictionary<AIStateType, AIState> _states = new Dictionary<AIStateType, AIState>();
        protected AITarget _target;
        protected int _rootPositionRefCount;
        protected int _rootRotationRefCount;

        // Protected Inspector Assigned
        [SerializeField] protected AIStateType _currentStateType = AIStateType.Idle;
        [SerializeField] protected SphereCollider _targetTrigger;
        [SerializeField] protected SphereCollider _sensorTrigger;
        
        [SerializeField] [Range(0, 15)] protected float _stoppingDistance = 1.0f;

        // Component Cache
        protected Animator _animator;
        protected NavMeshAgent _navAgent;
        protected Collider _collider;
        protected Transform _transform;
        
        // Public properties
        public Animator Animator => _animator;
        public NavMeshAgent NavAgent => _navAgent;
        public Vector3 SensorPosition
        {
            get
            {
                if (_sensorTrigger == null)
                {
                    return Vector3.zero;
                }

                var sensorTransform = _sensorTrigger.transform;
                var sensorCenter = _sensorTrigger.center;
                var sensorLossyScale = sensorTransform.lossyScale;
                
                Vector3 point = sensorTransform.position;
                point.x += sensorCenter.x * sensorLossyScale.x;
                point.y += sensorCenter.y * sensorLossyScale.y;
                point.z += sensorCenter.z * sensorLossyScale.z;

                return point;

            }
        }

        public float SensorRadius
        {
            get
            {
                if (_sensorTrigger == null)
                {
                    return 0.0f;
                }

                var sensorRadius = _sensorTrigger.radius;
                var sensorLossyScale = _sensorTrigger.transform.lossyScale;
                float radius = Mathf.Max(sensorRadius * sensorLossyScale.x,
                                         sensorRadius * sensorLossyScale.y);

                return Mathf.Max(radius, _sensorTrigger.radius * sensorLossyScale.z);
            }
        }

        public bool UseRootPosition => _rootPositionRefCount > 0;
        public bool UseRootRotation => _rootRotationRefCount > 0;


        /// <summary>
        /// Cache Components
        /// </summary>
        protected virtual void Awake()
        {
            // Cache all frequently accessed components
            _transform = transform;
            _animator = GetComponent<Animator>();
            _navAgent = GetComponent<NavMeshAgent>();
            _collider = GetComponent<Collider>();
            
            // Do we have a valid Game Scene Manager
            if (GameSceneManager.Instance != null)
            {
                // Register State Machines with Scene Database
                if (_collider)
                {
                    GameSceneManager.Instance.RegisterStateMachine(_collider.GetInstanceID(),
                                                                   this); 
                }

                if (_sensorTrigger)
                {
                    GameSceneManager.Instance.RegisterStateMachine(_sensorTrigger.GetInstanceID(), 
                                                                   this);
                }
            }
        }

        /// <summary>
        /// Called by Unity prior to first update to setup the object
        /// </summary>
        protected virtual void Start()
        {
            // Set the sensor trigger's parent to this state machine
            if (_sensorTrigger != null)
            {
                AISensor script = _sensorTrigger.GetComponent<AISensor>();
                if (script != null)
                {
                    script.ParentStateMachine = this;
                }
            }
            
            // Fetch all states on this game object
            AIState[] states = GetComponents<AIState>();

            // Loop through all states and add them to the state dictionary
            foreach (AIState state in states)
            {
                if (state != null && !_states.ContainsKey(state.GetStateType()))
                {
                    // Add this tate to the state dictionary 
                    _states[state.GetStateType()] = state;
                    state.SetStateMachine(this);
                }
            }

            // Set the current state
            if (_states.ContainsKey(_currentStateType))
            {
                _currentState = _states[_currentStateType];
                _currentState.OnEnterState();
            }
            else
            {
                _currentState = null;
            }

            // Fetch all AIStateMachineLink derived behaviours from the animator
            // and set their State Machine references to this state machine
            if (_animator)
            {
                AIStateMachineLink[] scripts = _animator.GetBehaviours<AIStateMachineLink>();
                foreach (AIStateMachineLink script in scripts)
                {
                    script.StateMachine = this;
                }
            }
        }

        /// <summary>
        /// (Overload) Sets the current target and configures the target trigger
        /// </summary>
        /// <param name="t"> Target type </param>
        /// <param name="c"> Target Collider </param>
        /// <param name="p"> Target Position </param>
        /// <param name="d"> Target Distance </param>
        public void SetTarget(AITargetType t, Collider c, Vector3 p, float d)
        {
            // Set the target info
            _target.Set(t, c, p, d);

            // Configure and enable the target trigger at the correct 
            // position with the correct radius    
            if (_targetTrigger != null)
            {
                _targetTrigger.radius = _stoppingDistance;
                _targetTrigger.transform.position = _target.position;
                _targetTrigger.enabled = true;
            }
        }

        /// <summary>
        /// (Overload) Sets the current target and configures the target trigger. <br/>
        /// This method allows for specifying a custom stopping distance 
        /// </summary>
        /// <param name="t"> Target Type </param>
        /// <param name="c"> Target Collider </param>
        /// <param name="p"> Target Position </param>
        /// <param name="d"> Target Distance </param>
        /// <param name="s"> Stopping Distance </param>
        public void SetTarget(AITargetType t, Collider c, Vector3 p, float d, float s)
        {
            // Set the target data
            _target.Set(t, c, p, d);

            // Configure and enable the target trigger at the correct 
            // position with the correct radius   
            if (_targetTrigger != null)
            {
                _targetTrigger.radius = s;
                _targetTrigger.transform.position = _target.position;
                _targetTrigger.enabled = true;
            }
        }

        /// <summary>
        /// (Overload) Sets the current target and configures the target trigger
        /// </summary>
        /// <param name="t"> The new target </param>
        public void SetTarget(AITarget t)
        {
            // Assign the new target 
            _target = t;

            // Configure and enable the target trigger at the correct 
            // position with the correct radius 
            if (_targetTrigger != null)
            {
                _targetTrigger.radius = _stoppingDistance;
                _targetTrigger.transform.position = t.position;
                _targetTrigger.enabled = true;
            }
        }

        /// <summary>
        /// Clears the current target
        /// </summary>
        public void ClearTarget()
        {
            _target.Clear();

            if (_targetTrigger!=null)
            {
                _targetTrigger.enabled = false;
            }
        }

        /// <summary>
        /// Called by Unity with each tick of the Physics system. <br/>
        /// It Clears the audio and visual threats each update and <br/>
        /// re-calculates the distance to the current target
        /// </summary>
        protected virtual void FixedUpdate()
        {
            _visualThreat.Clear();
            _audioThreat.Clear();

            if (_target.type != AITargetType.None)
            {
                _target.distance = Vector3.Distance(transform.position, _target.position); 
            }
        }

        /// <summary>
        /// Called by Unity each frame. Gives the current state a <br/>
        /// chance to update itself and perform transitions 
        /// </summary>
        protected virtual void Update()
        {
            if (_currentState == null)
            {
                return;
            }

            AIStateType newStateType = _currentState.OnUpdate();

            if (newStateType != _currentStateType)
            {
                AIState newState;
                if (_states.TryGetValue(newStateType, out newState))
                {
                    _currentState.OnExitState();
                    newState.OnEnterState();
                    _currentState = newState;
                }
                else if (_states.TryGetValue(AIStateType.Idle, out newState))
                {
                    _currentState.OnExitState();
                    newState.OnEnterState();
                    _currentState = newState;
                }

                _currentStateType = newStateType;
            }
        }

        /// <summary>
        /// Called by Physics system when the AI's Main collider enters
        /// its trigger. <br/> This allows the child state to know when it has
        /// entered the sphere of influence of a waypoint or last player
        /// sighted position
        /// </summary>
        /// <param name="other"> The trigger collider entered </param>
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (_targetTrigger == null || other != _targetTrigger)
            {
                return;
            }
            
            // Notify Child State
            if (_currentState)
            {
                _currentState.OnDestinationReached(true);
            }
        }

        /// <summary>
        /// Informs the Child State that the AI entity is no longer at its destination <br/>
        /// (typically true when a new target has been set by the child).
        /// </summary>
        /// <param name="other"> The trigger collider entered </param>
        public virtual void OnTriggerExit(Collider other)
        {
            if (_targetTrigger == null || other != _targetTrigger)
            {
                return;
            }
            
            // Notify Child State
            if (_currentState)
            {
                _currentState.OnDestinationReached(false);
            }
        }

        /// <summary>
        /// Called by our AISensor component when an AI Aggravator <br/>
        /// has entered/exited the sensor trigger.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="other"></param>
        public virtual void OnTriggerEvent(AITriggerEventType type, Collider other)
        {
            if (_currentState != null)
            {
                _currentState.OnTriggerEvent(type, other);
            }
        }

        /// <summary>
        /// Called by Unity after root motion has been <br/>
        /// evaluated but not applied to the object. <br/>
        /// This allows us to determine via code what to do <br/>
        /// with the root motion information
        /// </summary>
        protected virtual void OnAnimatorMove()
        {
            if (_currentState != null)
            {
                _currentState.OnAnimatorUpdated();
            }
        }

        /// <summary>
        /// Called by Unity just prior to the IK system being <br/>
        /// updated giving us a chance to setup up IK Targets <br/>
        /// and weights.
        /// </summary>
        /// <param name="layerIndex"> The layer index </param>
        protected virtual void OnAnimatorIK(int layerIndex)
        {
            if (_currentState != null)
            {
                _currentState.OnAnimatorIKUpdated();
            }
        }

        /// <summary>
        /// Configure the NavMeshAgent to enable/disable auto <br/>
        /// updates of position/rotation to our transform <br/>
        /// </summary>
        /// <param name="positionUpdate"> Should the nav agent update the position </param>
        /// <param name="rotationUpdate"> Should the nav agent update the rotation </param>
        public void NavAgentControl(bool positionUpdate, bool rotationUpdate)
        {
            if (_navAgent) 
            {
                _navAgent.updatePosition = positionUpdate;
                _navAgent.updateRotation = rotationUpdate;
            }
        }

        /// <summary>
        /// Called by the State Machine behaviours to <br/>
        /// enable / disable root motion
        /// </summary>
        /// <param name="rootPosition"> The change in root position </param>
        /// <param name="rootRotation"> The change in root rotation </param>
        public void AddRootMotionRequest(int rootPosition, int rootRotation)
        {
            _rootPositionRefCount += rootPosition;
            _rootRotationRefCount += rootRotation;
        }

    }
}
