using UnityEngine;

namespace Dead_Earth.Scripts.AI.State_Machine_Behaviours
{
    /// <summary>
    /// Should be used as the base class for any <br/>
    /// StateMachineBehaviour that needs to communicate with <br/>
    /// its AI State Machine 
    /// </summary>
    public class AIStateMachineLink : StateMachineBehaviour
    {
        // The AI State Machine reference
        protected AIStateMachine _stateMachine;

        public AIStateMachine StateMachine
        {
            set => _stateMachine = value;
        }
    }
}
