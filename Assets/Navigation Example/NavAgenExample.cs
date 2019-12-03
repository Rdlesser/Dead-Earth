using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// ----------------------------------------------------------
// CLASS	:	NavAgentExample
// DESC		:	Behaviour to test Unity's NavMeshAgent
// ----------------------------------------------------------
[RequireComponent(typeof(NavMeshAgent))]
public class NavAgenExample : MonoBehaviour
{
    // Inspector Assigned Variable
    public AIWaypointNetwork WaypointNetwork = null;
    public int CurrentIndex = 2;
    public bool HasPath = false;
    public bool PathPending = false;
    public bool PathStale = false;
    public NavMeshPathStatus PathStatus = NavMeshPathStatus.PathInvalid;

    // Private Members
    private NavMeshAgent _navAgent = null;

    
    // -----------------------------------------------------
    // Name :	Start
    // Desc	:	Cache MavMeshAgent and set initial 
    //			destination.
    // -----------------------------------------------------
    void Start()
    {
        // Cash Nav Mesh Reference
        _navAgent = GetComponent<NavMeshAgent>();
        
        // Turn off auto-update
        /*_navAgent.updatePosition = false;
        _navAgent.updateRotation = false;*/

        // If not valid Waypoint Network has been assigned then return
        if (WaypointNetwork == null)
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
        if (!WaypointNetwork)
        {
            return;
        }
        
        // Calculatehow much the current waypoint index needs to be incremented
        int incStep = increment ? 1 : 0;
        
        // Calculate index of next waypoint factoring in the increment with wrap-around and fetch waypoint
        int nextWaypoint = (CurrentIndex+incStep>=WaypointNetwork.Waypoints.Count)?0:CurrentIndex+incStep;
        Transform nextWaypointTransform =  WaypointNetwork.Waypoints[nextWaypoint];

        if (nextWaypointTransform != null)
        {
            // Update the current waypoint index, assign its position as the NavMeshAgents
            // Destination and then return
            CurrentIndex = nextWaypoint;
            _navAgent.destination = nextWaypointTransform.position;
            return;
        }

        // We did not find a valid waypoint in the list for this iteration
        CurrentIndex++;
    }

    // ---------------------------------------------------------
    // Name	:	Update
    // Desc	:	Called each frame by Unity
    // ---------------------------------------------------------
    void Update()
    {
        // Copy NavMeshAgents state into inspector visible variables
        HasPath = _navAgent.hasPath;
        PathPending = _navAgent.pathPending;
        PathStale = _navAgent.isPathStale;
        PathStatus = _navAgent.pathStatus;

        if (_navAgent.isOnOffMeshLink)
        {
            StartCoroutine(Jump(20.0f));
            return;
        }
        
        // If we don't have a path and one isn't pending then set the next
        // waypoint as the target, otherwise if path is stale regenerate path
        if ((!HasPath && !PathPending) || PathStatus == NavMeshPathStatus.PathInvalid)
        {
            SetnextDestination(true);
        }
        else if (PathStale)
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
