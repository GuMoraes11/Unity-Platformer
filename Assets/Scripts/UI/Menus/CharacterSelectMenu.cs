using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterSelectMenu : MonoBehaviour
{
    public Button startButton;
    public TMP_Text startButtonTextBG;
    public TMP_Text startButtonText;

    [Header("Button Colors")]
    public Color enabledButtonColor = Color.white;
    public Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    public Color enabledTextColor = Color.black;
    public Color disabledTextColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    private void Update()
    {
        bool bothReady =
            PlayerSetupManager.Instance != null &&
            PlayerSetupManager.Instance.players.Length >= 2 &&
            PlayerSetupManager.Instance.players[0].isReady &&
            PlayerSetupManager.Instance.players[1].isReady;

        startButton.interactable = bothReady;

        if (startButtonText != null)
            startButtonText.color = bothReady ? enabledTextColor : disabledTextColor;
    }

    public void StartGame()
    {
        if (PlayerSetupManager.Instance == null) return;
        if (!PlayerSetupManager.Instance.players[0].isReady) return;
        if (!PlayerSetupManager.Instance.players[1].isReady) return;

        SceneManager.LoadScene(1);
    }
}