using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class GameProfile : PlayerProfileBase
{
    [Header("Game UI")]
    [SerializeField] private TMP_Text scoreText;

    [SerializeField] private Button profileButton;
    private int currentScore = 0;

    public event Action<string> OnProfileClicked;

    void Awake()
    {
        profileButton.onClick.AddListener(OnClicked);
        profileButton.interactable = false;
    }
    public void SetupProfile(Player player, int score)
    {
        SetupBase(player);
        SetScore(score);
    }

    public void SetScore(int score)
    {
        currentScore = score;

        if (scoreText != null)
        {
            scoreText.text = "Score: " + score.ToString();
        }
    }

    public int GetScore()
    {
        return currentScore;
    }

    public void AddScore(int points)
    {
        SetScore(currentScore + points);
    }

    public void SetClickable(bool clickable)
    {
        if (profileButton != null)
        {
            profileButton.interactable = clickable;
        }
    }

    void OnClicked()
    {
        Debug.Log($"🥔 Profile clicked for player {playerId}");
        OnProfileClicked?.Invoke(playerId);
    }


}