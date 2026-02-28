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

    [Header("Spinner")]
    [SerializeField] private GameObject spinner;

    [Header("Player/Bomb Positions")]
    [SerializeField] private Transform[] playerPositions;
    [SerializeField] private Transform[] bombTargets;

    [Header("Bomb Animator")]
    [SerializeField] private BombAnimator bombAnimator;
    [SerializeField] private float fallbackBombDurationSeconds = 20f;

    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;
    private GameObject explosion;

    [Header("Loser Text")]
    [SerializeField] private GameObject loserTextObject;

    private NetworkManager nm;
    private ScoreManager sm;
    private List<GameProfile> activeProfiles = new List<GameProfile>();
    private Dictionary<string, int> playerIdToSlot = new Dictionary<string, int>();

    public enum GameState
    {
        Lobby,
        CountDown,
        InGame,
        GameOver
    }
    private GameState currentGameState = GameState.Lobby;

    void Start()
    {
        nm = NetworkManager.Instance;
        sm = ScoreManager.Instance;

        SetupPlayers();
        RefreshUI();
        UpdateProfileClickability();

        nm.OnMessageReceived += OnMessageReceived;

        startButton.onClick.AddListener(OnStartClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);

        loserTextObject.SetActive(false);
        if (spinner != null)
        {
            spinner.SetActive(false);
        }

        //hide bomb initially
        if (bombAnimator)
        {
            bombAnimator.Hide();
        }
    }

    void OnDestroy()
    {
        if (nm)
        {
            nm.OnMessageReceived -= OnMessageReceived;
        }
        foreach (GameProfile profile in activeProfiles)
        {
            if (profile)
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
                //get score from manager
                int score = sm != null ? sm.GetScore(player.id) : 0;
                profile.SetupProfile(player, score); //starting score

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
            case "GAME_PREPARING":
                Debug.Log("Game Preparing");
                if (spinner != null)
                {
                    spinner.SetActive(true);
                }
                break;

            case "GAME_STARTED":
                if (spinner != null)
                {
                    spinner.SetActive(false);
                }
                Debug.Log("GameUI 🎮 Game started!");
                currentGameState = GameState.CountDown;
                RebuildPlayers();
                RefreshUI();

                //delete old explosion if it is still active
                Destroy(explosion);
                //disable loser text
                loserTextObject.SetActive(false);

                if (bombAnimator != null)
                {
                    bombAnimator.StopAnimation();
                    bombAnimator.Hide();
                }

                if (CountdownManager.Instance != null)
                {
                    if (message.countdownEndsAt > 0)
                    {
                        CountdownManager.Instance.StartCountdownTo(message.countdownEndsAt);
                    }
                    else
                    {
                        CountdownManager.Instance.StartCountdown();
                    }
                }
                break;

            case "GAME_LIVE":
                Debug.Log("GameUI 🚀 Game is live!");
                StartActualGame();
                break;

            case "POTATO_PASSED":
                Debug.Log("🥔 Potato passed!");
                UpdateProfileClickability();
                UpdateBombPosition();
                break;

            case "GAME_ENDED":
                Debug.Log($"💥 Game ended! Loser: {message.loser?.name}");
                currentGameState = GameState.GameOver;

                if (bombAnimator != null)
                {
                    bombAnimator.StopAnimation();
                    bombAnimator.Hide();
                }

                // ✅ Disable all profiles on game end
                foreach (GameProfile profile in activeProfiles)
                {
                    profile.SetClickable(false);
                }
                RefreshUI();

                if (message.loser != null)
                {
                    //add points to loser
                    if (sm != null)
                    {
                        sm.AddScore(message.loser.id, 1);
                        Debug.Log($"{message.loser.name} now has {sm.GetScore(message.loser.id)} points");

                        //update loser's score display
                        GameProfile loserProfile = activeProfiles.Find(p => p.GetPlayerId() == message.loser.id);
                        loserProfile?.SetScore(sm.GetScore(message.loser.id));
                    }

                    Transform bombTransform = GetBombTargetTransform(message.loser.id);
                    if (bombTransform != null)
                    {
                        StartCoroutine(ActivateExplosion(bombTransform, 2f));
                    }

                    //show loser message
                    SetLoserText(message.loser.name);
                }
                break;

            case "RETURN_TO_LOBBY":
                Debug.Log("🔙 Server requested return to lobby");

                //reset scores when returning to lobby
                sm?.ResetAllScores();

                ReturnToLobby();
                break;

            case "LEAVE_SUCCESS":
                Debug.Log("✅ Successfully left room");
                ReturnToLobby();
                break;

            case "HOST_TRANSFERRED":
                Debug.Log("👑 Host transferred during game");
                if (message.room != null && message.room.phase == "lobby")
                {
                    sm?.ResetAllScores();
                    ReturnToLobby();
                }
                break;

            case "ROOM_UPDATE":
                Debug.Log("📋 Room updated during game");
                if (message.room != null && message.room.phase == "lobby")
                {
                    sm?.ResetAllScores();
                    ReturnToLobby();
                }
                break;
        }
    }

    void OnProfileClicked(string targetPlayerId)
    {
        Debug.Log($"Profile clicked! Attempting to pass potato to {targetPlayerId}");

        if (currentGameState != GameState.InGame)
        {
            Debug.Log("Ignoring pass: game is not live yet.");
            return;
        }

        // Validate that I actually have the potato
        if (nm.CurrentRoom == null || nm.CurrentRoom.potatoHolderId != nm.MyPlayerId)
        {
            Debug.LogWarning("You don't have the potato!");
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
                currentGameState == GameState.InGame;

            profile.SetClickable(isClickable);
        }
    }

    void UpdateBombPosition()
    {
        if (nm.CurrentRoom == null || bombAnimator == null)
        {
            Debug.LogWarning("Cannot update bomb - room or animator is null");
            return;
        }

        string holderId = nm.CurrentRoom.potatoHolderId;

        if (string.IsNullOrEmpty(holderId))
        {
            Debug.LogWarning("Potato holder ID is null");
            bombAnimator.Hide();
            return;
        }

        if (!playerIdToSlot.TryGetValue(holderId, out int slot))
        {
            Debug.LogWarning($"Potato holder {holderId} not found in slot map");
            return;
        }

        if (slot < 0 || slot >= bombTargets.Length)
        {
            Debug.LogWarning($"Slot {slot} out of range");
            return;
        }

        if (!bombTargets[slot])
        {
            Debug.LogWarning($"Bomb target at slot {slot} is null");
            return;
        }

        //move bomb to the target position
        bombAnimator.SnapTo(bombTargets[slot]);
        Debug.Log($"bomb moved to slot {slot}");
    }

    IEnumerator ActivateExplosion(Transform target, float duration)
    {
        explosion = Instantiate(explosionPrefab, target);

        RectTransform explosionRect = explosion.GetComponent<RectTransform>();
        explosionRect.anchoredPosition = new Vector2(0f, 0f);
        explosionRect.localScale = Vector3.one;

        yield return new WaitForSeconds(duration);

        Destroy(explosion);
    }

    private void StartActualGame()
    {
        Debug.Log("Applying GAME_LIVE state");

        currentGameState = GameState.InGame;
        RebuildPlayers();
        RefreshUI();
        UpdateProfileClickability();

        // Start bomb animation with game duration
        if (bombAnimator != null && nm.CurrentRoom != null)
        {
            //calculate remaining time
            long serverEndTime = nm.CurrentRoom.endTime;
            long currentTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            float duration = (serverEndTime - currentTime) / 1000f;

            if (duration <= 0f)
            {
                Debug.LogWarning(
                    $"Invalid/expired duration from server. endTime={serverEndTime}, now={currentTime}. " +
                    $"Using fallback {fallbackBombDurationSeconds:F1}s."
                );
                duration = fallbackBombDurationSeconds;
            }

            if (duration > 0f)
            {
                UpdateBombPosition();
                bombAnimator.Show();
                bombAnimator.StartAnimation(duration);
                Debug.Log($"Bomb animation started: {duration:F1}s remaining");
            }
        }
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

    private Transform GetBombTargetTransform(string playerId)
    {
        if (!playerIdToSlot.TryGetValue(playerId, out int slot))
        {
            return null;
        }

        if (slot < 0 || slot >= bombTargets.Length) return null;
        if (bombTargets[slot] == null) return null;

        return bombTargets[slot];
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

    private void SetLoserText(string loserName)
    {
        string defaultText = " Exploded Into French Fries.";
        loserTextObject.SetActive(true);
        loserTextObject.GetComponent<TextMeshProUGUI>().text = loserName + defaultText;
    }
}

