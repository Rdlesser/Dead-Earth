using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    /// <summary>
    /// Player identifier enum
    /// </summary>
    public enum PlayerType
    {
        Left,
        Right
    }

    #region Editor exposed members
    [SerializeField] private PlayerType _playerType;
    [SerializeField] private float _movementSpeed = 5;
    #endregion

    #region Private members
    private Transform _transform;
    private float _halfHeight;

#endregion

    private void Start()
    {
        // Store highly used variables in advance for performance
        _transform = transform;
        _halfHeight = GetComponent<Collider>().bounds.extents.y;
    }

    private void FixedUpdate()
    {
        // TODO: Get movement input (Make sure left/right player)
        // TODO: Move player
        // TODO: Make sure player doesn't leave screen bounds (ScreenUtil.ScreenPhysicalBounds will help you out)
        float verticalInput = _playerType == PlayerType.Left? 
                                  Input.GetAxisRaw("Vertical") : Input.GetAxisRaw("Vertical2");
        if (verticalInput > 0 && Math.Abs(_transform.position.y - ScreenUtil.ScreenPhysicalBounds.yMax) < _halfHeight ||
            verticalInput < 0 && Math.Abs(_transform.position.y - ScreenUtil.ScreenPhysicalBounds.yMin) < _halfHeight)
        {
            return;
        }

        var position = _transform.position;
        position = new Vector2(position.x,position.y + verticalInput * _movementSpeed * Time.deltaTime);
        _transform.position = position;
    }
}