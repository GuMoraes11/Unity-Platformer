using UnityEngine;
using TarodevController;

public class PlayerSetupManager : MonoBehaviour
{
    public enum GameMode
    {
        SinglePlayer,
        Multiplayer
    }

    public static PlayerSetupManager Instance;

    [Header("Mode")]
    [SerializeField] private GameMode currentGameMode = GameMode.Multiplayer;

    public PlayerSetupData[] players = new PlayerSetupData[2];

    public GameMode CurrentGameMode => currentGameMode;
    public int ActivePlayerCount => currentGameMode == GameMode.SinglePlayer ? 1 : 2;
    public bool IsSinglePlayer => currentGameMode == GameMode.SinglePlayer;
    public bool IsMultiplayer => currentGameMode == GameMode.Multiplayer;

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
        if (players == null || players.Length != 2)
            players = new PlayerSetupData[2];

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

    public void SetGameMode(GameMode mode)
    {
        currentGameMode = mode;

        for (int i = 0; i < players.Length; i++)
            players[i].isReady = false;
    }
}