using System.Collections;
using System.Collections.Generic;
using Dead_Earth.Scripts.AI;
using Dead_Earth.Scripts.AI.State_Machine_Behaviours;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Generic Patrolling Behaviour for a Zombie
/// </summary>
public class AIZombieState_Patrol1 : AIZombieState
{
    
    // Inspector Assigned
    [SerializeField] private AIWaypointNetwork _waypointNetwork;
    [SerializeField] private bool _randomPatrol;
    [SerializeField] private int _currentWaypoint;
    [SerializeField] private float _turnOnSpotThreashold = 80f;
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
        _zombieStateMachine.Speed = _speed;
        _zombieStateMachine.Seeking = 0;
        _zombieStateMachine.Feeding = false;
        _zombieStateMachine.AttackType = 0;
        
        // If the current target is not a waypoint then we need to select
        // a waypoint from the waypoint network and make this the new target
        // and plot a path to it
        if (_zombieStateMachine.TargetType != AITargetType.Waypoint)
        {
            // Clear any previous target
            _zombieStateMachine.ClearTarget();

            // Do we have a valid waypoint network
            if (_waypointNetwork != null && _waypointNetwork.Waypoints.Count > 0)
            {
                // if this is a random patrol then set current waypoint to a random
                // waypoint index
                if (_randomPatrol)
                {
                    _currentWaypoint = Random.Range(0, _waypointNetwork.Waypoints.Count);
                }

                // If its a valid index then fetch the waypoint and make the new target
                if (_currentWaypoint < _waypointNetwork.Waypoints.Count)
                {
                    Transform waypoint = _waypointNetwork.Waypoints[_currentWaypoint];
                    if (waypoint != null)
                    {
                        // This is the new state machines target
                        var position = waypoint.position;
                        _zombieStateMachine.SetTarget(AITargetType.Waypoint,
                                                      null,
                                                      position,
                                                      Vector3.Distance(_zombieStateMachine.transform.position,
                                                                       position));
                        
                        // Tell NavAgent to make a path to this waypoint
                        _zombieStateMachine.NavAgent.SetDestination(position);
                       
                    }
                }
            }
        }
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
        if (_zombieStateMachine.VisualThreat.Type == AITargetType.Audio)
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

        // Calculate angle we need to turn through to be facing our target
        var zombieTransform = _zombieStateMachine.transform;
        float angle = Vector3.Angle(zombieTransform.forward,
                                              _zombieStateMachine.NavAgent.steeringTarget - zombieTransform.position);

        // If its too big then drop out of Patrol and into Alerted
        if (Mathf.Abs(angle) > _turnOnSpotThreashold)
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
            NextWaypoint();
        }

        // Stay in Patrol State
        return AIStateType.Patrol;
    }

    /// <summary>
    /// Called to select a new waypoint. Either randomly selects a new <br/>
    /// waypoint from the waypoint network or increments the current <br/>
    /// waypoint index (with wrap-around) to visit the waypoints in <br/>
    /// the network in sequence. Sets the new waypoint as the the <br/>
    /// target and generates a nav agent path for it
    /// </summary>
    private void NextWaypoint()
    {

        // Increase the current waypoint with wrap-around to zero (or choose a random waypoint)
        if (_randomPatrol && _waypointNetwork.Waypoints.Count > 1)
        {
            // Keep generating random waypoint until we find one that isn't the current one
            // NOTE: Very important that waypoint networks do not only have one waypoint :)
            int oldWaypoint = _currentWaypoint;
            while (_currentWaypoint == oldWaypoint)
            {
                _currentWaypoint = Random.Range(0, _waypointNetwork.Waypoints.Count);
            }
        }
        else
        {
            _currentWaypoint = (_currentWaypoint + 1) % _waypointNetwork.Waypoints.Count;
        }

        // Fetch the new waypoint from the waypoint list
        if (_waypointNetwork.Waypoints[_currentWaypoint] != null)
        {
            Transform newWaypoint = _waypointNetwork.Waypoints[_currentWaypoint];
            
            // This is our new target position
            var newWaypointPosition = newWaypoint.position;
            _zombieStateMachine.SetTarget(AITargetType.Waypoint,
                                          null,
                                          newWaypointPosition,
                                          Vector3.Distance(newWaypointPosition, 
                                                           _zombieStateMachine.transform.position));
            
            // Set new Path
            _zombieStateMachine.NavAgent.SetDestination(newWaypointPosition);
        }
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
            NextWaypoint();
        }
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
        
        _zombieStateMachine.Animator.SetLookAtPosition(_zombieStateMachine.TargetPosition + Vector3.up);
        _zombieStateMachine.Animator.SetLookAtWeight(0.55f);
    }
}
