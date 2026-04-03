using UnityEngine.SceneManagement;

public static class SaveKeys
{
    public static string SceneName => SceneManager.GetActiveScene().name;

    public static string BestTimeKey(string sceneName)  => $"BestTime_{sceneName}";
    public static string BestGradeKey(string sceneName) => $"BestGrade_{sceneName}";
}