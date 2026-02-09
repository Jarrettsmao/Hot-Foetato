using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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

    [Header("Game Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startText;
    [SerializeField] private Button leaveButton;

    private NetworkManager nm;
    private Transform[] playerPositions;
    private Transform[] bombTargets;
    private List<GameProfile> activeProfiles = new List<GameProfile>();

    public enum GameState
    {
        Lobby,
        InGame,
        GameOver
    }
    private GameState currentGameState = GameState.Lobby;

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

        RefreshUI();
        UpdateProfileClickability();

        nm.OnMessageReceived += OnMessageReceived;

        startButton.onClick.AddListener(OnStartClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);
    }

    void OnDestroy()
    {
        if (nm != null)
        {
            nm.OnMessageReceived -= OnMessageReceived;
        }

        foreach (GameProfile profile in activeProfiles)
        {
            if (profile != null)
            {
                profile.OnProfileClicked -= OnProfileClicked;
            }
        }
    }

    void SetupPlayers()
    {
        if (nm.CurrentRoom == null || nm.CurrentRoom.players == null)
        {
            return;
        }

        List<Player> players = nm.CurrentRoom.players;

        int yourIndex = players.FindIndex(p => p.id == nm.MyPlayerId);

        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            //caculate display position (make sure you are always on the bottom)
            int displayPosition = (i - yourIndex + players.Count) % players.Count;

            if (displayPosition >= playerPositions.Length)
            {
                continue;
            }

            GameObject profileObj = Instantiate(
                playerProfilePrefab,
                playerPositions[displayPosition]
            );

            GameProfile profile = profileObj.GetComponent<GameProfile>();
            //set defaults for profiles & their corresponding sprites
            if (profile != null)
            {
                profile.SetupProfile(player, 0); //starting score

                int spriteIndex = player.potatoIndex;
                Sprite sprite = potatoSpritesData.GetSprite(spriteIndex);
                profile.SetPotatoIcon(sprite);

                //subscribe to profile click (potato pass) event
                profile.OnProfileClicked += OnProfileClicked;

                activeProfiles.Add(profile);
            }
        }
        Debug.Log($"✅ Setup {activeProfiles.Count} player profiles in Game scene");
    }

    void OnMessageReceived(ServerMessage message)
    {
        switch (message.type)
        {
            case "GAME_STARTED":
                Debug.Log("🎮 Game started!");
                // UpdateBombPosition(immediate: true);
                currentGameState = GameState.InGame;
                RefreshUI();
                break;

            case "POTATO_PASSED":
                Debug.Log("🥔 Potato passed!");
                // UpdateBombPosition(immediate: false);
                UpdateProfileClickability();
                break;

            case "GAME_ENDED":
                Debug.Log($"💥 Game ended! Loser: {message.loser?.name}");
                // ShowExplosion(message.loser);
                currentGameState = GameState.GameOver;
                // ✅ Disable all profiles on game end
                foreach (GameProfile profile in activeProfiles)
                {
                    profile.SetClickable(false);
                }
                RefreshUI();
                break;

            case "RETURN_TO_LOBBY":
                Debug.Log("🔙 Server requested return to lobby");
                
                nm.ApplyRoomUpdate(message.room);
                ReturnToLobby();
                break;

            case "LEAVE_SUCCESS":
                Debug.Log("✅ Successfully left room");
                ReturnToLobby();
                break;

            case "ROOM_UPDATE":
                Debug.Log("📋 Room updated during game");
                break;
        }
    }

    void OnProfileClicked(string targetPlayerId)
    {
        Debug.Log($"🥔 Profile clicked! Attempting to pass potato to {targetPlayerId}");

        // Validate that I actually have the potato
        if (nm.CurrentRoom == null || nm.CurrentRoom.potatoHolderId != nm.MyPlayerId)
        {
            Debug.LogWarning("⚠️ You don't have the potato!");
            return;
        }

        nm.PassPotato(targetPlayerId);
    }

    void UpdateProfileClickability()
    {
        if (nm.CurrentRoom == null) return;

        string potatoHolderId = nm.CurrentRoom.potatoHolderId;
        bool iHaveThePotato = potatoHolderId == nm.MyPlayerId;

        //check if 1. i have the potato 2. if the profile is not me then set profile clickable
        foreach (GameProfile profile in activeProfiles)
        {
            string profilePlayerId = profile.GetPlayerId();

            bool isClickable = iHaveThePotato && profilePlayerId != nm.MyPlayerId;

            profile.SetClickable(isClickable);
        }
    }

    void ReturnToLobby()
    {
        Debug.Log("🔙 Loading Lobby scene...");
        SceneManager.LoadScene("Lobby");
    }

    void OnStartClicked()
    {
        if (currentGameState == GameState.Lobby)
        {
            nm.StartGame();
        }
        else if (currentGameState == GameState.GameOver)
        {
            nm.PlayAgain();
        }
    }

    void OnLeaveClicked()
    {
        Debug.Log("Leave button clicked");

        if (nm.CurrentRoom == null)
        {
            return;
        }

        nm.LeaveRoom();
    }

    void RefreshUI()
    {
        if (nm.CurrentRoom == null)
            return;

        bool isHost = nm.CurrentRoom.hostId == nm.MyPlayerId;

        // START BUTTON
        if (!isHost)
        {
            startButton.gameObject.SetActive(false);
        }
        else
        {
            startButton.gameObject.SetActive(true);

            switch (currentGameState)
            {
                case GameState.Lobby:
                    startText.text = "Start";
                    startButton.interactable = true;
                    break;

                case GameState.InGame:
                    startButton.interactable = false;
                    break;

                case GameState.GameOver:
                    startText.text = "Play Again";
                    startButton.interactable = true;
                    break;
            }
        }

        // LEAVE BUTTON (always available)
        leaveButton.gameObject.SetActive(true);
        UpdateProfileClickability();
    }

}
