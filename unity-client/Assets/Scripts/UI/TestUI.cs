using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;
using System.Collections.Generic;

public class TestUI : MonoBehaviour
{
    [Header("Connection")]
    public TMP_InputField roomIdInput;
    public TMP_InputField playerNameInput;
    public Button connectButton;
    public Button joinButton;

    [Header("Game Controls")]
    public Button startButton;
    public Button playAgainButton;

    [Header("Display")]
    public TMP_Text statusText;
    public TMP_Text playersText;
    public TMP_Text roomInfoText;

    [Header("Pass Potato")]
    public Transform passButtonContainer;
    public GameObject passButtonPrefab;

    private NetworkManager nm;
    private List<GameObject> activePassButtons = new List<GameObject>();
    void Start()
    {
        nm = NetworkManager.Instance;

        //set up button listeners
        connectButton.onClick.AddListener(OnConnectClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        startButton.onClick.AddListener(OnStartClicked);
        playAgainButton.onClick.AddListener(OnPlayAgainClicked);

        //disable buttons on start
        joinButton.interactable = false;
        startButton.interactable = false;
        playAgainButton.interactable = false;

        //listen to network events
        nm.OnConnected += OnConnected;
        nm.OnMessageReceived += OnMessageReceived;
        nm.OnDisconnected += OnDisconnected;

        UpdateStatus("Click Connect to begin");
    }

    async void OnConnectClicked()
    {
        UpdateStatus("Connecting...");
        connectButton.interactable = false;

        try
        {
            await nm.Connect();
        }
        catch (System.Exception e)
        {
            UpdateStatus($"Connection failed: {e.Message}");
            connectButton.interactable = true;
        }
    }

    void OnConnected()
    {
        UpdateStatus("Connected! Ready to join a room.");
        joinButton.interactable = true;
    }

    void OnDisconnected()
    {
        UpdateStatus("Disconnected from server.");
        connectButton.interactable = true;
        joinButton.interactable = false;
        startButton.interactable = false;
        playAgainButton.interactable = false;
        ClearPassButtons();
    }

    void OnJoinClicked()
    {
        string roomId = roomIdInput.text;
        string playerName = playerNameInput.text;

        if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(playerName))
        {
            UpdateStatus("Please enter room ID and player name.");
            return;
        }

        nm.JoinRoom(roomId, playerName);
        UpdateStatus($"Joining room {roomId} as {playerName}...");
        joinButton.interactable = false;
    }

    void OnStartClicked()
    {
        nm.StartGame();
        UpdateStatus("Starting game...");
    }

    void OnPlayAgainClicked()
    {
        nm.PlayAgain();
        UpdateStatus("Requesting new game...");
        playAgainButton.interactable = false;
        startButton.interactable = true;
    }

    void OnMessageReceived(ServerMessage message)
    {
        switch (message.type)
        {
            case "JOIN_SUCCESS":
                UpdateStatus($"Joined room {message.room.roomId} successfully!");
                UpdateRoomDisplay(message.room);
                break;

            case "ROOM_UPDATE":
                UpdatePlayerList(message.room);
                UpdateRoomDisplay(message.room);
                if (!string.IsNullOrEmpty(message.message))
                {
                    UpdateStatus(message.message);
                }
                break;

            case "HOST_TRANSFERRED":
                UpdateRoomDisplay(message.room);
                if (message.newHostId == nm.MyPlayerId)
                {
                    UpdateStatus("👑 You are the new host!");
                }
                if (!string.IsNullOrEmpty(message.message))
                {
                    UpdateStatus(message.message);
                }
                break;

            case "GAME_STARTED":
                UpdateStatus("Game started!");
                startButton.interactable = false;
                UpdateRoomDisplay(message.room);
                UpdatePassButtons(message.room);
                break;

            case "POTATO_PASSED":
                UpdateStatus($"🥔 {message.message}");
                UpdateRoomDisplay(message.room);
                UpdatePassButtons(message.room);
                break;

            case "GAME_ENDED":
                UpdateStatus($"💥 {message.message}");
                UpdateRoomDisplay(message.room);
                playAgainButton.interactable = true;
                ClearPassButtons();
                break;

            case "ERROR":
                UpdateStatus($"Error: {message.message}");
                break;
        }
    }

