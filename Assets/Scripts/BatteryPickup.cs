using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float rechargeAmount = 50f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float floatHeight = 0.5f;
    [SerializeField] private float floatSpeed = 2f;
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem pickupParticles;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private Light glowLight;
    
    private Vector3 startPosition;
    private AudioSource audioSource;
    private bool isCollected = false;
    
    void Start()
    {
        startPosition = transform.position;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    void Update()
    {
        if (isCollected) return;
        
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        
        if (glowLight != null)
        {
            glowLight.intensity = 1f + Mathf.Sin(Time.time * 2f) * 0.5f;
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected || !other.CompareTag("Player")) return;
        
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.Recharge(rechargeAmount);
            
            isCollected = true;
            
            if (pickupParticles != null)
            {
                Instantiate(pickupParticles, transform.position, Quaternion.identity);
            }
            
            if (audioSource != null && pickupSound != null)
            {
                audioSource.PlayOneShot(pickupSound);
            }
            
            GetComponent<SpriteRenderer>().enabled = false;
            if (glowLight != null)
                glowLight.enabled = false;
            GetComponent<Collider2D>().enabled = false;
            
            Destroy(gameObject, pickupSound != null ? pickupSound.length : 1f);
        }
    }
}