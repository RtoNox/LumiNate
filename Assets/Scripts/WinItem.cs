using UnityEngine;

public class WinItem : MonoBehaviour
{
    [Header("Win Item Settings")]
    [SerializeField] private string itemName = "Key";
    
    [Header("Visuals")]
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minScale = 0.9f;
    [SerializeField] private float maxScale = 1.1f;
    
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gameObject.tag = "WinItem";
    }
    
    void Update()
    {
        // Rotate
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        
        // Pulse scale
        float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
        float scale = Mathf.Lerp(minScale, maxScale, pulse);
        transform.localScale = new Vector3(scale, scale, 1);
        
        // Pulse color (optional gold glow)
        if (spriteRenderer != null)
        {
            float colorPulse = Mathf.Sin(Time.time * 2f) * 0.3f + 0.7f;
            spriteRenderer.color = new Color(1f, 0.84f, 0f, colorPulse);
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
                // PlayerController handles the actual collection
            }
        }
    }
}