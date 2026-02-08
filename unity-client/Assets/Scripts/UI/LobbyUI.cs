using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
// using System.Diagnostics;

public class LobbyUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button joinCreateButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button randomButton;

    [Header("Ready Button")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button unreadyButton;

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private TMP_InputField roomIdInputField;

    [Header("Player List")]
    [SerializeField] private Transform playerProfileContainer;  // The Grid Layout parent
    [SerializeField] private TMP_Text playerListTitleText;
    [SerializeField] private GameObject playerProfilePrefab;
    private List<PlayerProfile> playerProfiles = new List<PlayerProfile>();

    [Header("Potato Sprites")]
    [SerializeField] private Sprite[] potatoSprites;
    private int potatoIndex;
    private NetworkManager nm;

    void Start()
    {
        nm = NetworkManager.Instance;
        nm.OnMessageReceived += OnMessageReceived;

        joinCreateButton.onClick.AddListener(OnJoinCreateClicked);
        startButton.onClick.AddListener(OnStartClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);
        randomButton.onClick.AddListener(OnRandomClicked);
        readyButton.onClick.AddListener(OnReadyClicked);
        unreadyButton.onClick.AddListener(OnReadyClicked);

        startButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(false);
        readyButton.gameObject.SetActive(false);
        unreadyButton.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (nm != null)
        {
            nm.OnMessageReceived -= OnMessageReceived;
        }
    }

    void OnMessageReceived(ServerMessage message)
    {
        switch (message.type)
        {
            case "JOIN_SUCCESS":
                Debug.Log("✅ Joined room successfully!");
                UpdateUI();
                joinCreateButton.gameObject.SetActive(false);
                leaveButton.gameObject.SetActive(true);
                break;

            case "LEAVE_SUCCESS":
                Debug.Log("🚪 Left room successfully");
                joinCreateButton.gameObject.SetActive(true);
                leaveButton.gameObject.SetActive(false);
                UpdateUI();
                break;

            case "HOST_TRANSFERRED":
                Debug.Log("👑 Host transferred");
                UpdateUI();
                break;

            case "ROOM_UPDATE":
                Debug.Log("📋 Room updated");
                UpdateUI();
                break;

            case "GAME_ROOM":
                Debug.Log("🎮 Game started!");
                SceneManager.LoadScene("Game");
                break;

            case "ERROR":
                Debug.LogError($"❌ Server error: {message.message}");
                break;
        }
    }

    //update methods
    void UpdatePlayerList()
    {
        ClearPlayerList();
        if (nm.CurrentRoom == null || nm.CurrentRoom.players == null)
        {
            Debug.LogWarning("⚠️ No room or players to display!");
            return;
        }

        for (int i = 0; i < nm.CurrentRoom.players.Count; i++)
        {
            Player player = nm.CurrentRoom.players[i];

            GameObject profileObj = Instantiate(playerProfilePrefab, playerProfileContainer);
            PlayerProfile profile = profileObj.GetComponent<PlayerProfile>();

            if (profile != null)
            {
                profile.SetupProfile(player);

                if (player.isHost)
                {
                    profile.SetStatus("Host");
                }
                else if (player.isReady)
                {
                    profile.SetStatus("Ready");
                }
                else
                {
                    profile.SetStatus("Waiting");
                }

                //assign potato sprite
                if (potatoSprites != null && potatoSprites.Length > 0)
                {
                    profile.SetPotatoIcon(potatoSprites[player.potatoIndex]);
                }

                if (player.id == nm.MyPlayerId)
                {
                    profile.SetAsLocalPlayer(true);
                }

                playerProfiles.Add(profile);
            }
        }

        UpdatePlayerListTitle();
    }

    private void UpdatePlayerListTitle()
    {
        if (playerListTitleText == null)
        {
            Debug.LogWarning("⚠️ playerListTitleText not assigned!");
            return;
        }

        if (nm.CurrentRoom == null)
        {
            Debug.LogWarning("⚠️ Not in a room!");
            playerListTitleText.text = "Join A Room | Players (0/4)";
            return;
        }

        if (nm.CurrentRoom.players == null)
        {
            Debug.LogWarning("⚠️ CurrentRoom.players is null!");
            playerListTitleText.text = "Join A Room | Players (0/4)";
            return;
        }

        int playerCount = nm.CurrentRoom.players.Count;
        int maxPlayers = nm.CurrentRoom.maxPlayers;

        playerListTitleText.text = $"Room {nm.CurrentRoom.roomId} | Players ({playerCount}/{maxPlayers})";

        Debug.Log($"✅ Updated player list: {playerCount}/{maxPlayers}");
    }

    private void UpdateStartButton()
    {
        // Default: OFF
        startButton.gameObject.SetActive(false);

        if (nm.CurrentRoom == null || nm.CurrentRoom.players == null)
            return;

        if (nm.CurrentRoom.players.Count < 2)
            return;

        Player localPlayer = nm.CurrentRoom.players.Find(p => p.id == nm.MyPlayerId);
        if (localPlayer == null || !localPlayer.isHost)
            return;

        if (!CheckAllReady())
            return;

        // Only reachable if ALL conditions pass
        startButton.gameObject.SetActive(true);
    }


    private void UpdateReadyButton()
    {
        readyButton.gameObject.SetActive(false);
        unreadyButton.gameObject.SetActive(false);

        if (nm.CurrentRoom == null || nm.CurrentRoom.players == null)
            return;

        Player localPlayer = nm.CurrentRoom.players
            .Find(p => p.id == nm.MyPlayerId);

        if (localPlayer == null)
            return;

        // Host should never see ready button
        if (localPlayer.isHost)
            return;

        // Only show when enough players
        if (nm.CurrentRoom.players.Count < 2)
            return;

        readyButton.gameObject.SetActive(true);

        if (localPlayer.isReady)
        {
            unreadyButton.gameObject.SetActive(true);
        }
        else
        {
            readyButton.gameObject.SetActive(true);
        }
    }

    //On button click
    void OnJoinCreateClicked()
    {
        Debug.Log("Join Status Clicked");

        string roomId = roomIdInputField.text;
        string playerName = playerNameInputField.text;

        if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(roomId))
        {
            Debug.LogWarning("Player name and room ID are required to join or create a room.");
            return;
        }

        ControlInputUI(false);

        // TEMP: assign by join order (future = player choice)
        // int potatoIndex = 0;
        // if (nm.CurrentRoom != null && nm.CurrentRoom.players != null)
        // {
        //     int playerCount = nm.CurrentRoom.players.Count;
        //     potatoIndex = playerCount % potatoSprites.Length;
        // }
        
        nm.JoinRoom(roomId, playerName, -1);
    }

    void OnStartClicked()
    {
        Debug.Log("Start Clicked");

        nm.MoveToGameRoom();
    }

    void OnLeaveClicked()
    {
        Debug.Log("Leave Clicked");
        nm.LeaveRoom();
        ControlInputUI(true);
    }

    void OnRandomClicked()
    {
        Debug.Log("Random Clicked");
        string roomText = GenerateRoomCode();
        roomIdInputField.text = roomText;
    }

    void OnReadyClicked()
    {
        Debug.Log("Ready Clicked");
        nm.ToggleReady();

    }

    //helper Methods 
    private string GenerateRoomCode()
    {
        const string chars = "ABCEDGHIJKLMNOPQRSTUVWXYZ0123456789";
        const int length = 6;

        string code = "";
        for (int i = 0; i < length; i++)
        {
            code += chars[Random.Range(0, chars.Length)];
        }

        return code;
    }

    private void ClearPlayerList()
    {
        foreach (PlayerProfile profile in playerProfiles)
        {
            Destroy(profile.gameObject);
        }
        playerProfiles.Clear();
    }

    private void UpdateUI()
    {
        UpdatePlayerListTitle();
        UpdatePlayerList();
        UpdateStartButton();
        UpdateReadyButton();
    }

    private void ControlInputUI(bool status)
    {
        roomIdInputField.interactable = status;
        playerNameInputField.interactable = status;
        randomButton.interactable = status;

    }

    private bool CheckAllReady()
    {
        bool allReady = true;
        foreach (Player player in nm.CurrentRoom.players)
        {
            if (player.isHost)
            {
                continue;
            }
            if (!player.isReady)
            {
                allReady = false;
                break;
            }
        }

        return allReady;
    }
}

