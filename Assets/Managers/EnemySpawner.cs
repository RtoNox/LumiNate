using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public enum SpawnMode { Offscreen, RandomPoints, WaveBased }
    
    [Header("Spawn Settings")]
    [SerializeField] private SpawnMode spawnMode = SpawnMode.Offscreen;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int enemiesPerSpawn = 5;
    [SerializeField] private float spawnDistance = 15f;
    
    [Header("Enemy Types")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private Transform[] spawnPoints;
    
    [Header("Progression")]
    [SerializeField] private float progressionInterval = 300f; // 5 minutes
    [SerializeField] private List<GameObject> progressiveEnemyPrefabs = new List<GameObject>();
    
    private float spawnTimer = 0f;
    private float progressionTimer = 0f;
    private int currentProgressionIndex = 0;
    private List<GameObject> availableEnemies = new List<GameObject>();
    
    void Start()
    {
        spawnTimer = spawnInterval;
        progressionTimer = progressionInterval;
        
        // Start with basic enemies
        availableEnemies.AddRange(enemyPrefabs);
    }
    
    void Update()
    {
        if (!GameManager.Instance.IsGameActive) return;
        
        // Handle enemy spawning
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnEnemies();
            spawnTimer = spawnInterval;
        }
        
        // Handle progression
        progressionTimer -= Time.deltaTime;
        if (progressionTimer <= 0 && currentProgressionIndex < progressiveEnemyPrefabs.Count)
        {
            AddNewEnemyType();
            progressionTimer = progressionInterval;
        }
    }
    
    void SpawnEnemies()
    {
        for (int i = 0; i < enemiesPerSpawn; i++)
        {
            // Spawn one of each available type
            foreach (var enemyPrefab in availableEnemies)
            {
                SpawnEnemy(enemyPrefab);
            }
        }
    }
    
    void SpawnEnemy(GameObject enemyPrefab)
    {
        Vector3 spawnPosition = GetSpawnPosition();
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        
        // Initialize enemy
        BaseEnemy enemyScript = enemy.GetComponent<BaseEnemy>();
        if (enemyScript != null)
        {
            GameManager.Instance.RegisterEnemy(enemyScript);
        }
    }
    
    Vector3 GetSpawnPosition()
    {
        switch (spawnMode)
        {
            case SpawnMode.Offscreen:
                return GetOffscreenPosition();
                
            case SpawnMode.RandomPoints:
                if (spawnPoints.Length > 0)
                {
                    Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    return point.position;
                }
                break;
                
            case SpawnMode.WaveBased:
                // Circular spawn around player
                Vector3 playerPos = GameManager.Instance.PlayerPosition;
                Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnDistance;
                return playerPos + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        
        return GetOffscreenPosition();
    }
    
    Vector3 GetOffscreenPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return Vector3.zero;
        
        // Random point outside camera view
        Vector3 viewportPoint = Vector3.zero;
        
        // Choose random side (0: left, 1: right, 2: top, 3: bottom)
        int side = Random.Range(0, 4);
        float margin = 0.1f; // Spawn just outside view
        
        switch (side)
        {
            case 0: // Left
                viewportPoint = new Vector3(-margin, Random.Range(0f, 1f), 10f);
                break;
            case 1: // Right
                viewportPoint = new Vector3(1 + margin, Random.Range(0f, 1f), 10f);
                break;
            case 2: // Top
                viewportPoint = new Vector3(Random.Range(0f, 1f), 1 + margin, 10f);
                break;
            case 3: // Bottom
                viewportPoint = new Vector3(Random.Range(0f, 1f), -margin, 10f);
                break;
        }
        
        return mainCamera.ViewportToWorldPoint(viewportPoint);
    }
    
    void AddNewEnemyType()
    {
        if (currentProgressionIndex < progressiveEnemyPrefabs.Count)
        {
            GameObject newEnemy = progressiveEnemyPrefabs[currentProgressionIndex];
            availableEnemies.Add(newEnemy);
            currentProgressionIndex++;
            
            Debug.Log($"New enemy type unlocked: {newEnemy.name}");
            
            // Show UI notification
            UIManager.Instance.ShowNotification($"New Enemy: {newEnemy.name}");
        }
    }
}