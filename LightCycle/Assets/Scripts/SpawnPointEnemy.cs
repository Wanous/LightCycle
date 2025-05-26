using System.Collections.Generic;
using UnityEngine;

public class SpawnPointEnemy : MonoBehaviour
{
    
    // --- To Spawn Enemy ---
    public GameObject unitPrefab;
    public LayerMask groundLayer;
    private List<Vector3> spawnPoints;
    
    // --- To know when we can spawn a unit ---
    public int unitalive;
    public int maxunitalive = 1;
    public bool isnotblock = true;
    public bool cantheunitjump = true;
    public Difficulty difficulty;
    
    public int nbMaxofEnemiesthatcanspawnbeforedestroying = 5;

    public bool CanSpawn => unitalive < maxunitalive;
    
    
    void Start()
    {
        unitalive = 0;
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
            spawnPoints.Add(GetPositionOnCircle(transform.position, transform.eulerAngles.y - 90, 2)); // Left
        if (Physics.Raycast(GetPositionOnCircle(transform.position, transform.eulerAngles.y + 90, 2), Vector3.down,
                out RaycastHit _, 1f,
                groundLayer))
            spawnPoints.Add(GetPositionOnCircle(transform.position, transform.eulerAngles.y + 90, 2)); // Right
        
    }

    private int i;

    // ReSharper disable Unity.PerformanceAnalysis
    public int spawn_ennemy()
    {
        if (nbMaxofEnemiesthatcanspawnbeforedestroying == -1)
        {
            Destroy(gameObject);
            return -1;
        }
        if (isnotblock)
        {
            unitalive++;
            EnemyAI ai = Instantiate(unitPrefab, spawnPoints[i] + Vector3.up * (float)0.5, transform.rotation).gameObject.GetComponent<EnemyAI>();
            ai.Spawnpointset(this);
            ai.canJump = cantheunitjump;
            switch (difficulty)
            {
                case Difficulty.Easy: ai.Difficulty = 0.25f;
                    break;
                case Difficulty.Medium: ai.Difficulty = 0.5f;
                    break;
                case Difficulty.Hard: ai.Difficulty = 0.75f;
                    break;
                default: ai.Difficulty = 1f;
                    break;
            }
            i++;
            nbMaxofEnemiesthatcanspawnbeforedestroying--;
            if (i >= spawnPoints.Count)
            {
                i = 0;
                isnotblock = false;
            }
        }
        else
        {
            if (unitalive == maxunitalive) return 0;
            Invoke(nameof(Canspawn),3f);
        }
        return 1;
    }

    Vector3 GetPositionOnCircle(Vector3 origin, float angleDeg, float radius = 3)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float x = Mathf.Sin(angleRad) * radius;
        float z = Mathf.Cos(angleRad) * radius;
        return origin + new Vector3(x, 0, z);
    }

    void Canspawn()
    {
        isnotblock = true;
    }
    
}
