using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
    /// <summary>
    /// The base class of all AI States used by our AI System.
    /// </summary>
    public abstract class AIState : MonoBehaviour
    {
    
        // Public Method
        
        /// <summary>
        /// Called by the parent state machine to assign its reference
        /// </summary>
        /// <param name="stateMachine"> The state machine to assign </param>
        public virtual void SetStateMachine(AIStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }
    
        // Default Handlers
        public virtual void OnEnterState() {} 
        public virtual void OnExitState() {}
        public virtual void OnAnimatorIKUpdated() {}
        public virtual void OnTriggerEvent(AITriggerEventType eventType, Collider other) {}
        public virtual void OnDestinationReached(bool isReached) {}

        // Abstract Methods
        public abstract AIStateType GetStateType();
        public abstract AIStateType OnUpdate();
        
        // Protected Fields
        protected AIStateMachine _stateMachine;
        
        /// <summary>
        /// Called by the parent state machine to allow root motion processing
        /// </summary>
        public virtual void OnAnimatorUpdated()
        {
            // Get the number of meters the root motion has updated for this update and
            // divide by deltaTime to get meters per second. We then assign this to
            // the nav agent's velocity.
            if (_stateMachine.UseRootPosition)
            {
                _stateMachine.NavAgent.velocity = _stateMachine.Animator.deltaPosition / Time.deltaTime;
            }

            // Grab the root rotation from the animator and assign as our transform's rotation.
            if (_stateMachine.UseRootRotation)
            {
                _stateMachine.transform.rotation = _stateMachine.Animator.rootRotation;
            }
        }

        /// <summary>
        /// Converts the passed sphere collider's position and radius into world space <br/>
        /// taking into account hierarchical scaling 
        /// </summary>
        /// <param name="collider"> The collider that is to be converted </param>
        /// <param name="worldPosition"> The position in the world space </param>
        /// <param name="radius"> The radius </param>
        public static void ConvertSphereColliderToWorldSpace(SphereCollider collider, 
                                                             out Vector3 worldPosition,
                                                             out float radius)
        {
            // Default Values
            worldPosition = Vector3.zero;
            radius = 0.0f;

            // If no valid sphere collider return
            if (collider == null)
            {
                return;
            }

            var colliderTransform = collider.transform;
            var colliderCenter = collider.center;
            var lossyScale = colliderTransform.lossyScale;
            
            // Calculate world space position of sphere center
            worldPosition = colliderTransform.position;
            worldPosition.x += colliderCenter.x * lossyScale.x;
            worldPosition.y += colliderCenter.y * lossyScale.y;
            worldPosition.z += colliderCenter.z * lossyScale.z;

            // Calculate world space radius of sphere
            var colliderRadius = collider.radius;
            radius = Mathf.Max(colliderRadius * lossyScale.x,
                               colliderRadius * lossyScale.y);
            radius = Mathf.Max(radius, collider.radius * lossyScale.z);
        }

        /// <summary>
        /// Returns the signed angle between two vectors (in degrees)
        /// </summary>
        /// <param name="fromVector"> The vector From which we would like to calculate </param>
        /// <param name="toVector"> The vector TO which we would like to calculate </param>
        /// <returns> (float) the signed angle between the from and two vector </returns>
        public static float FindSignedAngle(Vector3 fromVector, Vector3 toVector)
        {
            if (fromVector == toVector)
            {
                return 0f;
            }

            float angle = Vector3.Angle(fromVector, toVector);
            Vector3 cross = Vector3.Cross(fromVector, toVector);

            angle *= Mathf.Sign(cross.y);

            return angle;
        }
    }
}
