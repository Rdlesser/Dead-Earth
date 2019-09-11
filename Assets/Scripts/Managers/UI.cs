using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton UI class to handle all of Pong's UI
/// </summary>
public class UI : MonoBehaviour
{
    // Singleton
    public static UI Instance { get; private set; }

    #region Editor exposed members
    [SerializeField] private Text _mainText;
    [SerializeField] private Text _leftPlayerScoreText;
    [SerializeField] private Text _rightPlayerScoreText;
    #endregion

    void Awake()
    {
        // Singleton
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Changes the main message that is currently appearing on screen
    /// </summary>
    /// <param name="newMessage">Message text to show</param>
    public void ChangeMainMessage(string newMessage)
    {
        // TODO: Change _mainText
        _mainText.text = newMessage;
    }

    /// <summary>
    /// Updates the currently displayed player scores
    /// </summary>
    /// <param name="leftPlayerScore">Left player score</param>
    /// <param name="rightPlayerScore">Right player score</param>
    public void UpdatePlayersScores(int leftPlayerScore, int rightPlayerScore)
    {
        // TODO: Change _leftPlayerScoreText & _rightPlayerScoreText - Use N0 formatting
        _leftPlayerScoreText.text = leftPlayerScore.ToString("N0");
        _rightPlayerScoreText.text = rightPlayerScore.ToString("N0");
    }
}