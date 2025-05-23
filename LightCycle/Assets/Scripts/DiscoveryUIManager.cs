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
        hostButton.onClick.AddListener(HostGame);
        findButton.onClick.AddListener(FindServers);
        networkDiscovery.OnServerFound.AddListener(OnDiscoveredServer);
    }

    public void HostGame()
    {
        networkManager.StartHost();
        networkDiscovery.AdvertiseServer();
    }

    public void FindServers()
    {
        discoveredServers.Clear();
        foreach (Transform child in serverListParent)
            Destroy(child.gameObject);
        networkDiscovery.StartDiscovery();
    }

    void OnDiscoveredServer(ServerResponse info)
    {
        if (discoveredServers.ContainsKey(info.serverId))
            return;

        discoveredServers[info.serverId] = info;

        GameObject buttonObj = Instantiate(serverButtonPrefab, serverListParent);

        // Set button text to show IP address
        TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = info.EndPoint.Address.ToString();
            buttonText.color = Color.white;
        }

        // Add click listener to join server
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                networkDiscovery.StopDiscovery();
                networkManager.StartClient(info.uri);
            });
        }
    }
}