using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ColorManager : MonoBehaviour
{
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
    }

    [Header("Platform Groups")]
    [SerializeField] private List<PlatformColorGroup> platformGroups = new List<PlatformColorGroup>();

    [Header("Player 1 Keyboard Keys")]
    [SerializeField] private KeyCode player1CycleLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode player1CycleRightKey = KeyCode.E;

    [Header("Player 2 Keyboard Keys")]
    [SerializeField] private KeyCode player2CycleLeftKey = KeyCode.LeftBracket;
    [SerializeField] private KeyCode player2CycleRightKey = KeyCode.RightBracket;

    [Header("Numpad Color Keys")]
    [SerializeField] private KeyCode numpadCycleLeftKey = KeyCode.Keypad7;
    [SerializeField] private KeyCode numpadCycleRightKey = KeyCode.Keypad9;

    [Header("Visual Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float activeAlpha = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float inactiveAlpha = 0.2f;

    [Header("Startup")]
    [SerializeField] private bool startWithPlayer1ColorActive = false;
    [SerializeField] private bool startWithPlayer2ColorActive = false;

    private int _player1ActiveGroupIndex = -1;
    private int _player2ActiveGroupIndex = -1;
    private int _singlePlayerActiveGroupIndex = -1;

    private readonly List<int> _player1OwnedIndices = new();
    private readonly List<int> _player2OwnedIndices = new();
    private readonly List<int> _allValidIndices = new();

    private bool IsSinglePlayer =>
        PlayerSetupManager.Instance != null && PlayerSetupManager.Instance.IsSinglePlayer;

    private void Awake()
    {
        RebuildOwnershipLists();

        if (IsSinglePlayer)
        {
            _singlePlayerActiveGroupIndex = GetStartupIndexForSinglePlayer(startWithPlayer1ColorActive);
        }
        else
        {
            _player1ActiveGroupIndex = GetStartupIndex(PlayerOwner.Player1, startWithPlayer1ColorActive);
            _player2ActiveGroupIndex = GetStartupIndex(PlayerOwner.Player2, startWithPlayer2ColorActive);
        }

        RefreshAllGroups();
    }

    private void Update()
    {
        if (IsSinglePlayer)
        {
            HandleSinglePlayerInput();
            return;
        }

        HandlePlayerInput(0, PlayerOwner.Player1);

        // Only read player 2 input if player 2 actually exists
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
        Debug.Log($"Color Input -> PlayerIndex: {playerIndex}, Owner: {owner}, Scheme: {scheme}");

        if (GetCycleLeftPressedForScheme(scheme, playerIndex))
            CyclePlayer(owner, -1);
        else if (GetCycleRightPressedForScheme(scheme, playerIndex))
            CyclePlayer(owner, +1);
    }

    private TarodevController.PlayerInput.ControlScheme GetPlayerScheme(int playerIndex)
    {
        // Prefer the ACTUAL spawned player's input component
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

        // Fallback to setup data if needed
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
                return Input.GetKeyDown(player1CycleLeftKey);

            case TarodevController.PlayerInput.ControlScheme.KeyboardArrows:
                return Input.GetKeyDown(player2CycleLeftKey);

            case TarodevController.PlayerInput.ControlScheme.KeyboardNumpad:
                return Input.GetKeyDown(numpadCycleLeftKey);

            case TarodevController.PlayerInput.ControlScheme.Gamepad:
    #if ENABLE_INPUT_SYSTEM
                return Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame;
    #else
                return false;
    #endif

            case TarodevController.PlayerInput.ControlScheme.InputSystemActions:
            default:
                return Input.GetKeyDown(player1CycleLeftKey);
        }
    }

    private bool GetCycleRightPressedForScheme(TarodevController.PlayerInput.ControlScheme scheme, int playerIndex)
    {
        switch (scheme)
        {
            case TarodevController.PlayerInput.ControlScheme.KeyboardWASD:
                return Input.GetKeyDown(player1CycleRightKey);

            case TarodevController.PlayerInput.ControlScheme.KeyboardArrows:
                return Input.GetKeyDown(player2CycleRightKey);

            case TarodevController.PlayerInput.ControlScheme.KeyboardNumpad:
                return Input.GetKeyDown(numpadCycleRightKey);

            case TarodevController.PlayerInput.ControlScheme.Gamepad:
    #if ENABLE_INPUT_SYSTEM
                return Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame;
    #else
                return false;
    #endif

            case TarodevController.PlayerInput.ControlScheme.InputSystemActions:
            default:
                return Input.GetKeyDown(player1CycleRightKey);
        }
    }

    private void RebuildOwnershipLists()
    {
        _player1OwnedIndices.Clear();
        _player2OwnedIndices.Clear();
        _allValidIndices.Clear();

        for (int i = 0; i < platformGroups.Count; i++)
        {
            PlatformColorGroup group = platformGroups[i];
            if (group == null || group.platformParent == null)
                continue;

            _allValidIndices.Add(i);

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

        if (_allValidIndices.Count == 0)
            return -1;

        return _allValidIndices[0];
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
        if (_allValidIndices.Count == 0)
            return;

        if (_singlePlayerActiveGroupIndex == -1)
        {
            int nextIndex = direction >= 0 ? 0 : _allValidIndices.Count - 1;
            _singlePlayerActiveGroupIndex = _allValidIndices[nextIndex];
            RefreshAllGroups();
            return;
        }

        int currentIndexInAll = _allValidIndices.IndexOf(_singlePlayerActiveGroupIndex);

        if (currentIndexInAll < 0)
        {
            int fallbackIndex = direction >= 0 ? 0 : _allValidIndices.Count - 1;
            _singlePlayerActiveGroupIndex = _allValidIndices[fallbackIndex];
            RefreshAllGroups();
            return;
        }

        int newIndex = currentIndexInAll + (direction >= 0 ? 1 : -1);

        if (newIndex >= _allValidIndices.Count || newIndex < 0)
            _singlePlayerActiveGroupIndex = -1;
        else
            _singlePlayerActiveGroupIndex = _allValidIndices[newIndex];

        RefreshAllGroups();
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
            RefreshAllGroups();
            return;
        }

        int currentOwnedListIndex = owned.IndexOf(currentFullIndex);

        if (currentOwnedListIndex < 0)
        {
            int fallbackIndex = direction >= 0 ? 0 : owned.Count - 1;
            SetActiveIndex(owner, owned[fallbackIndex]);
            RefreshAllGroups();
            return;
        }

        int newOwnedListIndex = currentOwnedListIndex + (direction >= 0 ? 1 : -1);

        if (newOwnedListIndex >= owned.Count || newOwnedListIndex < 0)
            SetActiveIndex(owner, -1);
        else
            SetActiveIndex(owner, owned[newOwnedListIndex]);

        RefreshAllGroups();
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
            if (_singlePlayerActiveGroupIndex != -1 && !_allValidIndices.Contains(_singlePlayerActiveGroupIndex))
                _singlePlayerActiveGroupIndex = -1;
        }
        else
        {
            if (_player1ActiveGroupIndex != -1 && !_player1OwnedIndices.Contains(_player1ActiveGroupIndex))
                _player1ActiveGroupIndex = -1;

            if (_player2ActiveGroupIndex != -1 && !_player2OwnedIndices.Contains(_player2ActiveGroupIndex))
                _player2ActiveGroupIndex = -1;
        }

        RefreshAllGroups();
    }

    private void OnValidate()
    {
        inactiveAlpha = Mathf.Clamp01(inactiveAlpha);
        activeAlpha = Mathf.Clamp01(activeAlpha);
    }
}