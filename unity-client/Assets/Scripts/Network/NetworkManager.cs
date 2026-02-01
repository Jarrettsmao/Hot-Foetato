using System;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    private WebSocket websocket;
    public string serverUrl = "ws://localhost:8080";

    //events for other scripts to listen to
    public event Action<ServerMessage> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    public string myPlayerId { get; private set; }
    public GameRoom currentRoom { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task Connect()
    {
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ Connected to server!");
            OnConnected?.Invoke();
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("📩 Received: " + message);
            HandleMessage(message);
        };

        websocket.OnError += (error) =>
        {
            Debug.LogError("❌ WebSocket Error: " + error);
        };

        websocket.OnClose += (code) =>
        {
            Debug.Log("❌ Disconnected from server!");
            OnDisconnected?.Invoke();
        };

        await websocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    void HandleMessage(string messageJson)
    {
        try
        {
            ServerMessage message = JsonUtility.FromJson<ServerMessage>(messageJson);

            //Handle special message types
            switch (message.type)
            {
                case "JOIN_SUCCESS":
                    myPlayerId = message.playerId;
                    Debug.Log($"🆔 My Player ID: {myPlayerId}");
                    break;

                case "ROOM_UPDATE":
                case "GAME_STARTED":
                case "POTATO_PASSED":
                case "GAME_ENDED":
                    Debug.Log("sending game ended");
                    currentRoom = message.room;
                    break;

                case "ERROR":
                    Debug.LogWarning($"⚠️ Server Error: {message.message}");
                    break;
            }
            OnMessageReceived?.Invoke(message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Failed to parse server message: {ex.Message}");
        }
    }

    //send messages to server
    public void JoinRoom(string roomId, string playerName)
    {
        JoinRoomMessage message = new JoinRoomMessage
        {
            roomId = roomId,
            playerName = playerName
        };
        SendMessage(message);
    }

    public void StartGame()
    {
        SendMessage(new StartGameMessage());
    }

    public void PassPotato(string targetPlayerId)
    {
        PassPotatoMessage message = new PassPotatoMessage
        {
            targetPlayerId = targetPlayerId
        };
        SendMessage(message);
    }

    public void PlayAgain()
    {
        SendMessage(new PlayAgainMessage());
    }

    //helper function to send any message
    void SendMessage(object message)
    {
        if (websocket.State == WebSocketState.Open)
        {
            string messageJson = JsonUtility.ToJson(message);
            Debug.Log("📤 Sending: " + messageJson);
            websocket.SendText(messageJson);
        }
        else
        {
            Debug.LogWarning("⚠️ WebSocket not connected!");
        }
    }

    void OnApplicationQuit()
    {
        websocket?.Close();
    }
}
