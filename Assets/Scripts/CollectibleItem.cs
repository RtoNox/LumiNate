using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float floatAmount = 0.5f;
    [SerializeField] private Light itemLight;
    [SerializeField] private Color itemColor = Color.yellow;
    
    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float collectVolume = 0.5f;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem collectParticles;
    
    private Vector3 startPosition;
    private Renderer itemRenderer;
    private bool isCollected = false;
    
    public event Action OnCollected;
    
    void Start()
    {
        startPosition = transform.position;
        itemRenderer = GetComponent<Renderer>();
        
        // Setup visuals
        if (itemRenderer != null)
        {
            itemRenderer.material.color = itemColor;
        }
        
        if (itemLight != null)
        {
            itemLight.color = itemColor;
        }
        
        // Set tag for detection
        gameObject.tag = "Collectible";
        
        // Add collider if not present
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
        }
    }
    
    void Update()
    {
        if (isCollected) return;
        
        // Rotate
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Float up and down
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        
        // Pulse light intensity
        if (itemLight != null)
        {
            itemLight.intensity = 1f + Mathf.Sin(Time.time * 2f) * 0.5f;
        }
    }
    
    public void Collect()
    {
        if (isCollected) return;
        
        isCollected = true;
        
        // Play sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, collectVolume);
        }
        
        // Play particles
        if (collectParticles != null)
        {
            ParticleSystem particles = Instantiate(collectParticles, transform.position, Quaternion.identity);
            ParticleSystem.MainModule main = particles.main;
            main.startColor = itemColor;
            particles.Play();
            Destroy(particles.gameObject, 2f);
        }
        
        // Visual feedback
        StartCoroutine(CollectAnimation());
        
        // Notify subscribers
        OnCollected?.Invoke();
    }
    
    IEnumerator CollectAnimation()
    {
        // Shrink and fade out
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Shrink
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            // Fade
            if (itemRenderer != null)
            {
                Color color = itemRenderer.material.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                itemRenderer.material.color = color;
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    void OnDrawGizmos()
    {
        // Draw a sphere in editor to visualize pickup radius
        Gizmos.color = new Color(itemColor.r, itemColor.g, itemColor.b, 0.3f);
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}