using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Private members
    private Ball _ball;
    private int _leftPlayerScore;
    private int _rightPlayerScore;
    #endregion

    #region Editor exposed properties
    [SerializeField] private int _scoreToWin = 3;
    [SerializeField] private int _matchWaitSeconds = 3;
    #endregion

    // Singleton
    public static GameManager Instance { get; private set; }

    private void Start()
    {
        // Singleton init
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // Find the ball
        _ball = FindObjectOfType<Ball>();
        if (_ball == null)
        {
            Debug.LogError("Ball not found!");
            Application.Quit();
            return;
        }

        // Basic init
        UI.Instance.UpdatePlayersScores(_leftPlayerScore, _rightPlayerScore);
        _ball.EnteredEndZone += BallOnEnteredEndZone;

        StartCoroutine(StartNewMatch());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    /// <summary>
    /// Starts a new match
    /// </summary>
    private IEnumerator StartNewMatch()
    {
        _ball.Reset();

        // Sanity
        if (_matchWaitSeconds <= 0)
        {
            _matchWaitSeconds = 3;
        }

        float totalTime = _matchWaitSeconds;

        // Wait before starting new match (Countdown)
        // TODO: Using this coroutine, show a countdown message for _matchWaitSeconds seconds (Use UI.Instance.ChangeMainMessage)
        while (totalTime >= 0f)
        {
            UI.Instance.ChangeMainMessage(totalTime.ToString("0"));
            totalTime--;
            yield return new WaitForSeconds(1f);
            
        }
        UI.Instance.ChangeMainMessage("");

        // Start
        _ball.GiveRandomVelocity();

        // TODO: Remove this yield break
    }

    /// <summary>
    /// Restarts the entire game
    /// </summary>
    private void StartNewGame()
    {
        Application.LoadLevel(0);
    }

    /// <summary>
    /// Event listener to handle goal
    /// </summary>
    /// <param name="endZoneType">The goal side that the ball entered</param>
    private void BallOnEnteredEndZone(EndZone.EndZoneType endZoneType)
    {
        StartCoroutine(ShowGoalMessageAndHandleGoal(endZoneType == EndZone.EndZoneType.Left ? Player.PlayerType.Right : Player.PlayerType.Left));
    }

    /// <summary>
    /// Shows a message for the goal
    /// </summary>
    /// <param name="endZoneType">The end zone that was</param>
    /// <returns></returns>
    private IEnumerator ShowGoalMessageAndHandleGoal(Player.PlayerType scoringPlayer)
    {
        // TODO: Increase the correct player's score
        if (scoringPlayer == Player.PlayerType.Left)
        {
            _leftPlayerScore++; 
        }
        else
        {
            _rightPlayerScore++;
        }
        // Update score
        UI.Instance.UpdatePlayersScores(_leftPlayerScore, _rightPlayerScore);


        // Show message / Handle game victory
        // TODO: Handle victory condition (_scoreToWin)
        bool isGameOver = _leftPlayerScore == _scoreToWin || _rightPlayerScore == _scoreToWin;
        String message = scoringPlayer + " Player";
        if (isGameOver)
        {
            // TODO: Show message which player has won
            UI.Instance.ChangeMainMessage(message + " Wins!");
            // TODO: Wait 3 seconds before starting a new game
            yield return new WaitForSeconds(_matchWaitSeconds);
            StartNewGame();
        }
        else
        {
            // TODO: Show message which player has scored
            UI.Instance.ChangeMainMessage(message + " Scores!");
            // TODO: Wait 3 seconds before starting a new match
            yield return new WaitForSeconds(_matchWaitSeconds);
            StartCoroutine(StartNewMatch());
        }

        // TODO: Remove this "Yield break" after you've implemented the above
    }
    
}