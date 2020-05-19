﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// ----------------------------------------------------------
// CLASS	:	NavAgenNoRootMotion
// DESC		:	Behaviour to test Unity's NavMeshAgent
// ----------------------------------------------------------
[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentNoRootMotion : MonoBehaviour
{
    // Inspector Assigned Variable
    public AIWaypointNetwork waypointNetwork;
    public int currentIndex = 2;
    public bool hasPath;
    public bool pathPending;
    public bool pathStale;
    public NavMeshPathStatus pathStatus = NavMeshPathStatus.PathInvalid;

    // Private Members
    private NavMeshAgent _navAgent;
    private Animator _animator;
    private float _originalMaxSpeed;
    private static readonly int Horizontal = Animator.StringToHash("Horizontal");
    private static readonly int Vertical = Animator.StringToHash("Vertical");
    private static readonly int TurnOnSpot = Animator.StringToHash("TurnOnSpot");


    // -----------------------------------------------------
    // Name :	Start
    // Desc	:	Cache MavMeshAgent and set initial 
    //			destination.
    // -----------------------------------------------------
    void Start()
    {
        // Cash Nav Mesh Reference
        _navAgent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        if (_navAgent)
        {
            _originalMaxSpeed = _navAgent.speed;
        }

        // Turn off auto-update
        /*_navAgent.updatePosition = false;
        _navAgent.updateRotation = false;*/

        // If not valid Waypoint Network has been assigned then return
        if (waypointNetwork == null)
        {
            return;
        }
        SetnextDestination(false);
    }

    // -----------------------------------------------------
    // Name	:	SetNextDestination
    // Desc	:	Optionally increments the current waypoint
    //			index and then sets the next destination
    //			for the agent to head towards.
    // -----------------------------------------------------
    void SetnextDestination(bool increment)
    {
        // If no network return
        if (!waypointNetwork)
        {
            return;
        }
        
        // Calculatehow much the current waypoint index needs to be incremented
        int incStep = increment ? 1 : 0;
        
        // Calculate index of next waypoint factoring in the increment with wrap-around and fetch waypoint
        int nextWaypoint = (currentIndex+incStep>=waypointNetwork.Waypoints.Count)?0:currentIndex+incStep;
        Transform nextWaypointTransform =  waypointNetwork.Waypoints[nextWaypoint];

        if (nextWaypointTransform != null)
        {
            // Update the current waypoint index, assign its position as the NavMeshAgents
            // Destination and then return
            currentIndex = nextWaypoint;
            _navAgent.destination = nextWaypointTransform.position;
            return;
        }

        // We did not find a valid waypoint in the list for this iteration
        currentIndex++;
    }

    // ---------------------------------------------------------
    // Name	:	Update
    // Desc	:	Called each frame by Unity
    // ---------------------------------------------------------
    void Update()
    {
        int turnOnSpot;
        
        // Copy NavMeshAgents state into inspector visible variables
        hasPath = _navAgent.hasPath;
        pathPending = _navAgent.pathPending;
        pathStale = _navAgent.isPathStale;
        pathStatus = _navAgent.pathStatus;

        // If agent is on an off mesh link
        // if (_navAgent.isOnOffMeshLink)
        // {
        //     StartCoroutine(Jump(20.0f));
        //     return;
        // }

        var forward = transform.forward;
        Vector3 cross = Vector3.Cross(forward, _navAgent.desiredVelocity.normalized);
        float horizontal = cross.y < 0 ? -cross.magnitude : cross.magnitude;
        horizontal = Mathf.Clamp(horizontal * 2.32f, -2.32f, 2.32f);
        float angle = Vector3.Angle(_navAgent.desiredVelocity, forward);
        if (_navAgent.desiredVelocity.magnitude < 1.0f &&
            (angle > 10 || angle < 1))
        {
            _navAgent.speed = 0.1f;
            turnOnSpot = (int) Mathf.Sign(horizontal);


        }
        else
        {
            _navAgent.speed = _originalMaxSpeed;
            turnOnSpot = 0;
        }
        
        _animator.SetFloat(Horizontal, horizontal, 0.1f, Time.deltaTime);
        _animator.SetFloat(Vertical, _navAgent.desiredVelocity.magnitude, 0.1f, Time.deltaTime);
        _animator.SetInteger(TurnOnSpot, turnOnSpot);
        
        // If we don't have a path and one isn't pending then set the next
        // waypoint as the target, otherwise if path is stale regenerate path
        if ((!hasPath && !pathPending) || pathStatus == NavMeshPathStatus.PathInvalid)
        {
            SetnextDestination(true);
        }
        else if (pathStale)
        {
            SetnextDestination(false);
        }
    }

    IEnumerator Jump(float duration)
    {
        OffMeshLinkData data = _navAgent.currentOffMeshLinkData;
        Vector3 startPos = _navAgent.transform.position;
        Vector3 endPos = data.endPos + _navAgent.baseOffset * Vector3.up;
        float time = 0;

        while (time <= duration)
        {
            float t = time / duration;
            _navAgent.transform.position = Vector3.Lerp(startPos, endPos, t);
            time += Time.deltaTime;
            yield return null;
        }
        _navAgent.CompleteOffMeshLink();
    }
}