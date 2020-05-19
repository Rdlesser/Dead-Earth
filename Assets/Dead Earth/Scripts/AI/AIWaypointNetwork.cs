using System.Collections.Generic;
using UnityEngine;

// Display Mode that the Custom Inspector of an AIWaypointNetwork
// component can be in
public enum PathDisplayMode {None, Connection, Paths}

// -------------------------------------------------------------------
// CLASS	:	AIWaypointNetwork
// DESC		:	Contains a list of waypoints. Each waypoint is a 
//				reference to a transform. Also contains settings
//				for the Custom Inspector
// ------------------------------------------------------------------
public class AIWaypointNetwork : MonoBehaviour
{
    [HideInInspector]
    // Current Display Mode
    public PathDisplayMode displayMode = PathDisplayMode.Connection;
    [HideInInspector]
    // Start waypoint index for Paths mode
    public int uiStart;
    [HideInInspector]
    // End waypoint index for Paths mode
    public int uiEnd;
    
    // List of Transform references
    public List<Transform> Waypoints = new List<Transform>();
    
}
