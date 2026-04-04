using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ColorManager : MonoBehaviour
{
    public static ColorManager Instance { get; private set; }

    public enum PlayerOwner
    {
        None,
        Player1,
        Player2
    }

    [System.Serializable]
    public class PlatformColorGroup
    {
        public string displayName;
        public GameObject platformParent;
        public PlayerOwner controlledBy = PlayerOwner.None;
        public Color groupColor = Color.white;
    }

    [Header("Platform Groups")]
    [SerializeField] private List<PlatformColorGroup> platformGroups = new List<PlatformColorGroup>();

    [Header("Visual Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float activeAlpha = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float inactiveAlpha = 0.2f;

    [Header("Shared Color Sync")]
    [SerializeField] private List<SpriteRenderer> backgroundRenderers = new List<SpriteRenderer>();
    [SerializeField] private Color neutralColor = Color.gray;

    [Header("Startup")]
    [SerializeField] private bool startWithPlayer1ColorActive = false;
    [SerializeField] private bool startWithPlayer2ColorActive = false;

    private int _player1ActiveGroupIndex = -1;
    private int _player2ActiveGroupIndex = -1;
    private int _singlePlayerActiveGroupIndex = -1;

    private int _lastVisualGroupIndex = -1;

    private readonly List<int> _player1OwnedIndices = new();
    private readonly List<int> _player2OwnedIndices = new();
    private readonly List<int> _allValidIndices = new();
    private readonly List<int> _singlePlayerCycleIndices = new();

    public event Action<Color> OnSharedActiveColorChanged;

    private bool IsSinglePlayer =>
        PlayerSetupManager.Instance != null && PlayerSetupManager.Instance.IsSinglePlayer;

    public Color CurrentSharedColor
    {
        get
        {
            int index = GetCurrentSharedVisualGroupIndex();
            if (index >= 0 && index < platformGroups.Count && platformGroups[index] != null)
                return platformGroups[index].groupColor;

            return neutralColor;
        }
    }

    private const KeyCode Player1CycleLeftKey = KeyCode.Q;
    private const KeyCode Player1CycleRightKey = KeyCode.E;

    private const KeyCode Player2CycleLeftKey = KeyCode.LeftBracket;
    private const KeyCode Player2CycleRightKey = KeyCode.RightBracket;

    private const KeyCode NumpadCycleLeftKey = KeyCode.Keypad7;
    private const KeyCode NumpadCycleRightKey = KeyCode.Keypad9;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        RebuildOwnershipLists();

        if (IsSinglePlayer)
        {
            _singlePlayerActiveGroupIndex = GetStartupIndexForSinglePlayer(startWithPlayer1ColorActive);
            _lastVisualGroupIndex = _singlePlayerActiveGroupIndex;
        }
        else
        {
            _player1ActiveGroupIndex = GetStartupIndex(PlayerOwner.Player1, startWithPlayer1ColorActive);
            _player2ActiveGroupIndex = GetStartupIndex(PlayerOwner.Player2, startWithPlayer2ColorActive);

            if (_player1ActiveGroupIndex != -1)
                _lastVisualGroupIndex = _player1ActiveGroupIndex;
            else if (_player2ActiveGroupIndex != -1)
                _lastVisualGroupIndex = _player2ActiveGroupIndex;
            else
                _lastVisualGroupIndex = -1;
        }

        RefreshAllGroups();
        RefreshSharedColorVisuals();
    }

    private void Update()
    {
        if (IsSinglePlayer)
        {
            HandleSinglePlayerInput();
            return;
        }

        HandlePlayerInput(0, PlayerOwner.Player1);

        if (CouchCoopSpawner.Instance != null && CouchCoopSpawner.Instance.Player2Instance != null)
        {
            HandlePlayerInput(1, PlayerOwner.Player2);
        }
    }

    private void HandleSinglePlayerInput()
    {
        var scheme = GetPlayerScheme(0);

        if (GetCycleLeftPressedForScheme(scheme, 0))
            CycleSinglePlayer(-1);
        else if (GetCycleRightPressedForScheme(scheme, 0))
            CycleSinglePlayer(+1);
    }

    private void HandlePlayerInput(int playerIndex, PlayerOwner owner)
    {
        var scheme = GetPlayerScheme(playerIndex);

        if (GetCycleLeftPressedForScheme(scheme, playerIndex))
            CyclePlayer(owner, -1);
        else if (GetCycleRightPressedForScheme(scheme, playerIndex))
            CyclePlayer(owner, +1);
    }

    private TarodevController.PlayerInput.ControlScheme GetPlayerScheme(int playerIndex)
    {
        if (CouchCoopSpawner.Instance != null)
        {
            GameObject playerObject = null;

            if (playerIndex == 0)
                playerObject = CouchCoopSpawner.Instance.Player1Instance;
            else if (playerIndex == 1)
                playerObject = CouchCoopSpawner.Instance.Player2Instance;

            if (playerObject != null)
            {
                var input = playerObject.GetComponent<TarodevController.PlayerInput>();
                if (input != null)
                    return input.GetScheme();
            }
        }

        if (PlayerSetupManager.Instance != null &&
            PlayerSetupManager.Instance.players != null &&
            PlayerSetupManager.Instance.players.Length > playerIndex)
        {
            return PlayerSetupManager.Instance.players[playerIndex].controlScheme;
        }

        return TarodevController.PlayerInput.ControlScheme.KeyboardWASD;
    }

    private bool GetCycleLeftPressedForScheme(TarodevController.PlayerInput.ControlScheme scheme, int playerIndex)
    {
        switch (scheme)
        {
            case TarodevController.PlayerInput.ControlScheme.KeyboardWASD:
                return Input.GetKeyDown(Player1CycleLeftKey);

            case TarodevController.PlayerInput.ControlScheme.KeyboardArrows:
                return Input.GetKeyDown(Player2CycleLeftKey);

            case TarodevController.PlayerInput.ControlScheme.KeyboardNumpad:
                return Input.GetKeyDown(NumpadCycleLeftKey);

            case TarodevController.PlayerInput.ControlScheme.Gamepad:
#if ENABLE_INPUT_SYSTEM
                return Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame;
#else
                return false;
#endif

            case TarodevController.PlayerInput.ControlScheme.InputSystemActions:
            default:
                return Input.GetKeyDown(Player1CycleLeftKey);
        }
    }

    private bool GetCycleRightPressedForScheme(TarodevController.PlayerInput.ControlScheme scheme, int playerIndex)
    {
        switch (scheme)
        {
            case TarodevController.PlayerInput.ControlScheme.KeyboardWASD:
                return Input.GetKeyDown(Player1CycleRightKey);

            case TarodevController.PlayerInput.ControlScheme.KeyboardArrows:
                return Input.GetKeyDown(Player2CycleRightKey);

            case TarodevController.PlayerInput.ControlScheme.KeyboardNumpad:
                return Input.GetKeyDown(NumpadCycleRightKey);

            case TarodevController.PlayerInput.ControlScheme.Gamepad:
#if ENABLE_INPUT_SYSTEM
                return Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame;
#else
                return false;
#endif

            case TarodevController.PlayerInput.ControlScheme.InputSystemActions:
            default:
                return Input.GetKeyDown(Player1CycleRightKey);
        }
    }

    private void RebuildOwnershipLists()
    {
        _player1OwnedIndices.Clear();
        _player2OwnedIndices.Clear();
        _allValidIndices.Clear();
        _singlePlayerCycleIndices.Clear();

        for (int i = 0; i < platformGroups.Count; i++)
        {
            PlatformColorGroup group = platformGroups[i];
            if (group == null || group.platformParent == null)
                continue;

            _allValidIndices.Add(i);
            _singlePlayerCycleIndices.Add(i);

            switch (group.controlledBy)
            {
                case PlayerOwner.Player1:
                    _player1OwnedIndices.Add(i);
                    break;

                case PlayerOwner.Player2:
                    _player2OwnedIndices.Add(i);
                    break;
            }
        }
    }

    private int GetStartupIndex(PlayerOwner owner, bool shouldStartActive)
    {
        if (!shouldStartActive)
            return -1;

        List<int> owned = GetOwnedList(owner);
        if (owned == null || owned.Count == 0)
            return -1;

        return owned[0];
    }

    private int GetStartupIndexForSinglePlayer(bool shouldStartActive)
    {
        if (!shouldStartActive)
            return -1;

        if (_singlePlayerCycleIndices.Count == 0)
            return -1;

        return _singlePlayerCycleIndices[0];
    }

    private List<int> GetOwnedList(PlayerOwner owner)
    {
        return owner switch
        {
            PlayerOwner.Player1 => _player1OwnedIndices,
            PlayerOwner.Player2 => _player2OwnedIndices,
            _ => null
        };
    }

    private int GetActiveIndex(PlayerOwner owner)
    {
        return owner switch
        {
            PlayerOwner.Player1 => _player1ActiveGroupIndex,
            PlayerOwner.Player2 => _player2ActiveGroupIndex,
            _ => -1
        };
    }

    private void SetActiveIndex(PlayerOwner owner, int groupIndex)
    {
        switch (owner)
        {
            case PlayerOwner.Player1:
                _player1ActiveGroupIndex = groupIndex;
                break;
            case PlayerOwner.Player2:
                _player2ActiveGroupIndex = groupIndex;
                break;
        }
    }

    public void CycleSinglePlayer(int direction)
    {
        if (_singlePlayerCycleIndices.Count == 0)
            return;

        if (_singlePlayerActiveGroupIndex == -1)
        {
            int nextIndex = direction >= 0 ? 0 : _singlePlayerCycleIndices.Count - 1;
            _singlePlayerActiveGroupIndex = _singlePlayerCycleIndices[nextIndex];
            _lastVisualGroupIndex = _singlePlayerActiveGroupIndex;
            RefreshAllGroups();
            RefreshSharedColorVisuals();
            return;
        }

        int currentIndex = _singlePlayerCycleIndices.IndexOf(_singlePlayerActiveGroupIndex);

        if (currentIndex < 0)
        {
            int fallbackIndex = direction >= 0 ? 0 : _singlePlayerCycleIndices.Count - 1;
            _singlePlayerActiveGroupIndex = _singlePlayerCycleIndices[fallbackIndex];
            _lastVisualGroupIndex = _singlePlayerActiveGroupIndex;
            RefreshAllGroups();
            RefreshSharedColorVisuals();
            return;
        }

        int newIndex = currentIndex + (direction >= 0 ? 1 : -1);

        if (newIndex >= _singlePlayerCycleIndices.Count || newIndex < 0)
            _singlePlayerActiveGroupIndex = -1;
        else
            _singlePlayerActiveGroupIndex = _singlePlayerCycleIndices[newIndex];

        _lastVisualGroupIndex = _singlePlayerActiveGroupIndex;
        RefreshAllGroups();
        RefreshSharedColorVisuals();
    }

    public void CyclePlayer(PlayerOwner owner, int direction)
    {
        List<int> owned = GetOwnedList(owner);
        if (owned == null || owned.Count == 0)
            return;

        int currentFullIndex = GetActiveIndex(owner);

        if (currentFullIndex == -1)
        {
            int nextIndex = direction >= 0 ? 0 : owned.Count - 1;
            SetActiveIndex(owner, owned[nextIndex]);
            _lastVisualGroupIndex = owned[nextIndex];
            RefreshAllGroups();
            RefreshSharedColorVisuals();
            return;
        }

        int currentOwnedListIndex = owned.IndexOf(currentFullIndex);

        if (currentOwnedListIndex < 0)
        {
            int fallbackIndex = direction >= 0 ? 0 : owned.Count - 1;
            SetActiveIndex(owner, owned[fallbackIndex]);
            _lastVisualGroupIndex = owned[fallbackIndex];
            RefreshAllGroups();
            RefreshSharedColorVisuals();
            return;
        }

        int newOwnedListIndex = currentOwnedListIndex + (direction >= 0 ? 1 : -1);

        if (newOwnedListIndex >= owned.Count || newOwnedListIndex < 0)
        {
            SetActiveIndex(owner, -1);
            _lastVisualGroupIndex = GetAnyRemainingActiveGroupIndex();
        }
        else
        {
            SetActiveIndex(owner, owned[newOwnedListIndex]);
            _lastVisualGroupIndex = owned[newOwnedListIndex];
        }

        RefreshAllGroups();
        RefreshSharedColorVisuals();
    }

    public void RefreshAllGroups()
    {
        for (int i = 0; i < platformGroups.Count; i++)
        {
            PlatformColorGroup group = platformGroups[i];
            if (group == null || group.platformParent == null)
                continue;

            bool isActive;

            if (IsSinglePlayer)
                isActive = i == _singlePlayerActiveGroupIndex;
            else
                isActive = i == _player1ActiveGroupIndex || i == _player2ActiveGroupIndex;

            ApplyGroupState(group.platformParent, isActive);
        }
    }

    private void ApplyGroupState(GameObject parent, bool isActive)
    {
        float targetAlpha = isActive ? activeAlpha : inactiveAlpha;

        SpriteRenderer[] renderers = parent.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;

            Color c = sr.color;
            c.a = targetAlpha;
            sr.color = c;
        }

        Collider2D[] colliders = parent.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in colliders)
        {
            if (col == null) continue;
            col.enabled = isActive;
        }
    }

    private int GetCurrentSharedVisualGroupIndex()
    {
        if (IsSinglePlayer)
            return _singlePlayerActiveGroupIndex;

        if (_lastVisualGroupIndex != -1)
            return _lastVisualGroupIndex;

        if (_player1ActiveGroupIndex != -1)
            return _player1ActiveGroupIndex;

        if (_player2ActiveGroupIndex != -1)
            return _player2ActiveGroupIndex;

        return -1;
    }

    private int GetAnyRemainingActiveGroupIndex()
    {
        if (IsSinglePlayer)
            return _singlePlayerActiveGroupIndex;

        if (_player1ActiveGroupIndex != -1)
            return _player1ActiveGroupIndex;

        if (_player2ActiveGroupIndex != -1)
            return _player2ActiveGroupIndex;

        return -1;
    }

    private void RefreshSharedColorVisuals()
    {
        Color targetColor = CurrentSharedColor;

        for (int i = 0; i < backgroundRenderers.Count; i++)
        {
            if (backgroundRenderers[i] == null)
                continue;

            backgroundRenderers[i].color = targetColor;
        }

        OnSharedActiveColorChanged?.Invoke(targetColor);
    }

    public bool IsGroupActive(GameObject groupObject)
    {
        if (groupObject == null)
            return false;

        for (int i = 0; i < platformGroups.Count; i++)
        {
            PlatformColorGroup group = platformGroups[i];
            if (group == null || group.platformParent == null)
                continue;

            if (group.platformParent == groupObject)
            {
                if (IsSinglePlayer)
                    return i == _singlePlayerActiveGroupIndex;

                return i == _player1ActiveGroupIndex || i == _player2ActiveGroupIndex;
            }
        }

        return false;
    }

    public void ForceRefresh()
    {
        RebuildOwnershipLists();

        if (IsSinglePlayer)
        {
            if (_singlePlayerActiveGroupIndex != -1 && !_singlePlayerCycleIndices.Contains(_singlePlayerActiveGroupIndex))
                _singlePlayerActiveGroupIndex = -1;

            _lastVisualGroupIndex = _singlePlayerActiveGroupIndex;
        }
        else
        {
            if (_player1ActiveGroupIndex != -1 && !_player1OwnedIndices.Contains(_player1ActiveGroupIndex))
                _player1ActiveGroupIndex = -1;

            if (_player2ActiveGroupIndex != -1 && !_player2OwnedIndices.Contains(_player2ActiveGroupIndex))
                _player2ActiveGroupIndex = -1;

            _lastVisualGroupIndex = GetAnyRemainingActiveGroupIndex();
        }

        RefreshAllGroups();
        RefreshSharedColorVisuals();
    }

    private void OnValidate()
    {
        inactiveAlpha = Mathf.Clamp01(inactiveAlpha);
        activeAlpha = Mathf.Clamp01(activeAlpha);
    }
}