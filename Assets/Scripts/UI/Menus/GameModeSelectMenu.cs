using UnityEngine;

public class GameModeSelectMenu : MonoBehaviour
{
    [SerializeField] private MenuManager menuManager;

    private void Awake()
    {
        if (menuManager == null)
            menuManager = FindObjectOfType<MenuManager>();
    }

    public void SelectSinglePlayer()
    {
        if (PlayerSetupManager.Instance != null)
            PlayerSetupManager.Instance.SetGameMode(PlayerSetupManager.GameMode.SinglePlayer);

        if (menuManager != null)
            menuManager.OpenCharacterSelectMenu();
    }

    public void SelectMultiplayer()
    {
        if (PlayerSetupManager.Instance != null)
            PlayerSetupManager.Instance.SetGameMode(PlayerSetupManager.GameMode.Multiplayer);

        if (menuManager != null)
            menuManager.OpenCharacterSelectMenu();
    }

    public void BackToMainMenu()
    {
        if (menuManager != null)
            menuManager.BackToMainMenuPanels();
    }
}