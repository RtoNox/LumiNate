using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;
    
    [Header("Pause Menu Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitFromPauseButton;
    
    [Header("Game Over/Win Panel")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text timeText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;
    
    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip buttonClickSound;
    
    void Start()
    {
        SetupButtonListeners();
        
        GameManager.Instance.OnGamePaused += OnGamePaused;
        GameManager.Instance.OnGameResumed += OnGameResumed;
        GameManager.Instance.OnGameWon += OnGameWon;
        GameManager.Instance.OnGameOver += OnGameOver;
        
        HideAllPanels();
    }
    
    public void PauseGame()
    {
        ShowPauseMenu();
    }

    void SetupButtonListeners()
    {
        if (resumeButton != null) 
            resumeButton.onClick.AddListener(() => 
            {
                PlaySound(buttonClickSound);
                GameManager.Instance.ResumeGame();
            });
            
        if (restartButton != null) 
            restartButton.onClick.AddListener(() => 
            {
                PlaySound(buttonClickSound);
                GameManager.Instance.RestartGame();
            });
            
        if (mainMenuButton != null) 
            mainMenuButton.onClick.AddListener(() => 
            {
                PlaySound(buttonClickSound);
                GameManager.Instance.LoadMainMenu();
            });
            
        if (quitFromPauseButton != null) 
            quitFromPauseButton.onClick.AddListener(() => 
            {
                PlaySound(buttonClickSound);
                GameManager.Instance.QuitToDesktop();
            });
        
        if (retryButton != null) 
            retryButton.onClick.AddListener(() => 
            {
                PlaySound(buttonClickSound);
                GameManager.Instance.RestartGame();
            });
            
        if (menuButton != null) 
            menuButton.onClick.AddListener(() => 
            {
                PlaySound(buttonClickSound);
                GameManager.Instance.LoadMainMenu();
            });
    }
    
    #region Panel Management
    
    void ShowPauseMenu()
    {
        HideAllPanels();
        if (pauseMenuPanel != null) 
            pauseMenuPanel.SetActive(true);
    }
    
    void ShowGameOverPanel(float timePlayed)
    {
        HideAllPanels();
        if (gameOverPanel != null) 
        {
            gameOverPanel.SetActive(true);
            
            if (titleText != null)
                titleText.text = "GAME OVER";
            
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(timePlayed / 60);
                int seconds = Mathf.FloorToInt(timePlayed % 60);
                timeText.text = $"Time: {minutes:00}:{seconds:00}";
            }
        }
    }
    
    void ShowWinPanel(float timePlayed)
    {
        HideAllPanels();
        if (winPanel != null) 
        {
            winPanel.SetActive(true);
            
            if (titleText != null)
                titleText.text = "VICTORY!";
            
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(timePlayed / 60);
                int seconds = Mathf.FloorToInt(timePlayed % 60);
                timeText.text = $"Time: {minutes:00}:{seconds:00}";
            }
        }
    }
    
    void HideAllPanels()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
    }
    
    #endregion
    
    #region GameManager Event Handlers
    
    public void OnGamePaused()
    {
        ShowPauseMenu();
    }
    
    void OnGameResumed()
    {
        HideAllPanels();
    }
    
    void OnGameWon(float timePlayed)
    {
        ShowWinPanel(timePlayed);
    }
    
    void OnGameOver(float timePlayed)
    {
        ShowGameOverPanel(timePlayed);
    }
    
    #endregion
    
    void PlaySound(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }
    
    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused -= OnGamePaused;
            GameManager.Instance.OnGameResumed -= OnGameResumed;
            GameManager.Instance.OnGameWon -= OnGameWon;
            GameManager.Instance.OnGameOver -= OnGameOver;
        }
    }
}