using UnityEngine;
using System.Collections.Generic;
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance {get; private set;}
    
    //track scores by player ID
    private Dictionary<string, int> playerScores = new Dictionary<string, int>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject);
        }
    }

    //set all scores to 0
    public void InitializeScores(List<Player> players)
    {
        foreach(Player player in players)
        {
            if (!playerScores.ContainsKey(player.id))
            {
                playerScores[player.id] = 0;
                Debug.Log($"Intialized score for {player.name}: 0");
            }
        }
    }

    //add points to a player's score
    public void AddScore(string playerId, int points)
    {
        if (!playerScores.ContainsKey(playerId))
        {
            playerScores[playerId] = 0;
        }

        playerScores[playerId] += points;
        Debug.Log($"Player {playerId} score: {playerScores[playerId]} (+{points})");
    }

    public int GetScore(string playerId)
    {
        if (playerScores.TryGetValue(playerId, out int score))
        {
            return score;
        }
        return 0;
    }

    public void ResetAllScores()
    {
        playerScores.Clear();
        Debug.Log("All scores reset");
    }

    public void ResetScore(string playerId)
    {
        if (playerScores.ContainsKey(playerId))
        {
            playerScores[playerId] = 0;
            Debug.Log($"Reset score for player {playerId}");
        }
    }

    public List<KeyValuePair<string, int>> GetSortedScores()
    {
        var sortedList = new List<KeyValuePair<string, int>>(playerScores);
        sortedList.Sort((a, b) => b.Value.CompareTo(a.Value)); //descending order
        return sortedList;
    }

    public bool HasScore(string playerId)
    {
        return playerScores.ContainsKey(playerId);
    }
}
