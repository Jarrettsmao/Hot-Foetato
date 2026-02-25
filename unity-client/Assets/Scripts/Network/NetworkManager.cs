using System;
// using System.Diagnostics;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    private WebSocket websocket;
    public string serverUrl = "wss://hot-foetato.onrender.com/";

    //events for other scripts to listen to
    public event Action<ServerMessage> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    public string MyPlayerId { get; private set; }
    public GameRoom CurrentRoom { get; private set; }

    public string MyPlayerName { get; private set; }

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
                    MyPlayerId = message.playerId;
                    CurrentRoom = message.room;
                    Debug.Log($"🆔 My Player ID: {MyPlayerId}");

                    if (message.room.hostId == MyPlayerId)
                    {
                        Debug.Log("👑 I am the host");
                    }
                    break;
                case "LEAVE_SUCCESS":
                    Debug.Log("🚪 Left room successfully");
                    CurrentRoom = null;
                    MyPlayerId = null;
                    MyPlayerName = null;
                    break;
                case "RETURN_TO_LOBBY":
                    if (message.room != null)
                    {
                        CurrentRoom = message.room;
                        Debug.Log($"🔙 Returned to lobby, still in room: {CurrentRoom.roomId}");
                        Debug.Log($"   Players: {CurrentRoom.players.Count}");
                        Debug.Log($"   Host: {CurrentRoom.hostId}, Am I host? {CurrentRoom.hostId == MyPlayerId}");

                        // Log all players for debugging
                        foreach (var p in CurrentRoom.players)
                        {
                            Debug.Log($"   Player: {p.name}, isHost: {p.isHost}, isReady: {p.isReady}");
                        }
                    }
                    break;
                case "ROOM_UPDATE":
                    CurrentRoom = message.room;
                    Debug.Log("📋 Room updated");
                    break;
                case "GAME_STARTED":
                    CurrentRoom = message.room;
                    Debug.Log("NM 🎮 Game started");
                    break;
                case "POTATO_PASSED":
                    CurrentRoom = message.room;
                    Debug.Log("🥔 Potato passed");
                    break;
                case "GAME_ENDED":
                    Debug.Log($"💥 Game ended! Loser: {message.loser?.name}");
                    CurrentRoom = message.room;
                    break;
                case "HOST_TRANSFERRED":
                    CurrentRoom = message.room;
                    if (message.newHostId == MyPlayerId)
                    {
                        Debug.Log("👑 You are the new host");
                    }
                    else
                    {
                        Debug.Log($"👑 New host is player {message.newHostId}");
                    }
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
    public void JoinRoom(string roomId, string playerName, int potatoIndex)
    {
        MyPlayerName = playerName;

        JoinRoomMessage message = new JoinRoomMessage
        {
            roomId = roomId,
            playerName = playerName,
            potatoIndex = potatoIndex
        };

        SendMessage(message);
    }

    public void StartGame()
    {
        SendMessage(new StartGameMessage());
    }

    public void MoveToGameRoom()
    {
        SendMessage(new GameRoomMessage());
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

    public void LeaveRoom()
    {
        SendMessage(new LeaveRoomMessage());
        Debug.Log("Left the room.");
    }

    public void ToggleReady()
    {
        SendMessage(new ToggleReadyMessage());
        Debug.Log("📤 Toggling ready status");
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
