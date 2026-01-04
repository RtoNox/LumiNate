using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;
    
    [Header("Game Settings")]
    [SerializeField] private int itemsToWin = 30;
    [SerializeField] private float itemSpawnInterval = 30f;
    [SerializeField] private float enemySpawnInterval = 5f;
    
    [Header("Game State")]
    [SerializeField] private bool isGameActive = true;
    public bool IsGameActive => isGameActive;
    
    [Header("Wave System")]
    private int currentWave = 1;
    public int CurrentWave => currentWave; // Fixed: Added this property
    
    [Header("UI References")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject gameOverScreen;
    
    private int collectedItems = 0;
    
    private PlayerController player;
    private List<BaseEnemy> activeEnemies = new List<BaseEnemy>();
    private List<CollectibleItem> spawnedItems = new List<CollectibleItem>();
    
    private float enemySpawnTimer = 0f;
    private float itemSpawnTimer = 0f;
    
    private Dictionary<EnemyType, int> enemiesPerType = new Dictionary<EnemyType, int>();
    
    // Events
    public event System.Action<int> OnItemCollected;
    public event System.Action<int> OnEnemyCountChanged;
    public event System.Action<bool> OnGameEnd;
    
    public Vector3 PlayerPosition => player != null ? player.transform.position : Vector3.zero;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        InitializeEnemyCounts();
    }
    
    void Start()
    {
        // Start spawning immediately
        enemySpawnTimer = enemySpawnInterval;
        itemSpawnTimer = itemSpawnInterval;
        
        UIManager.Instance.UpdateItems(collectedItems, itemsToWin);
        UIManager.Instance.UpdateEnemyCount(activeEnemies.Count);
        
        Debug.Log($"GameManager initialized. Current Wave: {CurrentWave}");
    }
    
    void Update()
    {
        if (!isGameActive) return;
        
        // Handle enemy spawning
        enemySpawnTimer -= Time.deltaTime;
        if (enemySpawnTimer <= 0)
        {
            SpawnEnemyWave();
            enemySpawnTimer = enemySpawnInterval;
        }
        
        // Handle item spawning
        itemSpawnTimer -= Time.deltaTime;
        if (itemSpawnTimer <= 0)
        {
            SpawnItem();
            itemSpawnTimer = itemSpawnInterval;
        }
    }
    
    void InitializeEnemyCounts()
    {
        // Initialize with 5 enemies per type
        enemiesPerType[EnemyType.Crawler] = 5;
        enemiesPerType[EnemyType.Dasher] = 5;
        enemiesPerType[EnemyType.Mimic] = 5;
        enemiesPerType[EnemyType.Drainer] = 5;
    }
    
    void SpawnEnemyWave()
    {
        Debug.Log($"Spawning enemy wave {CurrentWave}...");
        
        // Spawn each enemy type
        foreach (var kvp in enemiesPerType)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                SpawnEnemy(kvp.Key);
            }
        }
        
        // Every 5 minutes, add new enemy type
        StartCoroutine(ProgressionRoutine());
    }
    
    void SpawnEnemy(EnemyType enemyType)
    {
        Vector3 spawnPosition = GetRandomOffscreenPosition();
        GameObject enemyPrefab = GetEnemyPrefab(enemyType);
        
        if (enemyPrefab != null)
        {
            GameObject enemyObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            BaseEnemy enemy = enemyObj.GetComponent<BaseEnemy>();
            
            if (enemy != null)
            {
                RegisterEnemy(enemy);
            }
        }
    }
    
    void SpawnItem()
    {
        Debug.Log("Spawning item...");
        
        Vector3 spawnPosition = GetRandomOffscreenPosition();
        GameObject itemPrefab = GetItemPrefab();
        
        if (itemPrefab != null)
        {
            GameObject itemObj = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
            CollectibleItem item = itemObj.GetComponent<CollectibleItem>();
            
            if (item != null)
            {
                spawnedItems.Add(item);
                item.OnCollected += () => RemoveItem(item);
            }
        }
    }
    
    Vector3 GetRandomOffscreenPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return Vector3.zero;
        
        float screenBuffer = 2f;
        Vector3 viewportPos;
        
        int side = Random.Range(0, 4);
        
        switch (side)
        {
            case 0: // Left
                viewportPos = new Vector3(-screenBuffer, Random.Range(0f, 1f), 10f);
                break;
            case 1: // Right
                viewportPos = new Vector3(1 + screenBuffer, Random.Range(0f, 1f), 10f);
                break;
            case 2: // Top
                viewportPos = new Vector3(Random.Range(0f, 1f), 1 + screenBuffer, 10f);
                break;
            case 3: // Bottom
                viewportPos = new Vector3(Random.Range(0f, 1f), -screenBuffer, 10f);
                break;
            default:
                viewportPos = new Vector3(0.5f, 0.5f, 10f);
                break;
        }
        
        return mainCamera.ViewportToWorldPoint(viewportPos);
    }
    
    IEnumerator ProgressionRoutine()
    {
        float progressionInterval = 300f;
        
        while (isGameActive)
        {
            yield return new WaitForSeconds(progressionInterval);
            AddNewEnemyType();
        }
    }
    
    void AddNewEnemyType()
    {
        // Implementation depends on your progression system
    }
    
    public void CollectItem()
    {
        collectedItems++;
        OnItemCollected?.Invoke(collectedItems);
        UIManager.Instance.UpdateItems(collectedItems, itemsToWin);
        
        if (collectedItems >= itemsToWin)
        {
            GameOver(true);
        }
    }
    
    public void RegisterPlayer(PlayerController playerController)
    {
        player = playerController;
        player.OnDeath += () => GameOver(false);
    }
    
    public void RegisterEnemy(BaseEnemy enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
            enemy.OnDeath += () => RemoveEnemy(enemy);
            OnEnemyCountChanged?.Invoke(activeEnemies.Count);
            UIManager.Instance.UpdateEnemyCount(activeEnemies.Count);
        }
    }
    
    public void RemoveEnemy(BaseEnemy enemy)
    {
        activeEnemies.Remove(enemy);
        OnEnemyCountChanged?.Invoke(activeEnemies.Count);
        UIManager.Instance.UpdateEnemyCount(activeEnemies.Count);
    }
    
    void RemoveItem(CollectibleItem item)
    {
        spawnedItems.Remove(item);
    }
    
    public void GameOver(bool won)
    {
        if (!isGameActive) return;
        
        isGameActive = false;
        Time.timeScale = 0f;
        
        if (won)
        {
            Debug.Log($"You win! Collected {collectedItems} items!");
            if (winScreen != null)
                winScreen.SetActive(true);
        }
        else
        {
            Debug.Log("Game Over!");
            if (gameOverScreen != null)
                gameOverScreen.SetActive(true);
        }
        
        OnGameEnd?.Invoke(won);
    }
    
    GameObject GetEnemyPrefab(EnemyType enemyType)
    {
        string path = $"Enemies/{enemyType}Enemy";
        GameObject prefab = Resources.Load<GameObject>(path);
        
        if (prefab == null)
        {
            prefab = CreatePlaceholderEnemy(enemyType);
        }
        
        return prefab;
    }
    
    GameObject GetItemPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>("Items/CollectibleItem");
        
        if (prefab == null)
        {
            prefab = new GameObject("CollectibleItem");
            prefab.AddComponent<CollectibleItem>();
            prefab.AddComponent<SphereCollider>().isTrigger = true;
            prefab.AddComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            prefab.AddComponent<MeshRenderer>().material.color = Color.yellow;
            prefab.tag = "Collectible";
        }
        
        return prefab;
    }
    
    GameObject CreatePlaceholderEnemy(EnemyType enemyType)
    {
        GameObject obj = new GameObject($"{enemyType}Enemy");
        
        switch (enemyType)
        {
            case EnemyType.Crawler:
                obj.AddComponent<Crawler>();
                break;
            case EnemyType.Dasher:
                obj.AddComponent<Dasher>();
                break;
            case EnemyType.Mimic:
                obj.AddComponent<Mimic>();
                obj.tag = "Collectible";
                break;
            case EnemyType.Drainer:
                obj.AddComponent<Drainer>();
                break;
        }
        
        obj.AddComponent<SphereCollider>().radius = 0.5f;
        Rigidbody rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        
        MeshFilter mf = obj.AddComponent<MeshFilter>();
        mf.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Standard"));
        
        switch (enemyType)
        {
            case EnemyType.Crawler:
                mr.material.color = Color.gray;
                break;
            case EnemyType.Dasher:
                mr.material.color = Color.red;
                break;
            case EnemyType.Mimic:
                mr.material.color = Color.magenta;
                break;
            case EnemyType.Drainer:
                mr.material.color = Color.cyan;
                break;
        }
        
        return obj;
    }
    
    // Wave management methods
    public void IncrementWave()
    {
        currentWave++;
        Debug.Log($"Wave incremented to: {currentWave}");
    }
    
    public void SetCurrentWave(int wave)
    {
        currentWave = wave;
        Debug.Log($"Wave set to: {currentWave}");
    }
}