using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public GameObject playerPrefab;

    void Start()
    {
        if (playerPrefab != null)
        {
            // Ajoute 90° sur l'axe Y
            Quaternion rotation = transform.rotation * Quaternion.Euler(0, -90, 0);
            Instantiate(playerPrefab, transform.position, rotation);
        }
        else
        {
            Debug.LogError("Player Prefab is not assigned in the SpawnPoint script.");
        }
    }
}

