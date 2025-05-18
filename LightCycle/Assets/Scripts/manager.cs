using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public List<SpawnPointEnemy> enemySpawnPoint { get; private set; }

    private int i;
    
    void Start()
    {
        i = 0;
        GameObject[] spawnObjects = GameObject.FindGameObjectsWithTag("ESP");
        enemySpawnPoint = new List<SpawnPointEnemy>();

        for (int i = 0; i < spawnObjects.Length; i++)
        {
            enemySpawnPoint.Add(spawnObjects[i].GetComponent<SpawnPointEnemy>());

            if (enemySpawnPoint[i] == null)
            {
                Debug.LogWarning("SpawnPointEnemy component not found on object: " + spawnObjects[i].name);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (0 != enemySpawnPoint.Count)
            {
                if (i >= enemySpawnPoint.Count)
                {
                    i = 0;
                }

                bool? b = enemySpawnPoint[i].spawn_ennemy();
                if(b == true)enemySpawnPoint.RemoveAt(i);
            }
        }
    }
    
    void Useless()
    {
        
    }
}
