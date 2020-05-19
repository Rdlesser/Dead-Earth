using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// ------------------------------------------------------------------------------------
// CLASS	:	AIWaypointNetworkEditor
// DESC		:	Custom Inspector and Scene View Rendering for the AIWaypointNetwork
//				Component
// ------------------------------------------------------------------------------------

[CustomEditor(typeof(AIWaypointNetwork))]
public class AIWaypointNetworkEditor : Editor
{
    // --------------------------------------------------------------------------------
    // Name	:	OnInspectorGUI (Override)
    // Desc	:	Called by Unity Editor when the Inspector needs repainting for an
    //			AIWaypointNetwork Component
    // --------------------------------------------------------------------------------\
    
    public override void OnInspectorGUI()
    {
        // Get reference to selected component
        AIWaypointNetwork network = (AIWaypointNetwork) target;

        // Show the Display Mode Enumeration Selector
        network.displayMode = (PathDisplayMode) EditorGUILayout.EnumPopup("Display Mode", network.displayMode);

        // If we are in Paths display mode then display the integer sliders for the Start and End waypoint indices
        if (network.displayMode == PathDisplayMode.Paths)
        {
            network.uiStart = EditorGUILayout.IntSlider("UIStart",
                                                        network.uiStart, 
                                                        0, 
                                                    network.Waypoints.Count - 1);
            network.uiEnd = EditorGUILayout.IntSlider("UIEnd",
                                                      network.uiEnd, 
                                                      0, 
                                                      network.Waypoints.Count - 1);
        }
        
        // Tell Unity to do its default drawing of all serialized members that are NOT hidden in the inspector
        DrawDefaultInspector();
        
        if(GUI.changed)
            // If the GUI has changed - Repaint the views (no need for mouse over "scene" view
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
    
    // --------------------------------------------------------------------------------
    // Name	:	OnSceneGUI
    // Desc	:	Implementing this functions means the Unity Editor will call it when
    //			the Scene View is being repainted. This gives us a hook to do our
    //			own rendering to the scene view.
    // --------------------------------------------------------------------------------
    private void OnSceneGUI()
    {
        // Get a reference to the component being rendered
        AIWaypointNetwork network = (AIWaypointNetwork) target;
        
        // Cahnge the text color to white
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;

        // Fetch all waypoints from the network and render a label for each one
        for (int i = 0; i < network.Waypoints.Count; i++)
        {
            if (network.Waypoints[i] != null)
            {
                Handles.Label(network.Waypoints[i].position, "Waypoint " + i.ToString(), style);
            }
            
            
        }

        // If we are in connections mode then we will to draw lines
        // connecting all waypoints
        if (network.displayMode == PathDisplayMode.Connection)
        {
            // Allocate array of vector to store the polyline positions
            Vector3[] linePoints = new Vector3[network.Waypoints.Count + 1];
            
            for (int i = 0; i <= network.Waypoints.Count && network.Waypoints.Count > 0; i++)
            {
                if (network.Waypoints[i % network.Waypoints.Count] == null)
                {
                    linePoints[i] = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                }
                else
                {
                    linePoints[i] = network.Waypoints[i % network.Waypoints.Count].position;
                }
            }
            
            // Set the Handle color to Cyan
            Handles.color = Color.cyan;
            // Render the polyline in the scene view by passing in our list of waypoint positions
            Handles.DrawPolyLine(linePoints);
        }
        // We are in paths mode so to proper navmesh path search and render result
        else if (network.displayMode == PathDisplayMode.Paths)
        {
            // Allocate a new NavMeshPath
            NavMeshPath path = new NavMeshPath();

            // Assuming both the start and end waypoint indices selected are ligit
            if (network.Waypoints[network.uiStart] != null &&
                network.Waypoints[network.uiEnd] != null)
            {
                // Fetch their positions from the waypoint network
                Vector3 from = network.Waypoints[network.uiStart].position;
                Vector3 to = network.Waypoints[network.uiEnd].position;

                // Request a path search on the nav mesh. This will return the path between
                // from and to vectors
                NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
                
                // Set Handles color to Yellow
                Handles.color = Color.yellow;
                
                // Draw a polyline passing int he path's corner points
                Handles.DrawPolyLine(path.corners);
            }
        }
    }

    
}
