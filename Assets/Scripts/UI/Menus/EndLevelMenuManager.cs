using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EndLevelMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject endLevelMenuUI;
    public TMP_Text currentTimeText;
    public TMP_Text bestTimeText;
    public TMP_Text currentGradeText;
    public TMP_Text bestGradeText;

    private string levelKey;
    private string gradeKey;

    private void Start()
    {
        // Ensure menu is hidden at start
        if (endLevelMenuUI != null)
            endLevelMenuUI.SetActive(false);

        levelKey = $"BestTime_{SceneManager.GetActiveScene().name}";
        gradeKey = $"Grade_{SceneManager.GetActiveScene().name}";
    }

    public void ShowEndLevelMenu(float currentTime, string currentGrade)
    {
        Time.timeScale = 0f;

        if (endLevelMenuUI != null)
            endLevelMenuUI.SetActive(true);

        // Current Time
        currentTimeText.text = $"Time: {FormatTime(currentTime)}";

        // Best Time
        float bestTime = PlayerPrefs.GetFloat(levelKey, Mathf.Infinity);
        bestTimeText.text = bestTime < Mathf.Infinity ? $"Best: {FormatTime(bestTime)}" : "Best: -";

        // Current Grade
        currentGradeText.text = $"Grade: {currentGrade}";

        // Best Grade
        string bestGrade = PlayerPrefs.GetString(gradeKey, "-");
        bestGradeText.text = $"Best Grade: {bestGrade}";
    }

    public void RetryLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(0); // Assuming Main Menu is scene 0
    }

    public void NextLevel()
    {
        Time.timeScale = 1f; // Unfreeze time

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex); // Load next level
        }
        else
        {
            // If no more levels, maybe go back to main menu?
            SceneManager.LoadScene(0);
        }
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);
        return minutes > 0 ? $"{minutes}:{seconds:00}.{milliseconds:00}" : $"{seconds}.{milliseconds:00}";
    }
}
