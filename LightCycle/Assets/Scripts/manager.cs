using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Manager : MonoBehaviour
{
    public List<SpawnPointEnemy> EnemySpawnPoint { get; private set; }

    public bool canTheUnitJump = true;
    
    public bool IsActive => EnemySpawnPoint.Count != 0;
    
    void Start()
    {
        GameObject[] spawnObjects = GameObject.FindGameObjectsWithTag("ESP");
        EnemySpawnPoint = new List<SpawnPointEnemy>();

        for (int i = 0; i < spawnObjects.Length; i++)
        {
            SpawnPointEnemy spe = spawnObjects[i].GetComponent<SpawnPointEnemy>();
            spe.cantheunitjump = canTheUnitJump;
            EnemySpawnPoint.Add(spe);
            if (EnemySpawnPoint[i] == null)
            {
                Debug.LogWarning("SpawnPointEnemy component not found on object: " + spawnObjects[i].name);
            }
        }
    }

    void Update()
    {
        for (int i = 0; i < EnemySpawnPoint.Count; i++)
        {
            if (EnemySpawnPoint[i].CanSpawn)
            {
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                int b = SpawnoneUnitAt(i);
                if (b == -1)
                {
                    EnemySpawnPoint.RemoveAt(i);
                    i--;
                }
                else if (b == -3) Destroy(gameObject);
            }
        } 
    }

    int SpawnoneUnitAt(int i)
    {
        if (i < 0) i += EnemySpawnPoint.Count;
        if (i < 0 || i >= EnemySpawnPoint.Count)
        {
            Debug.LogError("i is out of range", this);
            return -2;
        }
        if (0 != EnemySpawnPoint.Count)
        {
            if (i >= EnemySpawnPoint.Count)
            {
                i = 0;
            }
            return EnemySpawnPoint[i].spawn_ennemy();
            
        }
        else
        {
            Debug.LogWarning("None SpawnPoint detected", this);
            return -3;
        }
    }
}
