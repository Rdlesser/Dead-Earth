using System.Collections.Generic;
using Dead_Earth.Scripts.AI;
using UnityEngine;

namespace Dead_Earth.Scripts
{
     /// <summary>
     /// Singleton class that acts as the scene database
     /// </summary>
     public class GameSceneManager : MonoBehaviour
     {
          // Inspector Assigned Variables
          [SerializeField] private ParticleSystem _bloodParticles;
          
          // Statics
          private static GameSceneManager _instance;

          public static GameSceneManager Instance
          {
               get
               {
                    if (_instance == null)
                    {
                         _instance = (GameSceneManager) FindObjectOfType(typeof(GameSceneManager));
                    }

                    return _instance;
               }
          }

          // Private
          private Dictionary<int, AIStateMachine> _stateMachines = new Dictionary<int, AIStateMachine>();
          
          // Properties
          public ParticleSystem BloodParticles => _bloodParticles;

          // Public Methods
          /// <summary>
          /// Stores the passed state machine in the dictionary with
          /// the supplied key
          /// </summary>
          /// <param name="key"> The machine key to register </param>
          /// <param name="stateMachine"> The state machine to register </param>
          public void RegisterStateMachine(int key, AIStateMachine stateMachine)
          {
               if (!_stateMachines.ContainsKey(key))
               {
                    _stateMachines[key] = stateMachine;
               }
          }

          /// <summary>
          /// Returns an AI State Machine reference searched on by the
          /// instance ID of an object
          /// </summary>
          /// <param name="key"> The key of the machine </param>
          /// <returns></returns>
          public AIStateMachine GetAIStateMachine(int key)
          {
               AIStateMachine stateMachine;
               if (_stateMachines.TryGetValue(key, out stateMachine))
               {
                    return stateMachine;
               }

               return null;
          }
     }
}
