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
    public PathDisplayMode DisplayMode = PathDisplayMode.Connection;
    [HideInInspector]
    // Start wayopoint index for Paths mode
    public int UIStart = 0;
    [HideInInspector]
    // End waypoint index for Paths mode
    public int UIEnd = 0;
    
    // List of Transform references
    public List<Transform> Waypoints = new List<Transform>();
    
}
