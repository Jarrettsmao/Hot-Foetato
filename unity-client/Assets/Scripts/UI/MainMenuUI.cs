using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class MainMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button connectButton;
    [SerializeField] private TMP_Text statusText;

    private NetworkManager nm;

    void Start()
    {
        nm = NetworkManager.Instance;
        connectButton.onClick.AddListener(OnConnectClicked);
        statusText.enabled = false;

        nm.OnConnected += OnConnected;
        nm.OnDisconnected += OnDisconnected;
    }

    async void OnConnectClicked()
    {
        statusText.text = "Connecting...";
        statusText.enabled = true;
        connectButton.interactable = false;

        try
        {
            await nm.Connect(); // await the Task
        }
        catch (Exception ex)
        {
            Debug.LogError($"Connection failed: {ex.Message}");
            OnDisconnected();
        }
    }


    private void OnConnected()
    {
        nm.OnConnected -= OnConnected;
        SceneManager.LoadScene("Lobby");
    }

    private void OnDisconnected()
    {
        nm.OnDisconnected -= OnDisconnected;
        connectButton.interactable = true;
        statusText.text = "Connection failed.";
    }
}
