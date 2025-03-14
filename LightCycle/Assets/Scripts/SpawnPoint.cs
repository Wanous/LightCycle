using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public GameObject playerPrefab;

    void Start()
    {
        if (playerPrefab != null)
        {
            Instantiate(playerPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogError("Player Prefab is not assigned in the SpawnPoint script.");
        }
    }
}
