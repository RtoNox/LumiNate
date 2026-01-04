using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    
    [Header("Advanced Settings")]
    public bool followX = true;
    public bool followY = true;
    
    // Screen shake variables
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.7f;
    private float dampingSpeed = 1.0f;
    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Handle screen shake
        if (shakeDuration > 0)
        {
            transform.localPosition = initialPosition + Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.deltaTime * dampingSpeed;
        }
        else
        {
            // Normal camera follow
            shakeDuration = 0f;
            FollowTarget();
        }
    }
    
    void FollowTarget()
    {
        Vector3 desiredPosition = target.position + offset;
        
        // Apply axis constraints
        if (!followX) desiredPosition.x = transform.position.x;
        if (!followY) desiredPosition.y = transform.position.y;
        
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
        
        // Update initial position for screen shake
        initialPosition = transform.position;
    }
    
    // Public method to trigger screen shake
    public void TriggerShake(float duration, float magnitude = 0.7f)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}
