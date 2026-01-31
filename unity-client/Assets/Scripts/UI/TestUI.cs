using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;

public class TestUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField roomIdInput;
    public TMP_InputField playerNameInput;
    public Button connectButton;
    public Button joinButton;
    public Button startButton;
    public TMP_Text statusText;
    public TMP_Text playersText;

    private NetworkManager nm;
    void Start()
    {
        nm = NetworkManager.Instance;

        //set up button listeners
        connectButton.onClick.AddListener(OnConnectClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        startButton.onClick.AddListener(OnStartClicked);

        //disable buttons on start
        joinButton.interactable = false;
        startButton.interactable = false;

        //listen to network events
        nm.OnConnected += OnConnected;
        nm.OnMessageReceived += OnMessageReceived;

        UpdateStatus("Not connected");
    }

    async void OnConnectClicked()
    {
        UpdateStatus("Connecting...");
        connectButton.interactable = false;
        await nm.Connect();
    }

    void OnConnected()
    {
        UpdateStatus("Connected! Ready to join a room.");
        joinButton.interactable = true;
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
    }

    void OnMessageReceived(ServerMessage message)
    {
        switch (message.type)
        {
            case "JOIN_SUCCESS":
                UpdateStatus($"Joined room successfully!");
                startButton.interactable = true;
                break;

            case "ROOM_UPDATE":
                UpdatePlayerList(message.room);
                break;

            case "GAME_STARTED":
                UpdateStatus("Game started!");
                startButton.interactable = false;
                break;

            case "ERROR":
                UpdateStatus($"Error: {message.message}");
                break;
        }
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
