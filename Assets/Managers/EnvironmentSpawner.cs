using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnvironmentSpawner : MonoBehaviour
{
    [Header("Spawn Area")]
    public Vector2 spawnAreaSize = new Vector2(15f, 15f);
    
    [Header("Spawn Settings")]
    public float spawnInterval = 2f;
    public int maxSpawnedItems = 10;
    
    [Header("Loot Prefabs")]
    public List<GameObject> commonLootPrefabs;
    
    [Header("Debug")]
    public bool showDebugLogs = true;

    public Transform player;
    private List<GameObject> spawnedItems = new List<GameObject>();

    void Start()
    {
        Debug.Log("=== ENVIRONMENT SPAWNER START ===");
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
        yield return new WaitForSeconds(1f);
        
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("❌ PLAYER NOT FOUND! Make sure:");
            Debug.LogError("1. Player GameObject exists");
            Debug.LogError("2. Player has 'Player' tag");
            yield break;
        }
        Debug.Log("✅ Player found: " + player.name);

        if (commonLootPrefabs.Count == 0)
        {
            Debug.LogError("❌ NO PREFABS ASSIGNED!");
            Debug.LogError("Please drag loot prefabs into Common Loot Prefabs list");
            yield break;
        }

        Debug.Log("✅ Prefabs found: " + commonLootPrefabs.Count);

        StartCoroutine(SpawnRoutine());
        Debug.Log("🚀 Spawning started!");
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            TrySpawnItem();
        }
    }

    void TrySpawnItem()
    {
        if (player == null) return;
        
        spawnedItems.RemoveAll(item => item == null);

        if (spawnedItems.Count >= maxSpawnedItems)
        {
            if (showDebugLogs) Debug.Log("Max items reached: " + spawnedItems.Count);
            return;
        }

        Vector2 spawnPos = GetSpawnPosition();
        if (spawnPos != Vector2.zero)
        {
            SpawnItemAtPosition(spawnPos);
        }
    }

    Vector2 GetSpawnPosition()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float distance = Random.Range(3f, 8f);
        Vector2 spawnPos = (Vector2)player.position + randomDir * distance;

        foreach (var item in spawnedItems)
        {
            if (item != null && Vector2.Distance(spawnPos, item.transform.position) < 2f)
            {
                return Vector2.zero;
            }
        }

        return spawnPos;
    }

    void SpawnItemAtPosition(Vector2 position)
    {
        if (commonLootPrefabs.Count == 0) return;

        GameObject prefab = commonLootPrefabs[0];
        if (prefab == null)
        {
            Debug.LogError("Prefab is null!");
            return;
        }

        GameObject newItem = Instantiate(prefab, position, Quaternion.identity);
        spawnedItems.Add(newItem);

        SpriteRenderer sr = newItem.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("Spawned item has no SpriteRenderer!");
        }
        else if (sr.sprite == null)
        {
            Debug.LogError("Spawned item SpriteRenderer has no sprite!");
        }

        Debug.Log($"✅ Spawned: {prefab.name} at {position} (Visible: {sr != null && sr.sprite != null})");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 center = player != null ? player.position : transform.position;
        Gizmos.DrawWireCube(center, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0));

        Gizmos.color = Color.red;
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                Gizmos.DrawWireSphere(item.transform.position, 0.5f);
            }
        }
    }

    [ContextMenu("💥 TEST SPAWN NOW")]
    void TestSpawn()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        if (player != null && commonLootPrefabs.Count > 0)
        {
            Vector2 spawnPos = GetSpawnPosition();
            if (spawnPos != Vector2.zero)
            {
                SpawnItemAtPosition(spawnPos);
            }
        }
        else
        {
            Debug.LogError("Cannot test: Player or prefabs missing");
        }
    }
}