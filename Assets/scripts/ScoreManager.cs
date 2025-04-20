using UnityEngine;
using TMPro;

/// <summary>
/// Manages the player's score and updates the UI text.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [Tooltip("Reference to the UI TextMeshProUGUI component that displays the score.")]
    [SerializeField] private TextMeshProUGUI scoreText;

    // Internal score counter
    private int _score = 0;

    /// <summary>
    /// Adds the specified points to the score and updates the UI.
    /// </summary>
    /// <param name="points">Number of points to add.</param>
    public void AddScore(int points)
    {
        _score += points;
        UpdateScoreText();
    }

    /// <summary>
    /// Resets the score back to zero and updates the UI.
    /// </summary>
    public void ResetScore()
    {
        _score = 0;
        UpdateScoreText();
    }

    /// <summary>
    /// Gets the current score value.
    /// </summary>
    public int GetScore()
    {
        return _score;
    }

    // Updates the scoreText UI element.
    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{_score}";
        }
    }
}
