using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DoorState
{
    Open,
    Animating,
    Closed
};

public class SlidingDoorDemo : MonoBehaviour
{
    // Public members
    public float slidingDistance = 4f;
    public float duration = 1.5f;
    public AnimationCurve jumpCurve = new AnimationCurve();
    
    // Private members
    private Transform _transform;
    private Vector3 _openPos = Vector3.zero;
    private Vector3 _closedPos = Vector3.zero;
    private DoorState _doorState = DoorState.Closed;
    
    // Start is called before the first frame update
    void Start()
    {
        _transform = transform;
        _closedPos = _transform.position;
        _openPos = _closedPos + _transform.right * slidingDistance;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && _doorState != DoorState.Animating)
        {
            StartCoroutine(AnimateDoor(_doorState == DoorState.Open ? DoorState.Closed : DoorState.Open));
        }
    }

    IEnumerator AnimateDoor(DoorState newState)
    {
        _doorState = DoorState.Animating;
        float time = 0.0f;
        Vector3 startPos = newState == DoorState.Open ? _closedPos : _openPos;
        Vector3 endPos = newState == DoorState.Open ? _openPos : _closedPos;

        while (time <= duration)
        {
            float t = time / duration;
            _transform.position = Vector3.Lerp(startPos, endPos, jumpCurve.Evaluate(t));
            time += Time.deltaTime;
            yield return null;
        }

        _transform.position = endPos;
        _doorState = newState;
    }
}
