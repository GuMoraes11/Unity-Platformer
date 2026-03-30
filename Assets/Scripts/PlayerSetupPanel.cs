using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TarodevController;

public class PlayerSetupPanel : MonoBehaviour
{
    public int playerIndex;

    [Header("UI")]
    public TMP_InputField nameInput;
    public TMP_Text controlSchemeText;
    public Image skinPreview;
    public TMP_Text skinNameText;
    public Toggle readyToggle;

    [Header("Skin Data")]
    public Sprite[] skins;
    public string[] skinNames;

    private PlayerSetupData Data => PlayerSetupManager.Instance.players[playerIndex];

    private void Start()
    {
        RefreshUI();

        nameInput.onEndEdit.AddListener(OnNameChanged);
        readyToggle.onValueChanged.AddListener(OnReadyChanged);
    }

    private PlayerInput.ControlScheme[] allowedSchemes = new PlayerInput.ControlScheme[]
    {
        PlayerInput.ControlScheme.KeyboardWASD,
        PlayerInput.ControlScheme.KeyboardArrows
        // later you can add:
        // PlayerInput.ControlScheme.InputSystemActions
    };

    // -------- NAME --------
    private void OnNameChanged(string value)
    {
        Data.playerName = string.IsNullOrWhiteSpace(value)
            ? $"Player {playerIndex + 1}"
            : value.Trim();

        RefreshUI();
    }

    // -------- READY --------
    private void OnReadyChanged(bool value)
    {
        Data.isReady = value;
    }

    // -------- CONTROL --------
    public void NextControlScheme()
    {
        SetNextAvailableScheme(1);
    }

    public void PreviousControlScheme()
    {
        SetNextAvailableScheme(-1);
    }

    // -------- SKINS --------
    public void NextSkin()
    {
        Data.skinIndex = (Data.skinIndex + 1) % skins.Length;
        RefreshUI();
    }

    public void PreviousSkin()
    {
        Data.skinIndex = (Data.skinIndex - 1 + skins.Length) % skins.Length;
        RefreshUI();
    }

    // -------- GetControlSchemeDisplayName --------
    private string GetControlSchemeDisplayName(PlayerInput.ControlScheme scheme)
    {
        switch (scheme)
        {
            case PlayerInput.ControlScheme.KeyboardWASD:
                return "WASD";

            case PlayerInput.ControlScheme.KeyboardArrows:
                return "Arrow Keys";

            default:
                return "WASD"; // fallback (never show controller)
        }
    }

    private void SetNextAvailableScheme(int direction)
    {
        var players = PlayerSetupManager.Instance.players;
        var otherPlayer = players[1 - playerIndex];

        int currentIndex = System.Array.IndexOf(allowedSchemes, Data.controlScheme);

        for (int i = 1; i <= allowedSchemes.Length; i++)
        {
            int nextIndex = (currentIndex + i * direction + allowedSchemes.Length) % allowedSchemes.Length;
            var candidate = allowedSchemes[nextIndex];

            // skip if other player is using it
            if (candidate != otherPlayer.controlScheme)
            {
                Data.controlScheme = candidate;
                break;
            }
        }

        RefreshUI();
    }

    // -------- UI REFRESH --------
    private void RefreshUI()
    {
        nameInput.text = Data.playerName;
        controlSchemeText.text = GetControlSchemeDisplayName(Data.controlScheme);

        skinPreview.sprite = skins[Data.skinIndex];
        skinNameText.text = skinNames[Data.skinIndex];

        readyToggle.isOn = Data.isReady;
    }
}