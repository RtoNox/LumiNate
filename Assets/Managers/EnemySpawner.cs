
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyGroup
    {
        public GameObject enemyPrefab;
        public int baseCount;
        public float baseSpawnRate = 1f;
        public float quantityMultiplier = 1f;
    }

    [Header("Spawn Settings")]
    public float spawnDistanceFromCamera = 12f;
    public float minDistanceBetweenSpawns = 3f;
    public int maxActiveEnemies = 50;

    [Header("Enemy Settings")]
    public List<EnemyGroup> enemyTypes = new List<EnemyGroup>();

    [Header("Spawn Rate Settings")]
    public float initialSpawnInterval = 5f;
    public float spawnRateIncreaseInterval = 120f;
    public float initialSpawnRate = 0.2f;
    public float spawnRateIncrease = 1f;
    public float maxSpawnRate = 10f;
    public int initialEnemies = 5;

    [Header("Current Game Info")]
    public float gameTime = 0f;
    public int enemiesRemaining = 0;
    public bool isSpawning = false;
    public float currentSpawnRate = 0.2f;

    public Transform player;

    private Camera mainCamera;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Coroutine currentSpawnCoroutine;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        mainCamera = Camera.main;

        if (player == null)
            Debug.LogError("Player not found! Make sure player has 'Player' tag.");

        if (mainCamera == null)
            Debug.LogError("Main camera not found!");

        StartSpawning();
    }

    void Update()
    {
        // Remove destroyed enemies
        activeEnemies.RemoveAll(enemy => enemy == null);

        // Update current enemy count
        enemiesRemaining = activeEnemies.Count;

        // Update game time
        if (isSpawning)
        {
            gameTime += Time.deltaTime;
            UpdateSpawnRate();
        }
    }

    public void StartSpawning()
    {
        if (enemyTypes.Count == 0)
        {
            Debug.LogWarning("No enemy types configured in EnemySpawner!");
            return;
        }

        isSpawning = true;
        gameTime = 0f;
        currentSpawnRate = initialSpawnRate;

        if (currentSpawnCoroutine != null)
            StopCoroutine(currentSpawnCoroutine);

        currentSpawnCoroutine = StartCoroutine(SpawnContinuously());
    }

    IEnumerator SpawnContinuously()
    {
        // Spawn initial enemies immediately
        for (int i = 0; i < initialEnemies; i++)
        {
            if (!isSpawning)
                yield break;

            if (activeEnemies.Count >= maxActiveEnemies)
                break;

            SpawnRandomEnemy();
        }

        float spawnTimer = 0f;

        while (isSpawning)
        {
            spawnTimer += Time.deltaTime;

            float spawnInterval = 1f / currentSpawnRate;

            if (spawnTimer >= spawnInterval)
            {
                // Only spawn if below the maximum enemy limit
                if (activeEnemies.Count < maxActiveEnemies)
                {
                    SpawnRandomEnemy();
                }

                spawnTimer = 0f;
            }

            yield return null;
        }
    }

    void UpdateSpawnRate()
    {
        // First 2 minutes: 0.2 enemies per second
        // Every 2 minutes after that: increase by 1
        int increases = Mathf.FloorToInt(
            gameTime / spawnRateIncreaseInterval
        );

        currentSpawnRate = Mathf.Min(
            initialSpawnRate + (increases * spawnRateIncrease),
            maxSpawnRate
        );
    }

    void SpawnRandomEnemy()
    {
        if (player == null || mainCamera == null)
            return;

        if (enemyTypes.Count == 0)
            return;

        int randomIndex = Random.Range(0, enemyTypes.Count);

        GameObject prefab = enemyTypes[randomIndex].enemyPrefab;

        if (prefab == null)
            return;

        Vector2 spawnPosition = GetOffScreenSpawnPosition();

        if (spawnPosition != Vector2.zero)
        {
            GameObject enemy = Instantiate(
                prefab,
                spawnPosition,
                Quaternion.identity
            );

            activeEnemies.Add(enemy);
        }
    }

    Vector2 GetOffScreenSpawnPosition()
    {
        if (mainCamera == null || player == null)
            return Vector2.zero;

        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        Vector2 cameraCenter = mainCamera.transform.position;
        Vector2 playerPosition = player.position;

        List<Vector2> spawnEdges = new List<Vector2>
        {
            new Vector2(
                cameraCenter.x - cameraWidth / 2 - spawnDistanceFromCamera,
                playerPosition.y + Random.Range(-cameraHeight / 2, cameraHeight / 2)
            ),

            new Vector2(
                cameraCenter.x + cameraWidth / 2 + spawnDistanceFromCamera,
                playerPosition.y + Random.Range(-cameraHeight / 2, cameraHeight / 2)
            ),

            new Vector2(
                playerPosition.x + Random.Range(-cameraWidth / 2, cameraWidth / 2),
                cameraCenter.y + cameraHeight / 2 + spawnDistanceFromCamera
            ),

            new Vector2(
                playerPosition.x + Random.Range(-cameraWidth / 2, cameraWidth / 2),
                cameraCenter.y - cameraHeight / 2 - spawnDistanceFromCamera
            )
        };

        foreach (Vector2 spawnPos in spawnEdges)
        {
            if (IsSpawnPositionValid(spawnPos))
            {
                return spawnPos;
            }
        }

        return Vector2.zero;
    }

    bool IsSpawnPositionValid(Vector2 spawnPos)
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null &&
                Vector2.Distance(
                    spawnPos,
                    enemy.transform.position
                ) < minDistanceBetweenSpawns)
            {
                return false;
            }
        }

        return true;
    }

    public void StopSpawning()
    {
        isSpawning = false;

        if (currentSpawnCoroutine != null)
        {
            StopCoroutine(currentSpawnCoroutine);
            currentSpawnCoroutine = null;
        }
    }

    public void ResetSpawning()
    {
        StopSpawning();

        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        activeEnemies.Clear();

        gameTime = 0f;
        enemiesRemaining = 0;
        currentSpawnRate = initialSpawnRate;
    }

    public float GetCurrentSpawnRate()
    {
        return currentSpawnRate;
    }

    public int GetCurrentEnemyCount()
    {
        return activeEnemies.Count;
    }

    void OnDrawGizmosSelected()
    {
        if (mainCamera != null)
        {
            float cameraHeight = 2f * mainCamera.orthographicSize;
            float cameraWidth = cameraHeight * mainCamera.aspect;

            Vector2 cameraCenter = mainCamera.transform.position;

            Gizmos.color = Color.yellow;

            Gizmos.DrawWireCube(
                cameraCenter,
                new Vector3(cameraWidth, cameraHeight, 0)
            );

            Gizmos.color = Color.red;

            Gizmos.DrawWireCube(
                cameraCenter,
                new Vector3(
                    cameraWidth + spawnDistanceFromCamera * 2,
                    cameraHeight + spawnDistanceFromCamera * 2,
                    0
                )
            );
        }
    }
}
