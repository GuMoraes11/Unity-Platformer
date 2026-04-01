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
        PlayerInput.ControlScheme.KeyboardNumpad,
        PlayerInput.ControlScheme.Gamepad
    };

    private void Start()
    {
        if (nameInput != null)
            nameInput.onEndEdit.AddListener(OnNameChanged);

        if (readyToggle != null)
            readyToggle.onValueChanged.AddListener(OnReadyChanged);

        RefreshUI();
    }

    private void OnEnable()
    {
        RefreshUI();
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
        if (skins == null || skins.Length == 0) return;

        Data.skinIndex = (Data.skinIndex + 1) % skins.Length;
        RefreshUI();
    }

    public void PreviousSkin()
    {
        if (skins == null || skins.Length == 0) return;

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

            case PlayerInput.ControlScheme.Gamepad:
                return "Controller";

            case PlayerInput.ControlScheme.InputSystemActions:
            default:
                return "Input Actions";
        }
    }

    private void SetNextAvailableScheme(int direction)
    {
        int currentIndex = System.Array.IndexOf(allowedSchemes, Data.controlScheme);
        if (currentIndex < 0) currentIndex = 0;

        if (PlayerSetupManager.Instance != null && PlayerSetupManager.Instance.IsSinglePlayer)
        {
            int nextIndex = (currentIndex + direction + allowedSchemes.Length) % allowedSchemes.Length;
            Data.controlScheme = allowedSchemes[nextIndex];
            RefreshUI();
            return;
        }

        var players = PlayerSetupManager.Instance.players;
        var otherPlayer = players[1 - playerIndex];

        for (int i = 1; i <= allowedSchemes.Length; i++)
        {
            int nextIndex = (currentIndex + i * direction + allowedSchemes.Length) % allowedSchemes.Length;
            var candidate = allowedSchemes[nextIndex];

            if (candidate != otherPlayer.controlScheme)
            {
                Data.controlScheme = candidate;
                break;
            }
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (PlayerSetupManager.Instance == null) return;

        if (nameInput != null && nameInput.text != Data.playerName)
            nameInput.text = Data.playerName;

        if (controlSchemeText != null)
            controlSchemeText.text = GetControlSchemeDisplayName(Data.controlScheme);

        if (skinPreview != null && skins != null && skins.Length > 0)
            skinPreview.sprite = skins[Mathf.Clamp(Data.skinIndex, 0, skins.Length - 1)];

        if (skinNameText != null && skinNames != null && skinNames.Length > 0)
        {
            int clampedIndex = Mathf.Clamp(Data.skinIndex, 0, skinNames.Length - 1);
            skinNameText.text = skinNames[clampedIndex];
        }

        if (readyToggle != null && readyToggle.isOn != Data.isReady)
            readyToggle.isOn = Data.isReady;
    }
}