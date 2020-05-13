using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
    /// <summary>
    /// Notifies the parent AIStateMachine of any threats tha <br/>
    /// enter its trigger via the AIStateMachine's OnTriggerEvent <br/>
    /// method.
    /// </summary>
    public class AISensor : MonoBehaviour
    {
    
        // Private
        private AIStateMachine _parentStateMachine;
    
        // Public
        public AIStateMachine ParentStateMachine
        {
            set => _parentStateMachine = value;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_parentStateMachine != null)
            {
                _parentStateMachine.OnTriggerEvent(AITriggerEventType.Enter, other);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (_parentStateMachine != null)
            {
                _parentStateMachine.OnTriggerEvent(AITriggerEventType.Stay, other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_parentStateMachine != null)
            {
                _parentStateMachine.OnTriggerEvent(AITriggerEventType.Exit, other);
            }
        }
    }
}
