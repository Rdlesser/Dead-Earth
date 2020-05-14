using System;
using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
    /// <summary>
    /// The immediate base class of all zombie states. It provides the event <br/>
    /// processing and storage of the current threats.
    /// </summary>
    public abstract class AIZombieState : AIState
    {

        // Private
        protected int _playerLayerMask = -1;
        protected int _bodyPartLayer = -1;
        protected int _visualLayerMask = -1;
        protected AIZombieStateMachine _zombieStateMachine;

        /// <summary>
        /// Calculate the masks and layers used for Raycasting and layer testing
        /// </summary>
        private void Awake()
        {
            
            // Get a mask for line of sight testing with the player. (+1) is a hack to include the
            // default layer in the current version of unity.
            _playerLayerMask = LayerMask.GetMask("Player", "AI Body Part") + 1; 
            _visualLayerMask = LayerMask.GetMask("Player", "AI Body Part", "Visual Aggravator") + 1; 
            
            // Get the layer index of the AI Body Part layer
            _bodyPartLayer = LayerMask.NameToLayer("AI Body Part");
            
        }
        
        /// <summary>
        /// Check for type compliance and store reference as derived type
        /// </summary>
        /// <param name="stateMachine"> The state machine </param>
        public override void SetStateMachine(AIStateMachine stateMachine)
        {
            if (stateMachine.GetType() == typeof(AIZombieStateMachine))
            {
                base.SetStateMachine(stateMachine);
                _zombieStateMachine = (AIZombieStateMachine) stateMachine;
            }
        }

        /// <summary>
        /// Called by the parent state machine when threats enter/stay/exit the zombie's <br/>
        /// sensor trigger, This will be any collider assigned to the Visual or Audio <br/>
        /// Aggravator layers or the player. <br/>
        /// It examines the threat and stored it in the parent machine Visual or Audio <br/>
        /// threat members if found to be a higher priority threat. <br/>
        /// </summary>
        /// <param name="eventType"> The event type to examine </param>
        /// <param name="other"> The collider of the other object to examine </param>
        public override void OnTriggerEvent(AITriggerEventType eventType, Collider other)
        {
            // If we don't have a parent state machine then bail
            if (_zombieStateMachine == null)
            {
                return;
            }

            // We are not interested in exit events so only step in and process if its an
            // enter or stay.
            if (eventType != AITriggerEventType.Exit)
            {
                // What is the type of the current visual threat we have stored
                AITargetType currentType = _zombieStateMachine.VisualThreat.Type;

                // Is the collider that has entered our sensor a player
                if (other.CompareTag("Player"))
                {
                    // Get distance from the sensor origin to the collider
                    float distance = Vector3.Distance(_zombieStateMachine.SensorPosition, other.transform.position);
                    
                    // If the currently stored threat is not a player or if this player is closer than a player
                    // previously stored as the visual threat...this could be more important
                    if (currentType != AITargetType.Visual_Player ||
                        currentType == AITargetType.Visual_Player && distance < _zombieStateMachine.VisualThreat.Distance)
                    {
                        // Is the collider within our view cone and do we have line of sight
                        if (ColliderIsVisible(other, out var hitInfo, _playerLayerMask))
                        {
                            // Yep...it's close and in our FOV and we have line of sight
                            // so store as the current most dangerous threat
                            _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Player,
                                                            other,
                                                            transform.position,
                                                            distance);
                        }
                    }
                }
                // TODO: Add comments
                else if (other.CompareTag("Flash Light") && currentType != AITargetType.Visual_Player)
                {
                    BoxCollider flashLightTrigger = (BoxCollider) other;
                    float distanceToThreat = Vector3.Distance(_zombieStateMachine.SensorPosition,
                                                              flashLightTrigger.transform.position);
                    float zSize = flashLightTrigger.size.z * flashLightTrigger.transform.lossyScale.z;
                    float aggravationFactor = distanceToThreat / zSize;

                    if (aggravationFactor <= _zombieStateMachine.Sight && aggravationFactor <= _zombieStateMachine.Intelligence)
                    {
                        _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Light, 
                                                             other, 
                                                             other.transform.position,
                                                             distanceToThreat);
                    }
                }
                else if (other.CompareTag("AI Sound Emitter"))
                {
                    SphereCollider soundTrigger = (SphereCollider) other;
                    if (soundTrigger == null)
                    {
                        return;
                    }
                    
                    // Get the position of the Agent Sensor
                    Vector3 agentSensorPosition = _zombieStateMachine.SensorPosition;

                    ConvertSphereColliderToWorldSpace(soundTrigger, out var soundPosition, out var soundRadius);
                    
                    // How far inside the sound's radius are we 
                    float distanceToThreat = (soundPosition - agentSensorPosition).magnitude;
                    
                    // Calculate a distance factor such that it is 1.0 when at sound radius, 0 when at center
                    float distanceFactor = distanceToThreat / soundRadius;
                    
                    // Bias the factor base on hearing ability of Agent
                    distanceFactor += distanceFactor * (1.0f - _zombieStateMachine.Hearing);

                    // Agent is too far away to be able to hear the threat
                    if (distanceFactor > 1.0f)
                    {
                        return;
                    }

                    // If we can hear it and it is closer than what we had previously stored 
                    if (distanceToThreat < _zombieStateMachine.AudioThreat.Distance)
                    {
                        // Most dangerous Audio Threat so far
                        _zombieStateMachine.AudioThreat.Set(AITargetType.Audio,
                                                            other, 
                                                            soundPosition,
                                                            distanceToThreat);
                    }
                    
                }
                // Register the closest visual threat
                else if (other.CompareTag("AI Food") &&
                         currentType != AITargetType.Visual_Player && 
                         currentType != AITargetType.Visual_Light &&
                         _zombieStateMachine.Satisfaction <= 0.9f &&
                         _zombieStateMachine.AudioThreat.Type == AITargetType.None)
                {
                    // How far is the threat from us
                    float distanceToThreat = Vector3.Distance(other.transform.position,
                                                              _zombieStateMachine.SensorPosition);
                    
                    // Is this smaller than anything we have previously stored
                    if (distanceToThreat < _zombieStateMachine.VisualThreat.Distance)
                    {
                        // If so then check that it is in our FOV and it is within the range of this
                        // AIs sight
                        RaycastHit hitInfo;
                        if (ColliderIsVisible(other, out hitInfo, _visualLayerMask))
                        {
                            // Yep this is our most appealing target so far
                            _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Food,
                                                                 other, 
                                                                 other.transform.position,
                                                                 distanceToThreat);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Test the passed collider against the zombie's FOV and using the passed
        /// layer mask for line of sight testing.
        /// </summary>
        /// <param name="other"> The passed collider to test </param>
        /// <param name="hitInfo"> The hit info provided by the RayCast </param>
        /// <param name="layerMask"> The layer mask used for the test </param>
        /// <returns> True if the provided collider is visible to the zombie <br/>
        /// False otherwise </returns>
        protected virtual bool ColliderIsVisible(Collider other, out RaycastHit hitInfo, int layerMask = -1)
        {
            // Let's make sure we have something to return
            hitInfo = new RaycastHit();
            
            // We need the state machine to be a AIZombieStateMachine
            if (_zombieStateMachine == null)
            {
                return false;
            }

            // Calculate the angle between the sensor origin and the direction of the collider
            Vector3 head = _zombieStateMachine.SensorPosition;
            Vector3 direction = other.transform.position - head;
            float angle = Vector3.Angle(direction, transform.forward);

            // If thr angle is greater than half our FOV then it is outside the view cone so
            // return false - no visibility
            if (angle > _zombieStateMachine.Fov * 0.5f)
            {
                return false;
            }

            // Now we need to test line of sight. Perform a ray cast from our sensor origin in the direction of the collider for distance
            // of our sensor radius scaled by the zombie's sight ability. This will return ALL hits.
            RaycastHit[] hits = Physics.RaycastAll(head,
                                                   direction.normalized,
                                                   _zombieStateMachine.SensorRadius * _zombieStateMachine.Sight,
                                                   layerMask);
            
            // Find the closest collider that is NOT the AIs own body part. 
            // If it's not the target then the target is obstructed
            float closestColliderDistance = float.MaxValue;
            Collider closestCollider = null;

            // Examine each hit
            foreach (var hit in hits)
            {
                // Is this hit closer than any we previously have found and stored
                if (hit.distance < closestColliderDistance)
                {
                    // If the hit is on the body part layer
                    if (hit.transform.gameObject.layer == _bodyPartLayer)
                    {
                        // And assuming it is not our own body part
                        if (_stateMachine != GameSceneManager.Instance.GetAIStateMachine(hit.rigidbody.GetInstanceID()))
                        {
                            // Store the collider, distance and hit info.
                            closestColliderDistance = hit.distance;
                            closestCollider = hit.collider;
                            hitInfo = hit;
                        }
                    }
                    else
                    {
                        // Its not a body part so simply store this as the new closest hit we have found
                        closestColliderDistance = hit.distance;
                        closestCollider = hit.collider;
                        hitInfo = hit;
                    }
                }
            }

            // If the closest hit is the collider we are testing against, it means we have line-of-sight
            // so return true.
            if (closestCollider && closestCollider.gameObject == other.gameObject)
            {
                return true;
            }
            
            // otherwise, something else is closer to us than the collider so line-of-sight is blocked
            return false;
        }
    }
}