    void UpdateRoomDisplay(GameRoom room)
    {
        if (room == null)
        {
            Debug.LogWarning("UpdateRoomDisplay: room is null");
            roomInfoText.text = "Not in a room.";
            playersText.text = "";
            return;
        }

        if (room.players == null)
        {
            Debug.LogWarning("UpdateRoomDisplay: room.players is null");
            roomInfoText.text = $"Room: {room.roomId}";
            playersText.text = "Loading players...";
            return;
        }

        //room info
        roomInfoText.text = $"Room: {room.roomId} | Phase: {room.phase}";

        //player list
        string playerList = $" | Players: ({room.players.Count}/{room.maxPlayers}):\n\n";
        foreach (var player in room.players)
        {
            string line = "";
            // Host indicator
            if (player.isHost)
            {
                line += "👑 ";
            }
            // Potato indicator
            else if (room.potatoHolderId == player.id)
            {
                line += "🥔 ";
            }
            else
            {
                line += "   ";
            }

            // Player name
            line += player.name;

            // You indicator
            if (player.id == nm.MyPlayerId)
            {
                line += " (You)";
            }

            playerList += line + "\n";
        }

        playersText.text = playerList;

        bool amIHost = room.hostId == nm.MyPlayerId;
        //Update start button
        bool canStart = amIHost && room.phase == "lobby" && room.players.Count >= 2;
        Debug.Log($"Can start: {canStart} (Am I host: {amIHost}, Phase: {room.phase}, Players: {room.players.Count})");
        startButton.interactable = canStart;

        // Only enable play again if I'm host
        if (room.phase == "ended")
        {
            playAgainButton.interactable = amIHost;
        }
        else
        {
            playAgainButton.interactable = false;
        }

        // Status message
        if (room.phase == "lobby" && room.players.Count < 2)
        {
            if (amIHost)
            {
                UpdateStatus($"👑 You're the host! Waiting for players... ({room.players.Count}/2)");
            }
            else
            {
                UpdateStatus($"Waiting for host to start... ({room.players.Count}/2)");
            }
        }
    }

    void UpdatePassButtons(GameRoom room)
    {
        ClearPassButtons();

        //show pass button is 1. game active 2. has potato
        if (room.phase != "playing" || room.potatoHolderId != nm.MyPlayerId)
        {
            return; //not player's turn
        }

        //create button for each other player
        foreach (var player in room.players)
        {
            //skip myself
            if (player.id == nm.MyPlayerId) continue;

            //create button
            GameObject btnObj = Instantiate(passButtonPrefab, passButtonContainer);
            Button btn = btnObj.GetComponent<Button>();
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();

            btnText.text = $"Pass to {player.name}";

            //Catpture player ID
            string targetId = player.id;
            btn.onClick.AddListener(() => OnPassButtonClicked(targetId, player.name));

            activePassButtons.Add(btnObj);
        }
    }

    void OnPassButtonClicked(string targetPlayerId, string targetName)
    {
        nm.PassPotato(targetPlayerId);
        UpdateStatus($"Passing potato to {targetName}...");
        ClearPassButtons();
    }

    void ClearPassButtons()
    {
        foreach (var btn in activePassButtons)
        {
            Destroy(btn);
        }
        activePassButtons.Clear();
    }

    void UpdateStatus(string status)
    {
        statusText.text = status;
    }

    void UpdatePlayerList(GameRoom room)
    {
        string playerList = "Players: \n";
        foreach (var player in room.players)
        {
            playerList += $"- {player.name}\n";
        }
        playersText.text = playerList;
    }
}
