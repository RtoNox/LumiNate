using UnityEngine;
using System.Collections.Generic;

public class RevealCircle : MonoBehaviour
{
    [Header("Reveal Settings")]
    [SerializeField] private float revealRadius = 5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float updateRate = 0.2f;
    
    [Header("Visual")]
    [SerializeField] private bool showVisual = true;
    [SerializeField] private Color circleColor = new Color(1, 1, 1, 0.2f);
    
    private SpriteRenderer circleRenderer;
    
    private float lastUpdateTime;
    private List<BaseEnemy> revealedEnemies = new List<BaseEnemy>();
    
    void Start()
    {
        if (showVisual)
        {
            CreateVisualCircle();
        }
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime >= updateRate)
        {
            UpdateReveal();
            lastUpdateTime = Time.time;
        }
    }
    
    void CreateVisualCircle()
    {
        GameObject circleObj = new GameObject("RevealCircleVisual");
        circleObj.transform.SetParent(transform);
        circleObj.transform.localPosition = Vector3.zero;
        
        circleRenderer = circleObj.AddComponent<SpriteRenderer>();
        
        circleRenderer.sprite = CreateCircleSprite(128);
        circleRenderer.color = circleColor;
        circleRenderer.sortingOrder = -1;
        
        float scale = revealRadius * 2f;
        circleObj.transform.localScale = new Vector3(scale, scale, 1);
    }
    
    Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 0f;
                
                if (distance <= radius)
                {
                    float normalizedDistance = distance / radius;
                    alpha = 1f - Mathf.Pow(normalizedDistance, 2f);
                    alpha = Mathf.Clamp01(alpha) * circleColor.a;
                }
                
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        
        texture.Apply();
        
        return Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f)
        );
    }
    
    void UpdateReveal()
    {
        CleanupRevealedEnemies();
        
        RevealNewEnemies();
    }
    
    void CleanupRevealedEnemies()
    {
        for (int i = revealedEnemies.Count - 1; i >= 0; i--)
        {
            if (revealedEnemies[i] == null || revealedEnemies[i].IsDead)
            {
                revealedEnemies.RemoveAt(i);
                continue;
            }
            
            float distance = Vector2.Distance(transform.position, revealedEnemies[i].transform.position);
            
            if (distance > revealRadius || !HasLineOfSight(revealedEnemies[i].transform.position))
            {
                revealedEnemies[i].Hide();
                revealedEnemies.RemoveAt(i);
            }
        }
    }
    
    void RevealNewEnemies()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, revealRadius, enemyLayer);
        
        foreach (Collider2D hit in hits)
        {
            BaseEnemy enemy = hit.GetComponent<BaseEnemy>();
            if (enemy != null && !enemy.IsDead && !revealedEnemies.Contains(enemy))
            {
                if (HasLineOfSight(enemy.transform.position))
                {
                    enemy.Reveal();
                    revealedEnemies.Add(enemy);
                }
            }
        }
    }
    
    bool HasLineOfSight(Vector3 targetPosition)
    {
        Vector2 direction = targetPosition - transform.position;
        float distance = direction.magnitude;
        
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction.normalized,
            distance,
            obstacleLayer
        );
        
        return hit.collider == null || hit.collider.gameObject.transform.position == targetPosition;
    }
    
    public void SetRevealRadius(float newRadius)
    {
        revealRadius = Mathf.Max(0.5f, newRadius);
        
        if (circleRenderer != null)
        {
            float scale = revealRadius * 2f;
            circleRenderer.transform.localScale = new Vector3(scale, scale, 1);
        }
    }
    
    public void SetCircleVisible(bool visible)
    {
        if (circleRenderer != null)
        {
            circleRenderer.enabled = visible;
        }
    }
    
    void OnDestroy()
    {
        foreach (BaseEnemy enemy in revealedEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.Hide();
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, revealRadius);
    }
}