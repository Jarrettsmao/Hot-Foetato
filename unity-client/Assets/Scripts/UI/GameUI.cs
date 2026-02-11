using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameUI : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject playerProfilePrefab;

    [Header("Potato Sprites")]
    [SerializeField] private PotatoSpritesData potatoSpritesData;

    [Header("Game Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startText;
    [SerializeField] private Button leaveButton;

    private NetworkManager nm;
    [Header("Player/Bomb Positions")]
    [SerializeField] private Transform[] playerPositions;
    [SerializeField] private GameObject[] bombIndicators;

    private List<GameProfile> activeProfiles = new List<GameProfile>();
    private Dictionary<string, int> playerIdToSlot = new Dictionary<string, int>();
    [SerializeField] private GameObject explosionPrefab;
    private GameObject explosion;
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

        SetupPlayers();

        RefreshUI();
        UpdateProfileClickability();

        nm.OnMessageReceived += OnMessageReceived;

        startButton.onClick.AddListener(OnStartClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);

        // UpdateBombIndictator(false);
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
            //caculate display position (make sure you are always on the bottom) and store it in dictionary
            int displayPosition = (i - yourIndex + players.Count) % players.Count;
            playerIdToSlot[player.id] = displayPosition;

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
                Debug.Log("GameUI 🎮 Game started!");
                currentGameState = GameState.InGame;
                RebuildPlayers();
                RefreshUI();
                UpdateBombIndictator(true);

                //delete old explosion if it is still active
                Destroy(explosion);

                break;

            case "POTATO_PASSED":
                Debug.Log("🥔 Potato passed!");
                UpdateProfileClickability();
                UpdateBombIndictator(true);
                break;

            case "GAME_ENDED":
                Debug.Log($"💥 Game ended! Loser: {message.loser?.name}");
                currentGameState = GameState.GameOver;
                // ✅ Disable all profiles on game end
                foreach (GameProfile profile in activeProfiles)
                {
                    profile.SetClickable(false);
                }
                RefreshUI();
                UpdateBombIndictator(false);
                if (message.loser != null)
                {
                    Transform loserTransform = GetBombSlotTransform(message.loser.id);
                    if (loserTransform != null)
                    {
                        StartCoroutine(ActivateExplosion(loserTransform, 2f));
                    }
                }
                break;

            case "RETURN_TO_LOBBY":
                Debug.Log("🔙 Server requested return to lobby");

                // nm.ApplyRoomUpdate(message.room);
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

    private void UpdateProfileClickability()
    {
        if (nm.CurrentRoom == null) return;

        string potatoHolderId = nm.CurrentRoom.potatoHolderId;
        bool iHaveThePotato = potatoHolderId == nm.MyPlayerId;

        //check if 1. i have the potato 2. if the profile is not me then set profile clickable
        //3. not game over
        foreach (GameProfile profile in activeProfiles)
        {
            string profilePlayerId = profile.GetPlayerId();

            bool isClickable =
                iHaveThePotato &&
                profilePlayerId != nm.MyPlayerId &&
                currentGameState != GameState.GameOver;

            profile.SetClickable(isClickable);
        }
    }

    void UpdateBombIndictator(bool status)
    {
        Debug.Log("Holder according to UI: " + nm.CurrentRoom.potatoHolderId);
        if (nm.CurrentRoom == null) return;
        Debug.Log("room is not empty according to bomb" + status);
        foreach (var bomb in bombIndicators)
        {
            SetImageVisible(bomb, false);
        }
        if (status)
        {
            string holderId = nm.CurrentRoom.potatoHolderId;
            if (string.IsNullOrEmpty(holderId)) return;

            if (!playerIdToSlot.TryGetValue(holderId, out int slot))
            {
                Debug.LogWarning("Potato holder not found in slot map");
                return;
            }

            SetImageVisible(bombIndicators[slot], true);

            Debug.Log($"Bomb active at slot {slot}");
        }
    }

    //sets alpha to 255 or 0 depending on bool
    private void SetImageVisible(GameObject gameObject, bool visible)
    {
        Image image = gameObject.GetComponent<Image>();
        if (image == null) return;

        Color c = image.color;
        c.a = visible ? 1f : 0f;
        image.color = c;
    }

    IEnumerator ActivateExplosion(Transform target, float duration)
    {
        explosion = Instantiate(explosionPrefab, target);

        RectTransform explosionRect = explosion.GetComponent<RectTransform>();
        explosionRect.anchoredPosition = Vector2.zero;
        explosionRect.localScale = Vector3.one;

        yield return new WaitForSeconds(duration);

        Destroy(explosion);
    }


    private void ReturnToLobby()
    {
        Debug.Log("🔙 Loading Lobby scene...");
        SceneManager.LoadScene("Lobby");
    }

    private void OnStartClicked()
    {
        if (currentGameState == GameState.Lobby)
        {
            startButton.interactable = false;
            nm.StartGame();
        }
        else if (currentGameState == GameState.GameOver)
        {
            nm.PlayAgain();
        }
    }

    private void OnLeaveClicked()
    {
        Debug.Log("Leave button clicked");

        if (nm.CurrentRoom == null)
        {
            return;
        }

        nm.LeaveRoom();
    }

    private void RefreshUI()
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

    private Transform GetBombSlotTransform(string playerId)
    {
        if (!playerIdToSlot.TryGetValue(playerId, out int slot))
        {
            return null;
        }

        if (slot < 0 || slot >= playerPositions.Length) return null;

        return playerPositions[slot];
    }

    private void RebuildPlayers()
    {
        foreach (GameProfile profile in activeProfiles)
        {
            if (profile != null)
                Destroy(profile.gameObject);
        }

        activeProfiles.Clear();
        playerIdToSlot.Clear();

        SetupPlayers();
    }

}
