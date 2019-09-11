using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ColliderSnap : MonoBehaviour
{
    #region Editor exposed members
    [SerializeField] private ScreenUtil.SnapPosition _snapPosition;
    [SerializeField] private Vector2 _relativeOffset = Vector2.zero;
    #endregion

    private void Start()
    {
        // Snap and destroy
        ScreenUtil.SnapCollider(GetComponent<Collider>(), _snapPosition, _relativeOffset);
        Destroy(this);
    }
}