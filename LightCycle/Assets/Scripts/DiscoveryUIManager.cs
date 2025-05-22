using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Mirror.Discovery;

public class DiscoveryUIManager : MonoBehaviour
{
    public NetworkManager networkManager;
    public NetworkDiscovery networkDiscovery;

    [Header("UI References")]
    public Button hostButton;
    public Button findButton;
    public Transform serverListParent;
    public GameObject serverButtonPrefab;

    private Dictionary<long, ServerResponse> discoveredServers = new();

    void Start()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(HostGame);
        if (findButton != null)
            findButton.onClick.AddListener(FindServers);
        if (networkDiscovery != null)
            networkDiscovery.OnServerFound.AddListener(OnDiscoveredServer);
    }

    public void HostGame()
    {
        if (networkManager != null)
        {
            networkManager.StartHost();
            Debug.Log("Started hosting game");
        }
        if (networkDiscovery != null)
        {
            networkDiscovery.AdvertiseServer();
            Debug.Log("Started advertising server");
        }
    }

    public void FindServers()
    {
        discoveredServers.Clear();
        ClearServerList();

        if (networkDiscovery != null)
        {
            networkDiscovery.StartDiscovery();
            Debug.Log("Started server discovery");
        }
    }

    private void ClearServerList()
    {
        if (serverListParent != null)
        {
            foreach (Transform child in serverListParent)
                Destroy(child.gameObject);
        }
    }

    public void OnDiscoveredServer(ServerResponse info)
    {
        Debug.Log($"Server discovered: {info.serverId}, URI: {info.uri}");

        if (discoveredServers.ContainsKey(info.serverId))
        {
            Debug.Log($"Server {info.serverId} already in list, skipping");
            return;
        }

        discoveredServers[info.serverId] = info;
        CreateServerButton(info);
    }

    private void CreateServerButton(ServerResponse info)
    {
        if (serverButtonPrefab == null || serverListParent == null)
        {
            Debug.LogError("Server button prefab or parent is null!");
            return;
        }

        GameObject buttonObj = Instantiate(serverButtonPrefab, serverListParent);

        // Try multiple ways to find the text component
        TMP_Text buttonTMPText = buttonObj.GetComponentInChildren<TMP_Text>(true);
        if (buttonTMPText == null)
        {
            // Try getting Text component instead
            Text buttonText = buttonObj.GetComponentInChildren<Text>(true);
            if (buttonText != null)
            {
                buttonText.text = GetServerDisplayText(info);
                buttonText.color = Color.white;
                buttonText.fontSize = 24;
            }
        }
        else
        {
            buttonTMPText.text = GetServerDisplayText(info);
            buttonTMPText.color = Color.white;
            buttonTMPText.fontSize = 24;
            buttonTMPText.enableAutoSizing = true;
            buttonTMPText.overflowMode = TextOverflowModes.Overflow;
        }

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            // Capture the server info in a local variable to avoid closure issues
            ServerResponse serverInfo = info;
            button.onClick.AddListener(() => JoinServer(serverInfo));
        }
        else
        {
            Debug.LogError("Button component not found on server button prefab!");
        }
    }

    private string GetServerDisplayText(ServerResponse info)
    {
        // Try different ways to get the IP address
        string displayText = "Unknown Server";

        if (info.uri != null)
        {
            // Option 1: Use Host property
            if (!string.IsNullOrEmpty(info.uri.Host))
            {
                displayText = $"Join: {info.uri.Host}:{info.uri.Port}";
            }
            // Option 2: Use the full URI
            else
            {
                displayText = $"Join: {info.uri}";
            }
        }
        // Option 3: Use EndPoint if available
        else if (info.EndPoint != null)
        {
            displayText = $"Join: {info.EndPoint}";
        }

        Debug.Log($"Server display text: {displayText}");
        return displayText;
    }

    private void JoinServer(ServerResponse info)
    {
        Debug.Log($"Attempting to join server: {info.uri}");

        if (networkDiscovery != null)
        {
            networkDiscovery.StopDiscovery();
        }

        if (networkManager != null)
        {
            // Make sure we're not already connected
            if (networkManager.isNetworkActive)
            {
                networkManager.StopClient();
                networkManager.StopHost();
            }

            // Start client with the server URI
            networkManager.StartClient(info.uri);
        }
        else
        {
            Debug.LogError("NetworkManager is null!");
        }
    }

    // Optional: Add a method to stop discovery
    public void StopDiscovery()
    {
        if (networkDiscovery != null)
        {
            networkDiscovery.StopDiscovery();
        }
    }

    // Optional: Clean up on destroy
    void OnDestroy()
    {
        if (networkDiscovery != null)
        {
            networkDiscovery.OnServerFound.RemoveListener(OnDiscoveredServer);
        }
    }
}