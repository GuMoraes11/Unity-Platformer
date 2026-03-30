using UnityEngine;
using TarodevController;

public class PlayerSetupManager : MonoBehaviour
{
    public static PlayerSetupManager Instance;

    public PlayerSetupData[] players = new PlayerSetupData[2];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        for (int i = 0; i < players.Length; i++)
        {
            players[i] = new PlayerSetupData
            {
                playerIndex = i,
                playerName = $"Player {i + 1}",
                controlScheme = i == 0
                    ? PlayerInput.ControlScheme.KeyboardWASD
                    : PlayerInput.ControlScheme.KeyboardArrows,
                skinIndex = 0,
                isReady = false
            };
        }
    }
}