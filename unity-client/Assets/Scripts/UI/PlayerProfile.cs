using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
// using System.Diagnostics;

public class PlayerProfile : MonoBehaviour
{
    [SerializeField] private Image potatoSprite;
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image crownIcon;

    private string playerId;

    public void SetupProfile(Player player)
    {
        playerId = player.id;
        SetUsername(player.name);
        SetHost(player.isHost);
        SetStatus("Not Ready");
        Debug.Log($"✅ Set up profile for player: {player.name} (ID: {player.id}, Host: {player.isHost}, Connected: {player.connected})");
    }

    public void SetPotatoIcon(Sprite sprite)
    {
        Debug.Log("Setting potato icon for player " + sprite.name);
        potatoSprite.sprite = sprite;
    }

    public void SetHost(bool isHost)
    {
        if (crownIcon != null)
        {
            crownIcon.gameObject.SetActive(isHost);
        }
    }

    public void SetStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    public void SetUsername(string username)
    {
        if (usernameText != null)
        {
            usernameText.text = username;
        }
    }

    public string GetPlayerId()
    {
        return playerId;
    }

    public void SetAsLocalPlayer(bool isLocal)
    {
        if (isLocal) statusText.text += " (You)";
    }
}