using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelWin : MonoBehaviour
{
    private bool levelComplete = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (levelComplete) return;
        if (!other.CompareTag("Player")) return;

        levelComplete = true;

        LevelTimer timer = FindObjectOfType<LevelTimer>();
        if (timer != null)
        {
            timer.StopTimer();
        }
        else
        {
            Debug.LogWarning("LevelWin: No LevelTimer found in scene.");
        }

        UnlockNewLevel();

        EndLevelMenuManager endMenu = FindObjectOfType<EndLevelMenuManager>();
        if (endMenu != null && timer != null)
        {
            string grade = timer.GetCurrentGrade();
            endMenu.ShowEndLevelMenu(timer.GetCurrentTime(), grade);
        }
        else
        {
            Debug.LogWarning("LevelWin: EndLevelMenuManager or LevelTimer missing.");
        }
    }

    private void UnlockNewLevel()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;

        if (currentIndex >= PlayerPrefs.GetInt("ReachedIndex"))
        {
            PlayerPrefs.SetInt("ReachedIndex", currentIndex + 1);
            PlayerPrefs.SetInt("UnlockedLevel", PlayerPrefs.GetInt("UnlockedLevel", 2) + 1);
            PlayerPrefs.Save();
        }
    }
}