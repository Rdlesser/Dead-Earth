using System;
using System.Collections;
using System.Collections.Generic;
using Dead_Earth.Scripts;
using Dead_Earth.Scripts.AI;
using UnityEngine;

public class MeleeZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        AIStateMachine machine = GameSceneManager.Instance.GetAIStateMachine(other.GetInstanceID());
        if (machine)
        {
            machine.InMeleeRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        AIStateMachine machine = GameSceneManager.Instance.GetAIStateMachine(other.GetInstanceID());
        if (machine)
        {
            machine.InMeleeRange = false;
        }
    }
}
