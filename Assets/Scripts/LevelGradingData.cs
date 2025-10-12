using UnityEngine;

[CreateAssetMenu(menuName = "Level/Grading Data")]
public class LevelGradingData : ScriptableObject
{
    [System.Serializable]
    public struct GradeThreshold
    {
        public string gradeName; // e.g., "S", "A", "B"
        public float maxTime;    // Time in seconds (anything faster or equal earns this grade)
    }

    public GradeThreshold[] thresholds;

    public string GetGrade(float time)
    {
        foreach (var threshold in thresholds)
        {
            if (time <= threshold.maxTime)
                return threshold.gradeName;
        }
        return "F"; // Default lowest grade
    }
}
