using UnityEngine;

public class GameSkinDatabase : MonoBehaviour
{
    public static GameSkinDatabase Instance;

    public Sprite[] skins;

    private void Awake()
    {
        Instance = this;
    }
}