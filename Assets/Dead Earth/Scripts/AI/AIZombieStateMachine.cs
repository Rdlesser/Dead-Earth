using UnityEngine;
using UnityEngine.AI;

namespace Dead_Earth.Scripts.AI
{
    /// <summary>
    /// State Machine used by zombie characters
    /// </summary>
    public class AIZombieStateMachine : AIStateMachine
    {
        // Inspector Assigned
        [SerializeField] [Range(10f, 360f)] private float _fov = 50f;
        [SerializeField] [Range(0f, 1f)] private float _sight = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float _hearing = 1f;
        [SerializeField] [Range(0f, 1f)] private float _aggression = 0.5f;
        [SerializeField] [Range(0, 100)] private int _health = 100;
        [SerializeField] [Range(0f, 1f)] private float _intelligence = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float _satisfaction = 1f;
        [SerializeField] private float _replenishRate = 0.5f;
        [SerializeField] private float _depletionRate = 0.1f;
        
        // Private
        private int _seeking;
        private bool _feeding;
        private bool _crawling = false;
        private int _attackType;
        private float _speed;
        
        // Hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int FeedingHash = Animator.StringToHash("Feeding");
        private static readonly int SeekingHash = Animator.StringToHash("Seeking");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        // Public Properties
        public float ReplenishRate => _replenishRate;
        public float Fov => _fov;
        public float Hearing => _hearing;
        public float Sight => _sight;
        public bool Crawling => _crawling;
        public float Intelligence => _intelligence;
        public float Satisfaction
        {
            get => _satisfaction;
            set => _satisfaction = value;
        }
        public float Aggression
        {
            get => _aggression;
            set => _aggression = value;
        }

        public int Health
        {
            get => _health;
            set => _health = value;
        }

        public int AttackType
        {
            get => _attackType;
            set => _attackType = value;
        }

        public bool Feeding
        {
            get => _feeding;
            set => _feeding = value;
        }

        public int Seeking
        {
            get => _seeking;
            set => _seeking = value;
        }

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        /// <summary>
        /// Refresh the animator with up-to-date values for <br/>
        /// its parameters
        /// </summary>
        protected override void Update()
        {
            
            base.Update();

            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash , _speed);
                _animator.SetBool(FeedingHash, _feeding);
                _animator.SetInteger(SeekingHash, _seeking);
                _animator.SetInteger(AttackHash, _attackType);
            }

            _satisfaction = Mathf.Max(0, _satisfaction - (_depletionRate * Time.deltaTime / 100f) * Mathf.Pow(_speed, 3f));
        }
        
    }
}
