using UnityEngine;
using TMPro;

public class GameProfile : PlayerProfileBase
{
    [Header("Game UI")]
    [SerializeField] private TMP_Text scoreText;

    public void SetupProfile(Player player, int score)
    {
        SetupBase(player);
        SetScore(score);
    }

    public void SetScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score.ToString();
        }
    }
}