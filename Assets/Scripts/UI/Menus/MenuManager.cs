using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject pauseMenuUI;
    public GameObject mainMenuUI;
    public GameObject gameModeMenuUI;
    public GameObject characterSelectMenuUI;
    public GameObject levelMenuUI;

    [Header("Level Buttons")]
    public Button[] levelButtons;

    private bool isPaused = false;

    private void Awake()
    {
        // Replace any older menu manager that may have been left alive.
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        if (levelButtons != null && levelButtons.Length > 0)
        {
            int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 2);
            for (int i = 0; i < levelButtons.Length; i++)
            {
                levelButtons[i].interactable = (i + 1) < unlockedLevel;
            }
        }

        if (mainMenuUI != null || gameModeMenuUI != null || characterSelectMenuUI != null || levelMenuUI != null)
        {
            ShowOnly(mainMenuUI);
        }
    }

    private void Update()
    {
        bool pausePressed = Input.GetKeyDown(KeyCode.Escape);

    #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Gamepad.current != null &&
            UnityEngine.InputSystem.Gamepad.current.startButton.wasPressedThisFrame)
        {
            pausePressed = true;
        }
    #endif

        if (pauseMenuUI != null && pausePressed)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    private void ShowOnly(GameObject target)
    {
        if (mainMenuUI != null) mainMenuUI.SetActive(target == mainMenuUI);
        if (gameModeMenuUI != null) gameModeMenuUI.SetActive(target == gameModeMenuUI);
        if (characterSelectMenuUI != null) characterSelectMenuUI.SetActive(target == characterSelectMenuUI);
        if (levelMenuUI != null) levelMenuUI.SetActive(target == levelMenuUI);
    }

    public void OpenGameModeMenu()
    {
        ShowOnly(gameModeMenuUI);
    }

    public void OpenCharacterSelectMenu()
    {
        ShowOnly(characterSelectMenuUI);
    }

    public void OpenLevelMenu()
    {
        ShowOnly(levelMenuUI);
    }

    public void BackToMainMenuPanels()
    {
        ShowOnly(mainMenuUI);
    }

    public void BackToGameModeMenu()
    {
        ShowOnly(gameModeMenuUI);
    }

    public void Pause()
    {
        if (pauseMenuUI == null) return;

        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Resume()
    {
        if (pauseMenuUI == null) return;

        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(0);
    }

    public void OpenLevel(int levelId)
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("Level " + levelId);
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
        Time.timeScale = 1f;
        isPaused = false;
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene(0);
    }
}