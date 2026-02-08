using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerProfileBase : MonoBehaviour
{
    [Header("Common UI")]
    [SerializeField] protected Image potatoSprite;
    [SerializeField] protected TMP_Text usernameText;
    [SerializeField] private Image crownIcon;

    protected string playerId;

    public virtual void SetupBase(Player player)
    {
        playerId = player.id;
        SetUsername(player.name);
        SetHost(player.isHost);
    }

    public void SetPotatoIcon(Sprite sprite)
    {
        Debug.Log("Setting potato icon for player " + sprite.name);
        if (potatoSprite == null || sprite == null) return;
        potatoSprite.sprite = sprite;
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

    public void SetHost(bool isHost)
    {
        if (crownIcon != null)
        {
            crownIcon.gameObject.SetActive(isHost);
        }
    }
}
