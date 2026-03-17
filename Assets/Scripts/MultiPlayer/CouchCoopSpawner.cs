using UnityEngine;
using TarodevController;

public class CouchCoopSpawner : MonoBehaviour
{
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

    private void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("CouchCoopSpawner: playerPrefab not set.");
            return;
        }

        Vector3 p1Pos = player1Spawn != null ? player1Spawn.position : Vector3.zero;
        Vector3 p2Pos = player2Spawn != null ? player2Spawn.position : (Vector3.right * 2f);

        GameObject p1 = Instantiate(playerPrefab, p1Pos, Quaternion.identity);
        GameObject p2 = Instantiate(playerPrefab, p2Pos, Quaternion.identity);

        p1.name = "Player1";
        p2.name = "Player2";

        var input1 = p1.GetComponent<PlayerInput>();
        if (input1 != null) input1.SetScheme(PlayerInput.ControlScheme.KeyboardWASD);

        var input2 = p2.GetComponent<PlayerInput>();
        if (input2 != null) input2.SetScheme(PlayerInput.ControlScheme.KeyboardArrows);

        PlayerLabelFactory.CreateLabel(p1.transform, "Player 1");
        PlayerLabelFactory.CreateLabel(p2.transform, "Player 2");

        if (addTether)
        {
            var tether = gameObject.AddComponent<RopeTether2D>();
            tether.playerA = p1.transform;
            tether.playerB = p2.transform;
            tether.maxDistance = maxDistance;
            tether.pullStrength = pullStrength;
            tether.damping = damping;
            tether.ropeOffsetA = new Vector3(0f, 0.5f, 0f);
            tether.ropeOffsetB = new Vector3(0f, 0.5f, 0f);
        }
    }
}