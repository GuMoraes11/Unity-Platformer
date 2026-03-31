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

    private readonly PlayerInput.ControlScheme[] allowedSchemes = new PlayerInput.ControlScheme[]
    {
        PlayerInput.ControlScheme.KeyboardWASD,
        PlayerInput.ControlScheme.KeyboardArrows,
        PlayerInput.ControlScheme.KeyboardNumpad
    };

    private void Start()
    {
        RefreshUI();

        nameInput.onEndEdit.AddListener(OnNameChanged);
        readyToggle.onValueChanged.AddListener(OnReadyChanged);
    }

    private void OnNameChanged(string value)
    {
        Data.playerName = string.IsNullOrWhiteSpace(value)
            ? $"Player {playerIndex + 1}"
            : value.Trim();

        RefreshUI();
    }

    private void OnReadyChanged(bool value)
    {
        Data.isReady = value;
    }

    public void NextControlScheme()
    {
        SetNextAvailableScheme(1);
    }

    public void PreviousControlScheme()
    {
        SetNextAvailableScheme(-1);
    }

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

    private string GetControlSchemeDisplayName(PlayerInput.ControlScheme scheme)
    {
        switch (scheme)
        {
            case PlayerInput.ControlScheme.KeyboardWASD:
                return "WASD";

            case PlayerInput.ControlScheme.KeyboardArrows:
                return "Arrow Keys";

            case PlayerInput.ControlScheme.KeyboardNumpad:
                return "Numpad";

            default:
                return "WASD";
        }
    }

    private void SetNextAvailableScheme(int direction)
    {
        var players = PlayerSetupManager.Instance.players;
        var otherPlayer = players[1 - playerIndex];

        int currentIndex = System.Array.IndexOf(allowedSchemes, Data.controlScheme);
        if (currentIndex < 0)
            currentIndex = 0;

        for (int i = 1; i <= allowedSchemes.Length; i++)
        {
            int nextIndex = (currentIndex + i * direction + allowedSchemes.Length) % allowedSchemes.Length;
            var candidate = allowedSchemes[nextIndex];

            // Prevent both players from picking the same control scheme
            if (candidate != otherPlayer.controlScheme)
            {
                Data.controlScheme = candidate;
                break;
            }
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        nameInput.text = Data.playerName;
        controlSchemeText.text = GetControlSchemeDisplayName(Data.controlScheme);

        if (skins != null && skins.Length > 0 && Data.skinIndex >= 0 && Data.skinIndex < skins.Length)
        {
            skinPreview.sprite = skins[Data.skinIndex];
        }

        if (skinNames != null && skinNames.Length > 0 && Data.skinIndex >= 0 && Data.skinIndex < skinNames.Length)
        {
            skinNameText.text = skinNames[Data.skinIndex];
        }

        readyToggle.isOn = Data.isReady;
    }
}