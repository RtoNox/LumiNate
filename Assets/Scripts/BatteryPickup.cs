using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    [SerializeField] private float chargeAmount = 25f;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float batteryCharge = 25f;

    public float ChargeAmount => chargeAmount;
    
    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.Battery.AddCharge(chargeAmount);
                
                if (pickupSound != null)
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                
                Destroy(gameObject);
            }
        }
    }
    public void SetChargeAmount(float amount)
    {
        batteryCharge = amount;
    }
}