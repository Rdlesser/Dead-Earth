using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// ----------------------------------------------------------
// CLASS	:	NavAgentRootMotion
// DESC		:	Behaviour to test Unity's NavMeshAgent with
//				Animator component using root motion
// ----------------------------------------------------------
namespace Navigation_Example
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavAgentRootMotion : MonoBehaviour
    {
        // Inspector Assigned Variable
        public AIWaypointNetwork waypointNetwork;
        public int currentIndex;
        public bool hasPath;
        public bool pathPending;
        public bool pathStale;
        public NavMeshPathStatus pathStatus = NavMeshPathStatus.PathInvalid;
        public bool mixedMode = true;

        // Private Members
        private NavMeshAgent _navAgent;
        private Animator _animator;
        private float _smoothAngle;
        private static readonly int Angle = Animator.StringToHash("Angle");
        private static readonly int Speed = Animator.StringToHash("Speed");


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

            // Turn off auto-update
            /*_navAgent.updatePosition = false; */
            _navAgent.updateRotation = false;

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
            int nextWaypoint = currentIndex + incStep >= waypointNetwork.waypoints.Count ? 0 : currentIndex + incStep;
            Transform nextWaypointTransform =  waypointNetwork.waypoints[nextWaypoint];

            if (nextWaypointTransform != null)
            {
                // Update the current waypoint index, assign its position as the NavMeshAgents
                // Destination and then return
                currentIndex = nextWaypoint;
                _navAgent.destination = nextWaypointTransform.position;
                return;
            }

            // We did not find a valid waypoint in the list for this iteration
            currentIndex = nextWaypoint;
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
        
            // Transform agents desired velocity into local space
            Vector3 localDesiredVelocity = transform.InverseTransformVector(_navAgent.desiredVelocity);
        
            // Get angle in degrees we need to turn to reach the desired velocity direction
            float angle = Mathf.Atan2(localDesiredVelocity.x, localDesiredVelocity.z) * Mathf.Rad2Deg;
        
            // Smoothly interpolate towards the new angle
            _smoothAngle = Mathf.MoveTowardsAngle(_smoothAngle, angle, 80.0f * Time.deltaTime);

            // Speed is simply the amount of desired velocity projected onto our own forward vector
            float speed = localDesiredVelocity.z;

            // Set animator parameters
            _animator.SetFloat(Angle, _smoothAngle);
            _animator.SetFloat(Speed, speed, 0.1f, Time.deltaTime);
        

            if (_navAgent.desiredVelocity.sqrMagnitude > Mathf.Epsilon)
            {
                if (!mixedMode ||
                    (mixedMode && 
                     Mathf.Abs(angle) < 80.0f && 
                     _animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion")))
                {
                    Quaternion lookRotation = Quaternion.LookRotation(_navAgent.desiredVelocity, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5.0f * Time.deltaTime);
                }
            }

            // If we don't have a path and one isn't pending then set the next
            // waypoint as the target, otherwise if path is stale regenerate path
            if (_navAgent.remainingDistance <= _navAgent.stoppingDistance && !pathPending ||
                pathStatus == NavMeshPathStatus.PathInvalid)
            {
                SetnextDestination(true);
            }
            else if (_navAgent.isPathStale)
            {
                SetnextDestination(false);
            }
        
        }

        private void OnAnimatorMove()
        {
            // If we are in mixed mode and we are not in the Locomotion state then apply root rotation
            if (mixedMode &&
                !_animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion"))
            {
                transform.rotation = _animator.rootRotation;
            }
        
            // Override Agent's velocity with the velocity of the root motion
            _navAgent.velocity = _animator.deltaPosition / Time.deltaTime;
        
        }

        // ---------------------------------------------------------
        // Name	:	Jump
        // Desc	:	Manual OffMeshLInk traversal using an Animation
        //			Curve to control agent height.
        // ---------------------------------------------------------
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
}
