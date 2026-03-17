using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelWin : MonoBehaviour
{
    private readonly HashSet<GameObject> playersInGoal = new HashSet<GameObject>();
    private bool levelComplete = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GameObject rootPlayer = other.transform.root.gameObject;
        playersInGoal.Add(rootPlayer);

        TryCompleteLevel();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GameObject rootPlayer = other.transform.root.gameObject;
        playersInGoal.Remove(rootPlayer);
    }

    private void TryCompleteLevel()
    {
        if (levelComplete) return;

        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        if (allPlayers.Length == 0) return;

        // Require all players to be in the flag zone
        foreach (GameObject player in allPlayers)
        {
            if (!playersInGoal.Contains(player))
                return;
        }

        levelComplete = true;

        LevelTimer timer = FindObjectOfType<LevelTimer>();
        if (timer != null)
            timer.StopTimer();

        UnlockNewLevel();

        EndLevelMenuManager endMenu = FindObjectOfType<EndLevelMenuManager>();
        if (endMenu != null && timer != null)
        {
            string grade = timer.GetCurrentGrade();
            endMenu.ShowEndLevelMenu(timer.GetCurrentTime(), grade);
        }
        else
        {
            Debug.LogWarning("EndLevelMenuManager or LevelTimer not found when completing level.");
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