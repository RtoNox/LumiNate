using UnityEngine;

public class WinItem : MonoBehaviour
{
    [Header("Win Item Settings")]
    [SerializeField] private string itemName = "Light Shard";
    
    [Header("Visual Effects")]
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private float floatHeight = 0.3f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private Color glowColor = Color.yellow;
    [SerializeField] private float glowIntensity = 2f;
    
    private Vector3 startPosition;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        gameObject.tag = "WinItem";
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = glowColor;
        }
    }
    
    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        
        if (spriteRenderer != null)
        {
            float pulse = Mathf.PingPong(Time.time * 2f, 1f);
            Color pulseColor = glowColor * (1 + pulse * glowIntensity);
            spriteRenderer.color = pulseColor;
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                Debug.Log($"Collected {itemName}!");
            }
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}