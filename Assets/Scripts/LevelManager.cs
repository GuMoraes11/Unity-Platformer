using UnityEngine;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public LevelGradingData gradingData;

    private IEnumerator Start()
    {
        // Wait until the end of the first frame so LevelTimer has time to initialize
        yield return null;

        LevelTimer timer = FindObjectOfType<LevelTimer>();

        if (timer == null)
        {
            Debug.LogError("LevelManager: No LevelTimer found in scene after waiting a frame.");
            yield break;
        }

        if (gradingData == null)
        {
            Debug.LogError("LevelManager: gradingData is not assigned.");
            yield break;
        }

        timer.SetGradingData(gradingData);
        Debug.Log("LevelManager: gradingData successfully assigned to LevelTimer.");
    }
}