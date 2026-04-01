using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterSelectMenu : MonoBehaviour
{
    [Header("Start Button")]
    public Button startButton;
    public TMP_Text startButtonTextBG;
    public TMP_Text startButtonText;

    [Header("Button Colors")]
    public Color enabledButtonColor = Color.white;
    public Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    public Color enabledTextColor = Color.black;
    public Color disabledTextColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Panel Roots")]
    [SerializeField] private GameObject player1PanelRoot;
    [SerializeField] private GameObject player2PanelRoot;

    [Header("Optional Panel Scripts")]
    [SerializeField] private PlayerSetupPanel player1Panel;
    [SerializeField] private PlayerSetupPanel player2Panel;

    [SerializeField] private MenuManager menuManager;

    private void Awake()
    {
        if (menuManager == null)
            menuManager = FindObjectOfType<MenuManager>();
    }

    private void OnEnable()
    {
        RefreshLayout();
    }

    private void Update()
    {
        RefreshLayout();

        bool canStart = AreRequiredPlayersReady();

        if (startButton != null)
            startButton.interactable = canStart;

        if (startButtonText != null)
            startButtonText.color = canStart ? enabledTextColor : disabledTextColor;

        Image buttonImage = startButton != null ? startButton.GetComponent<Image>() : null;
        if (buttonImage != null)
            buttonImage.color = canStart ? enabledButtonColor : disabledButtonColor;
    }

    private void RefreshLayout()
    {
        if (PlayerSetupManager.Instance == null) return;

        bool singlePlayer = PlayerSetupManager.Instance.IsSinglePlayer;

        if (player1PanelRoot != null)
            player1PanelRoot.SetActive(true);

        if (player2PanelRoot != null)
            player2PanelRoot.SetActive(!singlePlayer);

        // Reset player 2 ready state when hidden
        if (singlePlayer && PlayerSetupManager.Instance.players != null && PlayerSetupManager.Instance.players.Length > 1)
        {
            PlayerSetupManager.Instance.players[1].isReady = false;
        }

        if (player1Panel != null)
            player1Panel.RefreshUI();

        if (player2Panel != null && !singlePlayer)
            player2Panel.RefreshUI();
    }

    private bool AreRequiredPlayersReady()
    {
        if (PlayerSetupManager.Instance == null) return false;
        if (PlayerSetupManager.Instance.players == null) return false;
        if (PlayerSetupManager.Instance.players.Length < 2) return false;

        if (PlayerSetupManager.Instance.IsSinglePlayer)
            return PlayerSetupManager.Instance.players[0].isReady;

        return PlayerSetupManager.Instance.players[0].isReady &&
               PlayerSetupManager.Instance.players[1].isReady;
    }

    public void StartGame()
    {
        if (!AreRequiredPlayersReady()) return;
        SceneManager.LoadScene(1);
    }

    public void BackToGameModeMenu()
    {
        if (menuManager != null)
            menuManager.BackToGameModeMenu();
    }
}