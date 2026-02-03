using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button connectButton;
    [SerializeField] private TMP_Text statusText;

    void Start()
    {
        connectButton.onClick.AddListener(OnConnectClicked);
        statusText.enabled = false;
    }

    async void OnConnectClicked()
    {
        statusText.text = "Connecting...";
        statusText.enabled = true;
        connectButton.interactable = false;

        try
        {
            //go to lobby
            SceneManager.LoadScene("Lobby");
            Debug.Log("Loading Lobby Scene");
            
            //Connect to server
            await NetworkManager.Instance.Connect();
        }
        catch (System.Exception ex)
        {
            //Failed - show error and re enable button
            connectButton.interactable = true;
            statusText.text = $"Connection failed.";
            Debug.LogError($"Connection failed: {ex.Message}");
            return;
        }
    }
}
