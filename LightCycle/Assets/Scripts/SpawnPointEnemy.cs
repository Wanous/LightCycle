using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnPointEnemy : MonoBehaviour
{
    public GameObject unitPrefab;

    // --- To Spawn Enemy ---
    public LayerMask groundLayer;
    private List<Vector3> spawnPoints;
    public int nbMaxofEnemiesthatcanspawnbeforedestroying = 20;
    
    public int nbOfEnemyToSpawnatonce = 1;

    void Start()
    {
        spawnPoints = new List<Vector3>();
        if (groundLayer.value == 0)
        {
            Debug.LogError("Ground Layer not set!", this);
            return;
        }

        i = 0;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit _, 1f,
                groundLayer)) spawnPoints.Add(transform.position);
        if (Physics.Raycast(GetPositionOnCircle(transform.position, transform.eulerAngles.y - 90, 2), Vector3.down,
                out RaycastHit _, 1f,
                groundLayer))
            spawnPoints.Add(GetPositionOnCircle(transform.position, transform.eulerAngles.y - 90, 2));
        if (Physics.Raycast(GetPositionOnCircle(transform.position, transform.eulerAngles.y + 90, 2), Vector3.down,
                out RaycastHit _, 1f,
                groundLayer))
            spawnPoints.Add(GetPositionOnCircle(transform.position, transform.eulerAngles.y + 90, 2));
        j = 0;
    }

    private int i;
    private int j;
    
    public bool spawn_ennemy()
    {
        for (; j < nbOfEnemyToSpawnatonce; j++)
        {
            Instantiate(unitPrefab, spawnPoints[i] + Vector3.up * (float)0.5, transform.rotation); 
            i++;
            nbMaxofEnemiesthatcanspawnbeforedestroying--;
            if (i >= spawnPoints.Count)
            {
                i = 0;
            }
            if (nbMaxofEnemiesthatcanspawnbeforedestroying == 0)
            {
                Destroy(gameObject);
                return true;
            }
        }
        j = 0;
        return false;
    }

    Vector3 GetPositionOnCircle(Vector3 origin, float angleDeg, float radius = 3)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float x = Mathf.Sin(angleRad) * radius;
        float z = Mathf.Cos(angleRad) * radius;
        return origin + new Vector3(x, 0, z);
    }

}
