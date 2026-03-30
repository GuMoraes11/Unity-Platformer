using UnityEngine;
using TMPro;
using System.Collections;

public class LevelTimer : MonoBehaviour
{
    public TMP_Text timerText;
    public TMP_Text gradeText;

    private float currentTime = 0f;
    private bool isRunning = true;

    private string levelKey;
    private string gradeKey;

    private LevelGradingData gradingData;

    private void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        levelKey = $"BestTime_{sceneName}";
        gradeKey = $"Grade_{sceneName}";
    }

    private void Update()
    {
        if (!isRunning) return;

        currentTime += Time.deltaTime;

        if (timerText != null)
            timerText.text = FormatTime(currentTime);
    }

    public void StopTimer()
    {
        if (!isRunning) return;
        isRunning = false;

        float bestTime = PlayerPrefs.GetFloat(levelKey, Mathf.Infinity);
        if (currentTime < bestTime)
        {
            PlayerPrefs.SetFloat(levelKey, currentTime);
            PlayerPrefs.Save();
            Debug.Log($"New Best Time: {FormatTime(currentTime)}");

            if (timerText != null)
                StartCoroutine(FlashColor(Color.yellow, 0.5f));
        }
        else
        {
            Debug.Log($"Finished Time: {FormatTime(currentTime)} | Best Time: {FormatTime(bestTime)}");
        }

        if (gradingData != null)
        {
            string currentGrade = gradingData.GetGrade(currentTime);
            Debug.Log($"Your grade: {currentGrade}");

            string bestGrade = PlayerPrefs.GetString(gradeKey, null);
            if (IsGradeBetter(currentGrade, bestGrade))
            {
                PlayerPrefs.SetString(gradeKey, currentGrade);
                PlayerPrefs.Save();
                Debug.Log($"New Best Grade: {currentGrade}");
            }

            if (gradeText != null)
                gradeText.text = $"Grade: {currentGrade}";
        }
        else
        {
            Debug.LogWarning("LevelTimer has no gradingData assigned. Grade will display as '-'.");
        }
    }

    private bool IsGradeBetter(string currentGrade, string bestGrade)
    {
        string[] grades = { "S", "A", "B", "F" };

        int currentIndex = System.Array.IndexOf(grades, currentGrade);
        int bestIndex = System.Array.IndexOf(grades, bestGrade);

        if (bestIndex == -1) return true;
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
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);

        return minutes > 0
            ? $"{minutes}:{seconds:00}.{milliseconds:00}"
            : $"{seconds}.{milliseconds:00}";
    }

    private IEnumerator FlashColor(Color flashColor, float duration)
    {
        Color originalColor = timerText.color;
        timerText.color = flashColor;
        yield return new WaitForSecondsRealtime(duration);
        timerText.color = originalColor;
    }

    public float GetCurrentTime() => currentTime;

    public string GetCurrentGrade()
    {
        if (gradingData != null)
            return gradingData.GetGrade(currentTime);

        return "-";
    }
}