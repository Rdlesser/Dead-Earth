using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// ----------------------------------------------------------
// CLASS	:	NavAgentExample
// DESC		:	Behaviour to test Unity's NavMeshAgent
// ----------------------------------------------------------

[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentExample : MonoBehaviour
{
    // Inspector Assigned Variable
    public AIWaypointNetwork waypointNetwork;
    public int currentIndex;
    public bool hasPath;
    public bool pathPending;
    public bool pathStale;
    public NavMeshPathStatus pathStatus = NavMeshPathStatus.PathInvalid;
    public AnimationCurve	 JumpCurve		 = new AnimationCurve();

    // Private Members
    private NavMeshAgent _navAgent;


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
    
        // Calculate how much the current waypoint index needs to be incremented
        int incStep = increment ? 1 : 0;
    
        // Calculate index of next waypoint factoring in the increment with wrap-around and fetch waypoint
        int nextWaypoint = currentIndex + incStep >= waypointNetwork.Waypoints.Count? 0 : currentIndex + incStep;
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
        // Copy NavMeshAgents state into inspector visible variables
        hasPath = _navAgent.hasPath;
        pathPending = _navAgent.pathPending;
        pathStale = _navAgent.isPathStale;
        pathStatus = _navAgent.pathStatus;

        if (_navAgent.isOnOffMeshLink)
        {
            StartCoroutine(Jump(1.0f));
            return;
        }
    
        // If we don't have a path and one isn't pending then set the next
        // waypoint as the target, otherwise if path is stale regenerate path
        if (!hasPath && !pathPending || pathStatus == NavMeshPathStatus.PathInvalid)
        {
            SetnextDestination(true);
        }
        else if (pathStale)
        {
            SetnextDestination(false);
        }
    }

    // ---------------------------------------------------------
    // Name	:	Jump
    // Desc	:	Manual OffMeshLInk traversal using an Animation
    //			Curve to control agent height.
    // ---------------------------------------------------------
    IEnumerator Jump ( float duration )
    {
        // Get the current OffMeshLink data
        OffMeshLinkData data = _navAgent.currentOffMeshLinkData;

        // Start Position is agent current position
        Vector3	startPos = _navAgent.transform.position;

        // End position is fetched from OffMeshLink data and adjusted for baseoffset of agent
        Vector3	endPos = data.endPos + _navAgent.baseOffset * Vector3.up;

        // Used to keep track of time
        float time = 0.0f;

        // Keeo iterating for the passed duration
        while ( time <=  duration )
        {
            // Calculate normalized time
            float t = time / duration;

            // Lerp between start position and end position and adjust height based on evaluation of t on Jump Curve
            _navAgent.transform.position = Vector3.Lerp( startPos, endPos, t ) + 
                                           JumpCurve.Evaluate(t) * Vector3.up ;

            // Accumulate time and yield each frame
            time += Time.deltaTime;
            yield return null;
        }

        // All done so inform the agent it can resume control
        _navAgent.CompleteOffMeshLink();
    }
}

