using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public GameObject playerPrefab;

    void Start()
    {
        if (playerPrefab != null)
        {
            // Combine la rotation du spawn point avec +90° sur l'axe Y
            Quaternion spawnRotation = transform.rotation * Quaternion.Euler(0, 90, 0);
            Instantiate(playerPrefab, transform.position, spawnRotation);
        }
        else
        {
            Debug.LogError("Player Prefab is not assigned in the SpawnPoint script.");
        }
    }
}


