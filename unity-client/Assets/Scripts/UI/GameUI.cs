using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameUI : MonoBehaviour
{
    [Header("Player Positions")]
    [SerializeField] private Transform playerPosBottom;
    [SerializeField] private Transform playerPosTop;
    [SerializeField] private Transform playerPosLeft;
    [SerializeField] private Transform playerPosRight;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerProfilePrefab;

    [Header("Potato Sprites")]
    [SerializeField] private PotatoSpritesData potatoSpritesData;

    private NetworkManager nm;
    private Transform[] playerPositions;
    private Transform[] bombTargets;
    private List<PlayerProfile> activeProfiles = new List<PlayerProfile>();

    void Start()
    {
        nm = NetworkManager.Instance;

        playerPositions = new Transform[]
        {
            playerPosBottom,
            playerPosTop,
            playerPosLeft,
            playerPosRight
        };

        SetupPlayers();

        nm.OnMessageReceived += OnMessageReceived;
    }

    void OnDestroy()
    {
        if (nm != null)
        {
            nm.OnMessageReceived -= OnMessageReceived;
        }
    }

    void SetupPlayers()
    {
        if (nm.CurrentRoom == null || nm.CurrentRoom.players== null)
        {
            return;
        }

        List<Player> players = nm.CurrentRoom.players;

        int yourIndex = players.FindIndex(p => p.id == nm.MyPlayerId);
        
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];

            int displayPosition = (i - yourIndex + players.Count) % players.Count;

            if (displayPosition >= playerPositions.Length)
            {
                continue;
            }

            GameObject profileObj = Instantiate(
                playerProfilePrefab,
                playerPositions[displayPosition]
            );

            PlayerProfile profile = profileObj.GetComponent<PlayerProfile>();

            if (profile != null)
            {
                profile.SetupProfile(player);

                activeProfiles.Add(profile);
            }
        }
        Debug.Log($"✅ Setup {activeProfiles.Count} player profiles in Game scene");
    }

    void OnMessageReceived(ServerMessage message)
    {
        switch (message.type)
        {
            case "POTATO_PASSED":
                Debug.Log("Potato passed!");
                // UpdatePotatoVisuals();
                break;
            case "GAME_ENDED":
                Debug.Log("Game ended! Loser: {message.loser?.name}");
                // ShowExplosion();
                break;
        }
    }

    // void ShowExplosion()
    // {
    //     explosionEffect.SetActive(true);
    // }
}
