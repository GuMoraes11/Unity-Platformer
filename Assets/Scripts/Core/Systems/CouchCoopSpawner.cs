using UnityEngine;
using TarodevController;

public class CouchCoopSpawner : MonoBehaviour
{
    public static CouchCoopSpawner Instance { get; private set; }

    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Spawn Points")]
    public Transform player1Spawn;
    public Transform player2Spawn;

    [Header("Tether")]
    public bool addTether = true;
    public float maxDistance = 6.5f;
    public float pullStrength = 45f;
    public float damping = 6f;

    public GameObject Player1Instance { get; private set; }
    public GameObject Player2Instance { get; private set; }
    public RopeTether2D TetherInstance { get; private set; }

    public Vector3 Player1SpawnPosition => player1Spawn != null ? player1Spawn.position : Vector3.zero;
    public Vector3 Player2SpawnPosition => player2Spawn != null ? player2Spawn.position : Vector3.right * 2f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("CouchCoopSpawner: playerPrefab not set.");
            return;
        }

        SpawnPlayers();
    }


    private void SetupPlayer(GameObject player, PlayerSetupData data)
    {
        player.name = data.playerName;

        // INPUT
        var input = player.GetComponent<PlayerInput>();
        if (input != null)
            input.SetScheme(data.controlScheme);

        // LABEL
        PlayerLabelFactory.CreateLabel(player.transform, data.playerName);

        // SKIN
        var animator = player.GetComponentInChildren<TarodevController.PlayerAnimator>();
        if (animator != null)
        {
            var skins = GameSkinDatabase.Instance.skins;

            if (data.skinIndex >= 0 && data.skinIndex < skins.Length)
            {
                animator.SetSkin(skins[data.skinIndex]);
            }
        }
    }

    private void SpawnPlayers()
    {
        Player1Instance = Instantiate(playerPrefab, Player1SpawnPosition, Quaternion.identity);
        Player2Instance = Instantiate(playerPrefab, Player2SpawnPosition, Quaternion.identity);

        var players = PlayerSetupManager.Instance.players;

        SetupPlayer(Player1Instance, players[0]);
        SetupPlayer(Player2Instance, players[1]);

        if (addTether)
        {
            TetherInstance = gameObject.AddComponent<RopeTether2D>();
            TetherInstance.playerA = Player1Instance.transform;
            TetherInstance.playerB = Player2Instance.transform;
            TetherInstance.maxDistance = maxDistance;
            TetherInstance.pullStrength = pullStrength;
            TetherInstance.damping = damping;
            TetherInstance.ropeOffsetA = new Vector3(0f, 0.5f, 0f);
            TetherInstance.ropeOffsetB = new Vector3(0f, 0.5f, 0f);
        }
    }
}