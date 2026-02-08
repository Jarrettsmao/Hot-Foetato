using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
// using System.Diagnostics;

public class PlayerProfile : PlayerProfileBase
{
    [SerializeField] private TMP_Text statusText;
    
    public void SetupProfile(Player player)
    {
        SetupBase(player);
        SetStatus(player.isHost ? "Host" : "Not Ready");
        Debug.Log($"✅ Set up profile for player: {player.name} (ID: {player.id}, Host: {player.isHost}, Connected: {player.connected})");
    }

    public void SetStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    public void SetAsLocalPlayer(bool isLocal)
    {
        if (isLocal) statusText.text += " (You)";
    }
}