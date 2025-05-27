using System.Collections.Generic;
using UnityEngine;

public class SpawnPointEnemy : MonoBehaviour
{
    
    // --- To Spawn Enemy ---
    public GameObject unitPrefab;
    public LayerMask groundLayer;
    private List<Vector3> _spawnPoints;
    private EnemyAI enemyAI;
    
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
        _spawnPoints = new List<Vector3>();
        if (groundLayer.value == 0)
        {
            Debug.LogError("Ground Layer not set!", this);
            return;
        }

        _i = 0;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit _, 6f,
                groundLayer)) _spawnPoints.Add(transform.position);
        if (Physics.Raycast(GetPositionOnCircle(transform.position, transform.eulerAngles.y - 90, 2), Vector3.down,
                out RaycastHit _, 6f,
                groundLayer))
            _spawnPoints.Add(GetPositionOnCircle(transform.position, transform.eulerAngles.y - 90, 2)); // Left
        if (Physics.Raycast(GetPositionOnCircle(transform.position, transform.eulerAngles.y + 90, 2), Vector3.down,
                out RaycastHit _, 6f,
                groundLayer))
            _spawnPoints.Add(GetPositionOnCircle(transform.position, transform.eulerAngles.y + 90, 2)); // Right
        
    }

    private int _i;

    // ReSharper disable Unity.PerformanceAnalysis
    public int spawn_ennemy()
    {
        if (nbMaxofEnemiesthatcanspawnbeforedestroying == -1)
        {
            Destroy(gameObject);
            return -1;
        }
        if (isnotblock && _spawnPoints.Count > 0)
        {
            unitalive++;
            enemyAI = Instantiate(unitPrefab, _spawnPoints[_i] + Vector3.up * (float)0.5, transform.rotation).gameObject.GetComponent<EnemyAI>();
            enemyAI.Spawnpointset(this);
            enemyAI.canJump = cantheunitjump;
            ChangeDifficulty(difficulty);
            _i++;
            nbMaxofEnemiesthatcanspawnbeforedestroying--;
            if (_i >= _spawnPoints.Count)
            {
                _i = 0;
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

    public void ChangeDifficulty(Difficulty difficult)
    {
        difficulty = difficult;
        switch (difficult)
        {
            case Difficulty.Easy: enemyAI.Difficulty = 0.25f;
                break;
            case Difficulty.Medium: enemyAI.Difficulty = 0.5f;
                break;
            case Difficulty.Hard: enemyAI.Difficulty = 0.75f;
                break;
        }
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
