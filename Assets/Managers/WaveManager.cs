using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class WaveConfig
    {
        public int waveNumber;
        public List<EnemySpawn> enemySpawns = new List<EnemySpawn>();
    }
    
    [System.Serializable]
    public class EnemySpawn
    {
        public GameObject enemyPrefab;
        public int count;
        public float spawnDelay;
    }
    
    [SerializeField] private List<WaveConfig> waveConfigs = new List<WaveConfig>();
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private float timeBetweenWaves = 10f;
    
    private List<BaseEnemy> currentWaveEnemies = new List<BaseEnemy>();
    private bool isSpawning = false;
    
    void Start()
    {
        StartCoroutine(StartFirstWave());
    }
    
    IEnumerator StartFirstWave()
    {
        yield return new WaitForSeconds(3f);
        StartWave(1);
    }
    
    public void StartWave(int waveNumber)
    {
        if (!isSpawning && GameManager.Instance.IsGameActive)
        {
            StartCoroutine(SpawnWave(waveNumber));
        }
    }
    
    IEnumerator SpawnWave(int waveNumber)
    {
        isSpawning = true;
        
        WaveConfig config = GetWaveConfig(waveNumber);
        if (config == null)
        {
            config = CreateDefaultWave(waveNumber);
        }
        
        Debug.Log($"Spawning wave {waveNumber} with {config.enemySpawns.Count} enemy types");
        
        foreach (EnemySpawn spawn in config.enemySpawns)
        {
            for (int i = 0; i < spawn.count; i++)
            {
                SpawnEnemy(spawn.enemyPrefab);
                yield return new WaitForSeconds(spawn.spawnDelay);
            }
        }
        
        isSpawning = false;
        
        StartCoroutine(CheckWaveCompletion(waveNumber));
    }
    
    IEnumerator CheckWaveCompletion(int waveNumber)
    {
        yield return new WaitForSeconds(2f);
        
        while (currentWaveEnemies.Count > 0 && GameManager.Instance.IsGameActive)
        {
            yield return new WaitForSeconds(1f);
        }
        
        if (!GameManager.Instance.IsGameActive) yield break;
        
        Debug.Log($"Wave {waveNumber} completed!");
        
        yield return new WaitForSeconds(timeBetweenWaves);
        
        if (GameManager.Instance.IsGameActive)
        {
            int nextWave = waveNumber + 1;
            GameManager.Instance.IncrementWave();
            StartWave(nextWave);
        }
    }
    
    void SpawnEnemy(GameObject enemyPrefab)
    {
        if (!GameManager.Instance.IsGameActive) return;
        
        if (spawnPoints.Length == 0)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            RegisterEnemy(enemy.GetComponent<BaseEnemy>());
        }
        else
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            RegisterEnemy(enemy.GetComponent<BaseEnemy>());
        }
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        Vector3 playerPos = GameManager.Instance.PlayerPosition;
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        return playerPos + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
    
    void RegisterEnemy(BaseEnemy enemy)
    {
        if (enemy != null && GameManager.Instance != null)
        {
            // FIXED: Now using GameManager.Instance.CurrentWave which exists
            enemy.InitializeWave(GameManager.Instance.CurrentWave);
            enemy.OnWaveStart();
            enemy.OnDeath += () => RemoveEnemy(enemy);
            currentWaveEnemies.Add(enemy);
        }
    }
    
    void RemoveEnemy(BaseEnemy enemy)
    {
        currentWaveEnemies.Remove(enemy);
    }
    
    WaveConfig GetWaveConfig(int waveNumber)
    {
        return waveConfigs.Find(w => w.waveNumber == waveNumber);
    }
    
    WaveConfig CreateDefaultWave(int waveNumber)
    {
        WaveConfig config = new WaveConfig { waveNumber = waveNumber };
        
        if (waveNumber >= 1)
        {
            config.enemySpawns.Add(new EnemySpawn {
                enemyPrefab = GetEnemyPrefab("Crawler"),
                count = Mathf.Min(3 + waveNumber, 10),
                spawnDelay = 5f
            });
        }
        
        if (waveNumber >= 2)
        {
            config.enemySpawns.Add(new EnemySpawn {
                enemyPrefab = GetEnemyPrefab("Dasher"),
                count = Mathf.Min(2 + waveNumber, 8),
                spawnDelay = 8f
            });
        }
        
        if (waveNumber >= 3)
        {
            config.enemySpawns.Add(new EnemySpawn {
                enemyPrefab = GetEnemyPrefab("Mimic"),
                count = Mathf.Min(1 + waveNumber, 5),
                spawnDelay = 15f
            });
        }
        
        if (waveNumber >= 4)
        {
            config.enemySpawns.Add(new EnemySpawn {
                enemyPrefab = GetEnemyPrefab("Drainer"),
                count = Mathf.Min(waveNumber, 4),
                spawnDelay = 20f
            });
        }
        
        return config;
    }
    
    GameObject GetEnemyPrefab(string enemyType)
    {
        GameObject obj = new GameObject(enemyType);
        
        switch (enemyType)
        {
            case "Crawler":
                obj.AddComponent<Crawler>();
                break;
            case "Dasher":
                obj.AddComponent<Dasher>();
                break;
            case "Mimic":
                obj.AddComponent<Mimic>();
                break;
            case "Drainer":
                obj.AddComponent<Drainer>();
                break;
        }
        
        obj.AddComponent<SphereCollider>();
        Rigidbody rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        
        return obj;
    }
}