using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SimpleMainMenu : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    
    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 1f;
        
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);
        
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }
    
    void StartGame()
    {
        PlayClickSound();
        Debug.Log("Starting game...");
        
        SceneManager.LoadScene("SampleScene");
    }
    
    void ExitGame()
    {
        PlayClickSound();
        Debug.Log("Exiting game...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            StartGame();
        
        if (Input.GetKeyDown(KeyCode.Escape))
            ExitGame();
    }
}