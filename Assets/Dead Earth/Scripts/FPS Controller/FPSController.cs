using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Enumerations
public enum PlayerMoveStatus
{
    NotMoving,
    Crouching,
    Walking,
    Running,
    NotGrounded,
    Landing
}

public enum CurveControlledBobCallbackType
{
    Horizontal,
    Vertical
}

// Delegates
public delegate void CurveControlledBobCallback();

[Serializable]
public class CurveControlledBobEvent
{
    public float Time = 0.0f;
    public CurveControlledBobCallback Function = null;
    public CurveControlledBobCallbackType Type = CurveControlledBobCallbackType.Vertical;
}

[Serializable]
public class CurveControlledBob
{
    [SerializeField] private AnimationCurve _bobCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f),
                           new Keyframe(1f, 0f), new Keyframe(1.5f, -1f),
                           new Keyframe(2f, 0f));
    
    // Inspector Assigned Bob Control Variables 
    [SerializeField] private float _horizontalMultiplier = 0.01f;
    [SerializeField] private float _verticalMultiplier = 0.02f;
    [SerializeField] private float _verticalToHorizontalSpeedRatio = 2.0f;
    [SerializeField] private float _baseInterval = 1.0f;
    
    // Internals
    private float _prevXPlayHead;
    private float _prevYPlayHead;
    private float _xPlayHead;
    private float _yPlayHead;
    private float _curveEndTime;
    private List<CurveControlledBobEvent> _events = new List<CurveControlledBobEvent>();

    public void Initialize()
    {
        // Record time length of bob curve
        _curveEndTime = _bobCurve[_bobCurve.length - 1].time;
        _xPlayHead = 0f;
        _yPlayHead = 0f;
        _prevXPlayHead = 0f;
        _prevYPlayHead = 0f;
    }

    public void RegisterEventCallback(float time,
                                      CurveControlledBobCallback function,
                                      CurveControlledBobCallbackType type)
    {
        CurveControlledBobEvent ccbeEvent = new CurveControlledBobEvent();
        ccbeEvent.Time = time;
        ccbeEvent.Function = function;
        ccbeEvent.Type = type;
        _events.Add(ccbeEvent);
        _events.Sort((t1, t2) => t1.Time.CompareTo(t2.Time));
    }

    public Vector3 GetVectorOffset(float speed)
    {
        _xPlayHead += speed * Time.deltaTime / _baseInterval;
        _yPlayHead += speed * Time.deltaTime / _baseInterval * _verticalToHorizontalSpeedRatio;

        if (_xPlayHead > _curveEndTime)
        {
            _xPlayHead -= _curveEndTime;
        }

        if (_yPlayHead > _curveEndTime)
        {
            _yPlayHead -= _curveEndTime;
        }
        
        // Process Events
        for (int i = 0; i < _events.Count; i++)
        {
            CurveControlledBobEvent curveEvent = _events[i];
            if (curveEvent != null)
            {
                if (curveEvent.Type == CurveControlledBobCallbackType.Vertical)
                {
                    if (_prevYPlayHead < curveEvent.Time && _yPlayHead >= curveEvent.Time ||
                        _prevYPlayHead > _yPlayHead && (curveEvent.Time > _prevYPlayHead || curveEvent.Time <= _yPlayHead))
                    {
                        curveEvent.Function();
                    }
                }
                else
                {
                    if (_prevXPlayHead < curveEvent.Time && _xPlayHead >= curveEvent.Time ||
                        _prevXPlayHead > _xPlayHead && (curveEvent.Time > _prevXPlayHead || curveEvent.Time <= _xPlayHead))
                    {
                        curveEvent.Function();
                    }
                }
            }
        }

        float xPos = _bobCurve.Evaluate(_xPlayHead) * _horizontalMultiplier;
        float yPos = _bobCurve.Evaluate(_yPlayHead) * _verticalMultiplier;

        _prevXPlayHead = _xPlayHead;
        _prevYPlayHead = _yPlayHead;

        return new Vector3(xPos, yPos, 0f);
    }
}

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    public List<AudioSource> AudioSources = new List<AudioSource>();
    private int _audioToUse = 0;

    // Inspector Assigned Locomotion Settings
    [SerializeField] private float _walkSpeed = 2.0f;
    [SerializeField] private float _runSpeed = 4.5f;
    [SerializeField] private float _jumpSpeed = 7.5f;
    [SerializeField] private float _crouchSpeed = 1.0f;
    [SerializeField] private float _stickToGroundForce = 5.0f;
    [SerializeField] private float _gravityMultiplier = 2.5f;
    [SerializeField] private float _runStepLengthen = 0.75f;
    [SerializeField] private CurveControlledBob _headBob = new CurveControlledBob();
    [SerializeField] private GameObject _flashLight = null;

	// User Standard Assets Mouse Look class for mouse input -> Camera Look Control
    [SerializeField] private UnityStandardAssets.Characters.FirstPerson.MouseLook _mouseLook = 
        new UnityStandardAssets.Characters.FirstPerson.MouseLook();

    // Private internals
    private Camera _camera = null;
    private bool _jumpButtonPressed = false;
    private Vector2 _inputVector = Vector2.zero;
    private Vector3 _moveDirection = Vector3.zero;
    private bool _previouslyGrounded = false;
    private bool _isWalking = true;
    private bool _isJumping = false;
    private bool _isCrouching = false;
    private Vector3 _localSpaceCameraPos = Vector3.zero;
    private float _controllerHeight = 0f;
	
    // Timers
    private float _fallingTimer = 0.0f;

    private CharacterController _characterController = null;
    private PlayerMoveStatus _movementStatus = PlayerMoveStatus.NotMoving;
    
    // Public Properties 
    public PlayerMoveStatus MovementStatus => _movementStatus;
    public float WalkSpeed => _walkSpeed;
    public float RunSpeed => _runSpeed;

	
		
	private void Start()
    {
		// Cache component references
        _characterController = GetComponent<CharacterController>();
        _controllerHeight = _characterController.height;
        
        // Get the main camera and cache local position within the FPS rig
        _camera = Camera.main;
        _localSpaceCameraPos = _camera.transform.localPosition;

        // Set initial to not jumping and not moving
        _movementStatus = PlayerMoveStatus.NotMoving;
        
        // Reset timers
        _fallingTimer = 0.0f;
        
        // Setup Mouse Look Script
        _mouseLook.Init(transform , _camera.transform);
        
        // Initialized Head Bob Object
        _headBob.Initialize();
        _headBob.RegisterEventCallback(1.5f, PlayFootstepSound, CurveControlledBobCallbackType.Vertical);

        if (_flashLight)
        {
            _flashLight.SetActive(false);
        }
    }

    protected void Update()
    {
		// If we are falling - increment timer
        if (_characterController.isGrounded)
        {
            _fallingTimer = 0.0f;
        }
        else
        {
            _fallingTimer += Time.deltaTime;
        }

		// Allow Mouse Look a chance to process mouse and rotate camera
        if (Time.timeScale > Mathf.Epsilon)
        {
            _mouseLook.LookRotation(transform, _camera.transform);
        }

        if (Input.GetButtonDown("Flashlight"))
        {
            if (_flashLight)
            {
                _flashLight.SetActive(!_flashLight.activeSelf);
            }
        }
		
        // Process the Jump Button
        // the jump state needs to read here to make sure it is not missed
        if (!_jumpButtonPressed && !_isCrouching) 
        {
            _jumpButtonPressed = Input.GetButtonDown("Jump");
        }

        if (Input.GetButtonDown("Crouch"))
        {
            _isCrouching = !_isCrouching;
            _characterController.height = _isCrouching ? _controllerHeight / 2f : _controllerHeight;
        }

		// Calculate Character Status
        if (!_previouslyGrounded && _characterController.isGrounded)
        {
            if (_fallingTimer > 0.5f)
            {
                // TODO: Play landing sound
            }

            _moveDirection.y = 0f;
            _isJumping = false;
            _movementStatus = PlayerMoveStatus.Landing;
        }

		else if (!_characterController.isGrounded)
        {
                _movementStatus = PlayerMoveStatus.NotGrounded;
        }

        else if (_characterController.velocity.sqrMagnitude < 0.01f)
        {
            _movementStatus = PlayerMoveStatus.NotMoving;
        }
        else if (_isCrouching)
        {
            _movementStatus = PlayerMoveStatus.Crouching;
        }
        else if (_isWalking)
        {
            _movementStatus = PlayerMoveStatus.Walking;
		}
        else
        {
            _movementStatus = PlayerMoveStatus.Running;
        }

		_previouslyGrounded = _characterController.isGrounded;

    }

    protected void FixedUpdate()
    {
        // Read input from axis
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool wasWalking = _isWalking;
        _isWalking = !Input.GetKey(KeyCode.LeftShift);
        
        // Set the desired speed to be either our walking speed or our running speed
        float speed = _isCrouching? _crouchSpeed : _isWalking ? _walkSpeed : _runSpeed;
        _inputVector = new Vector2(horizontal, vertical);

		// Normalize input if it exceeds 1 in combined length:
        if (_inputVector.sqrMagnitude > 1)
        {
            _inputVector.Normalize();
        }
		
        // Always move along the camera forward as it is the direction the it being aimed at
        Vector3 desiredMove = transform.forward * _inputVector.y + transform.right * _inputVector.x;
        
		// Get a normal for the surface that is being touched to move along it
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position,
                               _characterController.radius, 
                               Vector3.down,
                               out hitInfo, 
                               _characterController.height / 2f,
                               1))
        {
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
        }

		// Scale movement by our current speed (walking value or running value)s
        _moveDirection.x = desiredMove.x * speed;
        _moveDirection.z = desiredMove.z * speed;
		
        // If grounded
        if (_characterController.isGrounded)
        {
			// Apply severe down force to keep control sticking to floor
            _moveDirection.y = -_stickToGroundForce;
			
            // If the jump button was pressed then apply speed in up direction 
            // and set isJumping to true. Also, reset the jump button status
            if (_jumpButtonPressed)
            {
                _moveDirection.y = _jumpSpeed;
                _jumpButtonPressed = false;
                _isJumping = true;
                
                // TODO: Play jumping sound
            }
        }
        else
        {
            // Otherwise we are not on the ground so apply standard system gravity multiplier 
            // by our gravity modifier
            _moveDirection += Physics.gravity * (_gravityMultiplier * Time.fixedDeltaTime);
        }

		// Move the Character Controller
        _characterController.Move(_moveDirection * Time.fixedDeltaTime);
        
        // Are we moving
        var characterControllerVelocity = _characterController.velocity;
        Vector3 speedXZ = new Vector3(characterControllerVelocity.x, 0, characterControllerVelocity.z);
        if (speedXZ.magnitude > 0.01f)
        {
            _camera.transform.localPosition = _localSpaceCameraPos +
                                              _headBob.GetVectorOffset(speedXZ.magnitude * 
                                                                       (_isCrouching || _isWalking? 1.0f : _runStepLengthen));
        }
        else
        {
            _camera.transform.localPosition = _localSpaceCameraPos;
        }
		
    }

    public void PlayFootstepSound()
    {
        if (_isCrouching)
        {
            return;
        }
        AudioSources[_audioToUse].Play();
        _audioToUse = (_audioToUse + 1) % 2;
    }

}
