using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public LevelGradingData gradingData; // Assign this per level

    void Start()
    {
        // Find the LevelTimer (attached to player) and assign grading data
        LevelTimer timer = FindObjectOfType<LevelTimer>();
        if (timer != null && gradingData != null)
        {
            timer.SetGradingData(gradingData);
        }
    }
}
