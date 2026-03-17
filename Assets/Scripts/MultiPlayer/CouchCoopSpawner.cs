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

    private void SpawnPlayers()
    {
        Player1Instance = Instantiate(playerPrefab, Player1SpawnPosition, Quaternion.identity);
        Player2Instance = Instantiate(playerPrefab, Player2SpawnPosition, Quaternion.identity);

        Player1Instance.name = "Player1";
        Player2Instance.name = "Player2";

        var input1 = Player1Instance.GetComponent<PlayerInput>();
        if (input1 != null) input1.SetScheme(PlayerInput.ControlScheme.KeyboardWASD);

        var input2 = Player2Instance.GetComponent<PlayerInput>();
        if (input2 != null) input2.SetScheme(PlayerInput.ControlScheme.KeyboardArrows);

        PlayerLabelFactory.CreateLabel(Player1Instance.transform, "Player 1");
        PlayerLabelFactory.CreateLabel(Player2Instance.transform, "Player 2");

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