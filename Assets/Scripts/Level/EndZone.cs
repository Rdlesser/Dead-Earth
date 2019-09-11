using UnityEngine;

public class EndZone : MonoBehaviour
{
    public enum EndZoneType
    {
        Left,
        Right
    }

    #region Editor exposed members
    [SerializeField] private EndZoneType _endZoneType;
    #endregion

    #region Read only properties
    public EndZoneType EndZoneSide
    {
        get { return _endZoneType; }
    }
    #endregion
}