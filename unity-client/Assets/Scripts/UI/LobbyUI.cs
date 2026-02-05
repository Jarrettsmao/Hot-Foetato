using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
// using System.Diagnostics;

public class LobbyUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button joinCreateButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button randomButton;

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private TMP_InputField roomIdInputField;

    [Header("Player List")]
    [SerializeField] private Transform playerProfileContainer;  // The Grid Layout parent
    [SerializeField] private TMP_Text playerListTitleText;
    private List<GameObject> playerProfiles = new List<GameObject>();
    

    void Start()
    {
        NetworkManager.Instance.OnMessageReceived += OnMessageReceived;

        joinCreateButton.onClick.AddListener(OnJoinCreateClicked);
        startButton.onClick.AddListener(OnStartClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);
        randomButton.onClick.AddListener(OnRandomClicked);

        startButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(false);

        HideAllPlayerProfiles();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnJoinCreateClicked()
    {
        Debug.Log("Join Status Clicked");
        
        string roomId = roomIdInputField.text;
        string playerName = playerNameInputField.text;

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Player name is required to join or create a room.");
            return;
        }

        if (string.IsNullOrEmpty(roomId))
        {
            Debug.Log($"Creating room with ID: {roomId}");
        }
    }

    void OnStartClicked()
    {
        Debug.Log("Start Clicked");
        // Implement start game logic here
    }

    void OnMessageReceived(ServerMessage message)
    {
        Debug.Log("On Message Received in LobbyUI");
    }

    void OnLeaveClicked()
    {
        Debug.Log("Leave Clicked");
        // Implement leave room logic here
    }

    void OnRandomClicked()
    {
        Debug.Log("Random Clicked");
        string roomText = GenerateRoomCode();
        roomIdInputField.text = roomText;
    }
    
    //Header Methods 
    public string GenerateRoomCode()
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

    public void HideAllPlayerProfiles()
    {
        for (int i = 0; i < playerProfileContainer.childCount; i++)
        {
            GameObject profile = playerProfileContainer.GetChild(i).gameObject;
            playerProfiles.Add(profile);
            profile.SetActive(false);
            
        }
    }

    public void UpdatePlayerListTitle()
    {
        
    }
}
