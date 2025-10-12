using UnityEngine;
using TMPro;
using System.Collections;

public class LevelTimer : MonoBehaviour
{
    public TMP_Text timerText;   // Assign in inspector (timer display)
    public TMP_Text gradeText;   // Optional: Assign in inspector (grade display)
    private float currentTime = 0f;
    private bool isRunning = true;

    private string levelKey;  // For best time saving
    private string gradeKey;  // For best grade saving

    private LevelGradingData gradingData; // Set via LevelManager

    void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        levelKey = $"BestTime_{sceneName}";
        gradeKey = $"Grade_{sceneName}";
    }

    void Update()
    {
        if (!isRunning) return;

        currentTime += Time.deltaTime;
        if (timerText != null)
        {
            timerText.text = FormatTime(currentTime);
        }
    }

    public void StopTimer()
    {
        isRunning = false;

        float bestTime = PlayerPrefs.GetFloat(levelKey, Mathf.Infinity);
        if (currentTime < bestTime)
        {
            PlayerPrefs.SetFloat(levelKey, currentTime);
            PlayerPrefs.Save();
            Debug.Log($"New Best Time: {FormatTime(currentTime)}");

            // Optional: Flash color for PB
            if (timerText != null)
                StartCoroutine(FlashColor(Color.yellow, 0.5f));
        }
        else
        {
            Debug.Log($"Finished Time: {FormatTime(currentTime)} | Best Time: {FormatTime(bestTime)}");
        }

        // Handle grading
        if (gradingData != null)
        {
            string currentGrade = gradingData.GetGrade(currentTime);
            Debug.Log($"Your grade: {currentGrade}");

            // Compare and save best grade only if current is better
            string bestGrade = PlayerPrefs.GetString(gradeKey, null);
            if (IsGradeBetter(currentGrade, bestGrade))
            {
                PlayerPrefs.SetString(gradeKey, currentGrade);
                PlayerPrefs.Save();
                Debug.Log($"New Best Grade: {currentGrade}");
            }

            // Display current grade
            if (gradeText != null)
                gradeText.text = $"Grade: {currentGrade}";
        }
    }

    private bool IsGradeBetter(string currentGrade, string bestGrade)
    {
        // Define grade rankings (higher index = worse)
        string[] grades = { "S", "A", "B", "F" };

        int currentIndex = System.Array.IndexOf(grades, currentGrade);
        int bestIndex = System.Array.IndexOf(grades, bestGrade);

        // If no previous best grade, current is better
        if (bestIndex == -1) return true;

        // Current grade is better if its index is lower
        return currentIndex < bestIndex;
    }

    public void SetGradingData(LevelGradingData data)
    {
        gradingData = data;
    }

    public static string GetBestTimeString()
    {
        string key = $"BestTime_{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}";
        float bestTime = PlayerPrefs.GetFloat(key, Mathf.Infinity);
        return bestTime < Mathf.Infinity ? FormatTime(bestTime) : "No Time Yet";
    }

    private static string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f); // Two decimal places

        if (minutes > 0)
            return $"{minutes}:{seconds:00}.{milliseconds:00}";
        else
            return $"{seconds}.{milliseconds:00}";
    }

    private IEnumerator FlashColor(Color flashColor, float duration)
    {
        Color originalColor = timerText.color;
        timerText.color = flashColor;
        yield return new WaitForSecondsRealtime(duration); // Use unscaled time to work with time freeze
        timerText.color = originalColor;
    }

    public float GetCurrentTime()
    {
        return currentTime;
    }

    public string GetCurrentGrade()
    {
        if (gradingData != null)
            return gradingData.GetGrade(currentTime);
        return "-";
    }
}
