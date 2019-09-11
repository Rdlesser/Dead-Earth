using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Ball : MonoBehaviour
{
    #region Editor exposed members
    [SerializeField] private float _minVelocity = 5f;
    [SerializeField] private float _maxVelocity = 12f;
    #endregion

    #region Events
    public event Action<EndZone.EndZoneType> EnteredEndZone;
    #endregion

    private Rigidbody _rigidbody;

    private bool _canBeHit = true;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Gives the ball a completely random velocity (50%/50% left/right + 50%/50% up/down) with the minimum velocity
    /// </summary>
    public void GiveRandomVelocity()
    {
        // TODO: Give our rigidbody a random velocity, 50%/50% left/right + 50%/50% up/down - must be at least at _minVelocity
        int[] choices = { -1, 1};
        int randomIndex = Random.Range( 0, choices.Length );
        int verticalDirection = choices[randomIndex];
        randomIndex = Random.Range(0, choices.Length);
        int horizontalDirection = choices[randomIndex];
        float verticalVelocity = Random.Range(_minVelocity, _maxVelocity);
        float horizontalVelocity = Random.Range(_minVelocity, _maxVelocity);
        _rigidbody.velocity =  new Vector3(horizontalVelocity * horizontalDirection, 
                                           verticalVelocity * verticalDirection);
    }

    /// <summary>
    /// Resets the ball (position and velocity)
    /// </summary>
    public void Reset()
    {
        // TODO: Reset our ball's position and velocity
        _rigidbody.position = Vector3.zero;
        _rigidbody.velocity = Vector3.zero;
        _canBeHit = true;
    }

    private void OnCollisionEnter(Collision other)
    {
        
        // Make sure if the ball lost velocity that we're never below the minimum
        if (_canBeHit && _rigidbody.velocity.magnitude < _minVelocity)
        {
            float ratio = _minVelocity / _rigidbody.velocity.magnitude;
            _rigidbody.velocity = _rigidbody.velocity * ratio;
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        // TODO: Handle trigger collisions for endzones
        // TODO: Make sure we collided with an endzone
        // TODO: Raise the EnteredEndZone event if we did
        _rigidbody.velocity = Vector3.zero;
        _canBeHit = false;
        EndZone endZone = other.GetComponent<EndZone>();
        // if we've hit an EndZone
        if (endZone != null && EnteredEndZone != null)
        {
            EnteredEndZone.Invoke(endZone.EndZoneSide);
        }
    }
    
}