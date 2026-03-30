using System.Collections.Generic;
using UnityEngine;
using TarodevController;

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

    [Header("Visual Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float activeAlpha = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float inactiveAlpha = 0.2f;

    [Header("Startup")]
    [SerializeField] private bool startWithPlayer1ColorActive = false;
    [SerializeField] private bool startWithPlayer2ColorActive = false;

    // Full list indices. -1 means no active color for that player.
    private int _player1ActiveGroupIndex = -1;
    private int _player2ActiveGroupIndex = -1;

    private readonly List<int> _player1OwnedIndices = new();
    private readonly List<int> _player2OwnedIndices = new();

    private void Awake()
    {
        RebuildOwnershipLists();

        _player1ActiveGroupIndex = GetStartupIndex(PlayerOwner.Player1, startWithPlayer1ColorActive);
        _player2ActiveGroupIndex = GetStartupIndex(PlayerOwner.Player2, startWithPlayer2ColorActive);

        RefreshAllGroups();
    }

    private void Update()
    {
        HandlePlayer1Input();
        HandlePlayer2Input();
    }

    private void HandlePlayer1Input()
    {
        GetKeysForPlayer(0, out KeyCode left, out KeyCode right);

        if (left != KeyCode.None && Input.GetKeyDown(left))
        {
            CyclePlayer(PlayerOwner.Player1, -1);
        }
        else if (right != KeyCode.None && Input.GetKeyDown(right))
        {
            CyclePlayer(PlayerOwner.Player1, +1);
        }
    }

    private void HandlePlayer2Input()
    {
        GetKeysForPlayer(1, out KeyCode left, out KeyCode right);

        if (left != KeyCode.None && Input.GetKeyDown(left))
        {
            CyclePlayer(PlayerOwner.Player2, -1);
        }
        else if (right != KeyCode.None && Input.GetKeyDown(right))
        {
            CyclePlayer(PlayerOwner.Player2, +1);
        }
    }

    private void GetKeysForPlayer(int playerIndex, out KeyCode left, out KeyCode right)
    {
        left = KeyCode.None;
        right = KeyCode.None;

        if (PlayerSetupManager.Instance == null) return;
        if (PlayerSetupManager.Instance.players == null) return;
        if (playerIndex < 0 || playerIndex >= PlayerSetupManager.Instance.players.Length) return;

        var data = PlayerSetupManager.Instance.players[playerIndex];

        switch (data.controlScheme)
        {
            case PlayerInput.ControlScheme.KeyboardWASD:
                left = KeyCode.Q;
                right = KeyCode.E;
                break;

            case PlayerInput.ControlScheme.KeyboardArrows:
                left = KeyCode.LeftBracket;
                right = KeyCode.RightBracket;
                break;

            default:
                left = KeyCode.None;
                right = KeyCode.None;
                break;
        }
    }

    private void RebuildOwnershipLists()
    {
        _player1OwnedIndices.Clear();
        _player2OwnedIndices.Clear();

        for (int i = 0; i < platformGroups.Count; i++)
        {
            PlatformColorGroup group = platformGroups[i];

            if (group == null || group.platformParent == null)
                continue;

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

    public void CyclePlayer(PlayerOwner owner, int direction)
    {
        List<int> owned = GetOwnedList(owner);
        if (owned == null || owned.Count == 0)
            return;

        int currentFullIndex = GetActiveIndex(owner);

        // If currently none selected, start at beginning/end depending on direction.
        if (currentFullIndex == -1)
        {
            int nextIndex = direction >= 0 ? 0 : owned.Count - 1;
            SetActiveIndex(owner, owned[nextIndex]);
            RefreshAllGroups();
            return;
        }

        int currentOwnedListIndex = owned.IndexOf(currentFullIndex);

        // Fallback if something changed in inspector or data got invalid.
        if (currentOwnedListIndex < 0)
        {
            int fallbackIndex = direction >= 0 ? 0 : owned.Count - 1;
            SetActiveIndex(owner, owned[fallbackIndex]);
            RefreshAllGroups();
            return;
        }

        int newOwnedListIndex = currentOwnedListIndex + (direction >= 0 ? 1 : -1);

        // Includes "none active" in the loop.
        if (newOwnedListIndex >= owned.Count || newOwnedListIndex < 0)
        {
            SetActiveIndex(owner, -1);
        }
        else
        {
            SetActiveIndex(owner, owned[newOwnedListIndex]);
        }

        RefreshAllGroups();
    }

    public void RefreshAllGroups()
    {
        for (int i = 0; i < platformGroups.Count; i++)
        {
            PlatformColorGroup group = platformGroups[i];
            if (group == null || group.platformParent == null)
                continue;

            bool isActive =
                i == _player1ActiveGroupIndex ||
                i == _player2ActiveGroupIndex;

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
                return i == _player1ActiveGroupIndex || i == _player2ActiveGroupIndex;
            }
        }

        return false;
    }

    public void ForceRefresh()
    {
        RebuildOwnershipLists();

        if (_player1ActiveGroupIndex != -1 && !_player1OwnedIndices.Contains(_player1ActiveGroupIndex))
            _player1ActiveGroupIndex = -1;

        if (_player2ActiveGroupIndex != -1 && !_player2OwnedIndices.Contains(_player2ActiveGroupIndex))
            _player2ActiveGroupIndex = -1;

        RefreshAllGroups();
    }

    private void OnValidate()
    {
        if (activeAlpha < 0f) activeAlpha = 0f;
        if (activeAlpha > 1f) activeAlpha = 1f;
        if (inactiveAlpha < 0f) inactiveAlpha = 0f;
        if (inactiveAlpha > 1f) inactiveAlpha = 1f;
    }
}