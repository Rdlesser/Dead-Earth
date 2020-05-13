
using UnityEngine;
using UnityEngine.Animations;

namespace Dead_Earth.Scripts.AI.State_Machine_Behaviours
{
    /// <summary>
    /// A State Machine Behaviour that communicates <br/>
    /// with an AIStateMachine derived class to <br/>
    /// allow for enabling/disabling root motion on <br/>
    /// a per animation state basis. <br/>
    /// </summary>
    public class RootMotionConfigurator : AIStateMachineLink
    {
        // Inspector Assigned Reference Incrementing Variables
        [SerializeField] private int _rootPosition;
        [SerializeField] private int _rootRotation;

        /// <summary>
        /// Called prior to the first frame the <br/>
        /// animation assigned to this state.
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="stateInfo"></param>
        /// <param name="layerIndex"></param>
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Request the enabling/disabling of root motion for this animation state 
            if (_stateMachine)
            {
                _stateMachine.AddRootMotionRequest(_rootPosition, _rootRotation);
            }
        }

        /// <summary>
        /// Called on the last frame of the animation prior <br/>
        /// to leaving the state.
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="stateInfo"></param>
        /// <param name="layerIndex"></param>
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Inform the AI State Machine that we wish to relinquish our root motion request.
            if (_stateMachine)
            {
                _stateMachine.AddRootMotionRequest(-_rootPosition, -_rootRotation);
            }
        }
    }
}
