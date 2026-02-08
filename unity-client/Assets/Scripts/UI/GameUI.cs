using UnityEngine;
using TMPro;
using UnityEngine.UI;
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

    [Header("Game Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startText;
    [SerializeField] private Button leaveButton;

    private NetworkManager nm;
    private Transform[] playerPositions;
    private Transform[] bombTargets;
    private List<GameProfile> activeProfiles = new List<GameProfile>();

    private bool gameEnded = false;

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

        startButton.interactable = false;

        SetupPlayers();
        UpdateProfileClickability();
        UpdateButtonStates();

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
                gameEnded = false;
                // UpdateBombPosition(immediate: true);
                UpdateProfileClickability();
                UpdateButtonStates();
                break;

            case "POTATO_PASSED":
                Debug.Log("🥔 Potato passed!");
                // UpdateBombPosition(immediate: false);
                UpdateProfileClickability();
                break;

            case "GAME_ENDED":
                Debug.Log($"💥 Game ended! Loser: {message.loser?.name}");
                // ShowExplosion(message.loser);
                gameEnded = true;
                // ✅ Disable all profiles on game end
                foreach (GameProfile profile in activeProfiles)
                {
                    profile.SetClickable(false);
                }
                UpdateButtonStates();
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

    void OnStartClicked()
    {
        if (startText.text == "Start")
        {
            nm.StartGame();
        } else
        {
            nm.PlayAgain();
        }
        UpdateStartButton("mid-game");
    }

    void OnLeaveClicked()
    {
        
    }

    void UpdateButtonStates()
    {
        if (nm.CurrentRoom == null) return;

        bool isHost = nm.CurrentRoom.hostId == nm.MyPlayerId;

        if (isHost)
        {
            UpdateStartButton("show");
            
        } 
        if (gameEnded)
        {
            UpdateStartButton("end");
            Debug.Log(isHost + " ending");
        }

        leaveButton.gameObject.SetActive(true);
    }

    void UpdateStartButton(string status)
    {
        switch (status){
            case "show":
                startText.text = "Start";
                startButton.gameObject.SetActive(true);
                startButton.interactable = true;
                Debug.Log("Start is showing");
                break;
            case "mid-game":
                startButton.interactable = false;
                Debug.Log("Start is mid-game");
                break;
            case "end":
                startText.text = "Play Again";
                startButton.interactable = true;
                Debug.Log("Start is end");
                break;
        }
    }
}
