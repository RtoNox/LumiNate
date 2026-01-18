using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Scene Names")]
    public string mainMenuScene = "MainMenu";
    public string gameScene = "SampleScene";
    
    [Header("Game State")]
    private bool isPaused = false;
    private bool gameEnded = false;
    private float timePlayed = 0f;
    
    private PlayerController player;
    
    public event Action OnGamePaused;
    public event Action OnGameResumed;
    public event Action<float> OnGameWon;
    public event Action<float> OnGameOver;
    
    public bool IsPaused => isPaused;
    public bool GameEnded => gameEnded;
    public float TimePlayed => timePlayed;
    public string FormattedTimePlayed 
    { 
        get 
        {
            int minutes = Mathf.FloorToInt(timePlayed / 60);
            int seconds = Mathf.FloorToInt(timePlayed % 60);
            return $"{minutes:00}:{seconds:00}";
        }
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");
        
        if (scene.name == gameScene)
        {
            SetupGameScene();
        }
        else if (scene.name == mainMenuScene)
        {
            SetupMainMenuScene();
        }
    }
    
    void SetupGameScene()
    {
        FindPlayer();
        
        if (player != null)
        {
            player.OnDeath += OnPlayerDied;
            player.OnWinConditionMet += OnPlayerWon;
        }
        
        isPaused = false;
        gameEnded = false;
        timePlayed = 0f;
        Time.timeScale = 1f;
        
        Debug.Log("Game scene setup complete");
    }
    
    void SetupMainMenuScene()
    {
        isPaused = false;
        gameEnded = false;
        Time.timeScale = 1f;
        
        if (player != null)
        {
            player.OnDeath -= OnPlayerDied;
            player.OnWinConditionMet -= OnPlayerWon;
            player = null;
        }
    }
    
    void Update()
    {
        if (SceneManager.GetActiveScene().name == gameScene)
        {
            if (!isPaused && !gameEnded)
            {
                timePlayed += Time.deltaTime;
            }
            
            if (Input.GetKeyDown(KeyCode.Escape) && !gameEnded)
            {
                if (isPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }
    }
    
    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerController>();
            Debug.Log("Player found");
        }
        else
        {
            Debug.LogWarning("Player not found in scene!");
        }
    }
    
    void OnPlayerDied()
    {
        if (gameEnded) return;
        
        EndGame(false);
    }
    
    void OnPlayerWon()
    {
        Debug.Log("GameManager: OnPlayerWon called");
        
        if (gameEnded) 
        {
            Debug.Log("GameManager: Game already ended, ignoring win");
            return;
        }
        
        EndGame(true);
    }

    void EndGame(bool isVictory)
    {
        Debug.Log($"GameManager: EndGame called with isVictory={isVictory}");
        
        gameEnded = true;
        Time.timeScale = 0f;
        
        float finalTime = timePlayed;
        Debug.Log($"GameManager: Final time = {finalTime}");
        
        if (isVictory)
        {
            Debug.Log($"GameManager: Triggering OnGameWon event");
            OnGameWon?.Invoke(finalTime);
        }
        else
        {
            Debug.Log($"GameManager: Triggering OnGameOver event");
            OnGameOver?.Invoke(finalTime);
        }
    }
    
    #region Public Game Control Methods
    
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
    
    public void LoadGameScene()
    {
        SceneManager.LoadScene(gameScene);
    }
    
    public void PauseGame()
    {
        if (gameEnded || SceneManager.GetActiveScene().name != gameScene) return;
        
        isPaused = true;
        Time.timeScale = 0f;
        
        OnGamePaused?.Invoke();
        Debug.Log("Game Paused");
    }
    
    public void ResumeGame()
    {
        if (gameEnded || SceneManager.GetActiveScene().name != gameScene) return;
        
        isPaused = false;
        Time.timeScale = 1f;
        
        OnGameResumed?.Invoke();
        Debug.Log("Game Resumed");
    }
    
    public void RestartGame()
    {
        SceneManager.LoadScene(gameScene);
        Debug.Log("Restarting game...");
    }
    
    public void QuitToDesktop()
    {
        Debug.Log("Quitting to desktop...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    #endregion
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (player != null)
        {
            player.OnDeath -= OnPlayerDied;
            player.OnWinConditionMet -= OnPlayerWon;
        }
    }
}