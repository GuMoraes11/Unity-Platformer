using UnityEngine;
using TMPro;

public class GradeDisplay : MonoBehaviour
{
    [System.Serializable]
    public struct LevelGradeUI
    {
        public string sceneName;              // Scene name (e.g., "Level 1")
        public TMP_Text gradeText;            // Main grade text (affected by color shifts)
        public TMP_Text gradeDropShadowText;  // Drop shadow text (fixed color)
        public Color dropShadowColor;         // Color for drop shadow text
    }

    public LevelGradeUI[] levelGrades;

    // Static colors for non-S ranks
    public Color aColor = new Color(0.75f, 0.75f, 0.75f); // Silver
    public Color bColor = new Color(0.65f, 0.45f, 0.2f);  // Bronze
    public Color fColor = Color.red;                     // Red
    public Color defaultColor = Color.white;             // White for ungraded

    private void Start()
    {
        foreach (var levelGrade in levelGrades)
        {
            string gradeKey = $"Grade_{levelGrade.sceneName}";
            string savedGrade = PlayerPrefs.GetString(gradeKey, "-"); // "-" if no grade yet

            // Set grade letter for both main and drop shadow texts
            if (levelGrade.gradeText != null)
                levelGrade.gradeText.text = savedGrade;

            if (levelGrade.gradeDropShadowText != null)
            {
                levelGrade.gradeDropShadowText.text = savedGrade;
                levelGrade.gradeDropShadowText.color = levelGrade.dropShadowColor;
            }

            // Apply static color for non-S grades
            if (savedGrade != "S" && levelGrade.gradeText != null)
                levelGrade.gradeText.color = GetColorForGrade(savedGrade);
        }
    }

    private void Update()
    {
        // Apply gold hue shift for S rank grades every frame
        foreach (var levelGrade in levelGrades)
        {
            if (levelGrade.gradeText != null && levelGrade.gradeText.text == "S")
            {
                float hue = Mathf.Lerp(0.12f, 0.16f, Mathf.PingPong(Time.time * 0.5f, 1f)); // Gold hues
                Color shiftingGold = Color.HSVToRGB(hue, 0.8f, 1f);
                levelGrade.gradeText.color = shiftingGold;
            }
        }
    }

    Color GetColorForGrade(string grade)
    {
        switch (grade)
        {
            case "A": return aColor;
            case "B": return bColor;
            case "F": return fColor;
            default: return defaultColor;
        }
    }
}
