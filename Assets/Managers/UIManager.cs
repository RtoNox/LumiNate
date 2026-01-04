using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    public static UIManager Instance => instance;
    
    [Header("Battery UI")]
    [SerializeField] private Slider batterySlider;
    [SerializeField] private Image batteryFill;
    [SerializeField] private TMP_Text batteryText;  // Change Text to TMP_Text
    [SerializeField] private GameObject rechargeIndicator;
    [SerializeField] private TMP_Text rechargeRateText;
    
    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;
    
    [Header("Game UI")]
    [SerializeField] private TMP_Text itemsText;
    [SerializeField] private Text waveText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Text enemyCountText;
    
    [Header("Colors")]
    [SerializeField] private Color highBatteryColor = Color.green;
    [SerializeField] private Color mediumBatteryColor = Color.yellow;
    [SerializeField] private Color lowBatteryColor = Color.red;
    [SerializeField] private Color rechargeColor = Color.blue;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Subscribe to events if GameManager exists
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnItemCollected += UpdateItems;
            GameManager.Instance.OnEnemyCountChanged += UpdateEnemyCount;
        }
    }
    
    public void UpdateBattery(float percentage)
    {
        if (batterySlider != null)
            batterySlider.value = percentage;
        
        if (batteryFill != null)
        {
            if (percentage > 0.6f)
                batteryFill.color = highBatteryColor;
            else if (percentage > 0.3f)
                batteryFill.color = mediumBatteryColor;
            else
                batteryFill.color = lowBatteryColor;
        }
        
        if (batteryText != null)
            batteryText.text = $"{Mathf.RoundToInt(percentage * 100)}%";
    }
    
    public void UpdateHealth(float percentage)
    {
        if (healthSlider != null)
            healthSlider.value = percentage;
        
        if (healthText != null)
            healthText.text = $"{Mathf.RoundToInt(percentage * 100)}%";
    }
    
    public void UpdateItems(int collected, int total)
    {
        if (itemsText != null)
            itemsText.text = $"Items: {collected}/{total}";
    }
    
    public void UpdateItems(int collected)
    {
        // Overload for just current count
        if (itemsText != null)
        {
            // Extract total from existing text or use GameManager
            if (GameManager.Instance != null)
            {
                itemsText.text = $"Items: {collected}/{30}"; // Assuming 30 to win
            }
            else
            {
                itemsText.text = $"Items: {collected}";
            }
        }
    }
    
    public void UpdateWave(int waveNumber)
    {
        if (waveText != null)
            waveText.text = $"Wave: {waveNumber}";
    }
    
    public void UpdateWaveTimer(float timeRemaining)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = $"Next: {minutes:00}:{seconds:00}";
        }
    }
    
    public void UpdateEnemyCount(int count)
    {
        if (enemyCountText != null)
            enemyCountText.text = $"Enemies: {count}";
    }
    
    public void ShowRechargeStatus(bool isRecharging, float rechargeRate = 0f)
    {
        if (rechargeIndicator != null)
            rechargeIndicator.SetActive(isRecharging);
        
        if (rechargeRateText != null)
            rechargeRateText.text = isRecharging ? $"+{rechargeRate:F1}/s" : "";
    }
    
    [Header("Notifications")]
    [SerializeField] private Text notificationText;
    [SerializeField] private float notificationDuration = 3f;
    
    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);
            StartCoroutine(HideNotificationAfterDelay());
        }
    }
    
    private IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);
        
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }
}