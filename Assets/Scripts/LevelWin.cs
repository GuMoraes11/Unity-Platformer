using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelWin : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            LevelTimer timer = FindObjectOfType<LevelTimer>();
            if (timer != null)
                timer.StopTimer();

            UnlockNewLevel();

            // Show end level menu
            EndLevelMenuManager endMenu = FindObjectOfType<EndLevelMenuManager>();
            if (endMenu != null && timer != null)
            {
                string grade = timer.GetCurrentGrade(); // Add this method in LevelTimer
                endMenu.ShowEndLevelMenu(timer.GetCurrentTime(), grade);
            }

            // Remove scene loading here
        }
    }


    void UnlockNewLevel()
    {
        if(SceneManager.GetActiveScene().buildIndex>=PlayerPrefs.GetInt("ReachedIndex"))
        {
            PlayerPrefs.SetInt("ReachedIndex", SceneManager.GetActiveScene().buildIndex + 1);
            PlayerPrefs.SetInt("UnlockedLevel", PlayerPrefs.GetInt("UnlockedLevel", 2) + 1);
            PlayerPrefs.Save();
        }
    }
}
