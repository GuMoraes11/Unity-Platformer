using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pauseMenuUI;
    public GameObject mainMenuUI;
    public GameObject levelMenuUI;

    [Header("Level Buttons")]
    public Button[] levelButtons;

    private bool isPaused = false;

    void Start()
    {
        Time.timeScale = 1f;

        // UNLOCK LOGIC:
        if (levelButtons != null && levelButtons.Length > 0)
        {
            // Start UnlockedLevel at 2 to unlock Scene 1 (Level 1)
            int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 2);
            for (int i = 0; i < levelButtons.Length; i++)
            {
                // Level button index 0 maps to Scene 1 (Level 1), index 1 to Scene 2 (Level 2), etc.
                levelButtons[i].interactable = (i + 1) < unlockedLevel;
            }
        }
    }

    void Update()
    {
        if (pauseMenuUI != null && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    // Pause Menu
    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); // Main Menu
    }

    // Main Menu
    public void PlayFirstLevel()
    {
        SceneManager.LoadScene(1); // Level 1
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    public void ResetGame()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene(0); // Reload Main Menu
    }

    // Level Select
    public void OpenLevel(int levelId)
    {
        SceneManager.LoadScene("Level " + levelId);
    }

    public void OpenLevelMenu()
    {
        mainMenuUI.SetActive(false);
        levelMenuUI.SetActive(true);
    }

    public void BackToMainMenu()
    {
        levelMenuUI.SetActive(false);
        mainMenuUI.SetActive(true);
    }
}
