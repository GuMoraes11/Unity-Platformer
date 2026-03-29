using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [System.Serializable]
    public class Achievement
    {
        public string id;          // e.g., "lvl1_S_rank"
        public string displayName; // e.g., "S Rank: Level 1"
        [TextArea] public string description;
    }

    [Header("Catalog (optional, for menus)")]
    public List<Achievement> catalog = new List<Achievement>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsUnlocked(string id)
    {
        return PlayerPrefs.GetInt(GetKey(id), 0) == 1;
    }

    /// <summary>
    /// Unlocks an achievement by id (idempotent). Returns true if it was newly unlocked.
    /// </summary>
    public bool Unlock(string id)
    {
        if (IsUnlocked(id)) return false;
        PlayerPrefs.SetInt(GetKey(id), 1);
        PlayerPrefs.Save();
        Debug.Log($"[Achievements] Unlocked: {id}");
        return true;
    }

    /// <summary>
    /// Utility to generate a consistent S-rank id from a scene name.
    /// "Level 1" -> "lvl1_S_rank"; general fallback -> lowercase, spaces -> underscores.
    /// </summary>
    public static string MakeSRankIdFromScene(string sceneName)
    {
        string slug = sceneName.Trim().ToLower().Replace("level ", "lvl").Replace(" ", "_");
        return $"{slug}_S_rank";
    }

    private string GetKey(string id) => $"Achv_{id}";
}