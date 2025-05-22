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
        }

        if (networkDiscovery != null)
        {
            networkDiscovery.AdvertiseServer();
        }
    }

    public void FindServers()
    {
        discoveredServers.Clear();
        if (serverListParent != null)
        {
            foreach (Transform child in serverListParent)
                Destroy(child.gameObject);
        }

        if (networkDiscovery != null)
        {
            networkDiscovery.StartDiscovery();
        }
    }

    public void OnDiscoveredServer(ServerResponse info)
    {
        if (discoveredServers.ContainsKey(info.serverId))
            return;

        discoveredServers[info.serverId] = info;

        if (serverButtonPrefab == null || serverListParent == null)
            return;

        GameObject buttonObj = Instantiate(serverButtonPrefab, serverListParent);

        TMP_Text buttonTMPText = buttonObj.GetComponentInChildren<TMP_Text>(true);
        if (buttonTMPText != null)
        {
            // Use info.uri.Host for a more reliable address display
            buttonTMPText.text = $"Join: {info.uri.Host}";
            buttonTMPText.color = Color.black;
            buttonTMPText.fontSize = 24;
            buttonTMPText.enableAutoSizing = true;
            buttonTMPText.overflowMode = TextOverflowModes.Overflow;
        }

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                if (networkDiscovery != null)
                {
                    networkDiscovery.StopDiscovery();
                }
                if (networkManager != null)
                {
                    networkManager.StartClient(info.uri);
                }
            });
        }
    }
}