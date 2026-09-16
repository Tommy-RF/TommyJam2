// Handles network stuff for each player.
// Communicates with the GameManager script to manage player readiness.
// isReady is set by the PlayerPhysics script when a player enters or leaves the "Ready" trigger area.

using PurrNet;
using UnityEngine;
using PurrNet.Transports;
using System;

public class PlayerNetwork : PlayerIdentity <PlayerNetwork>
{
    public bool isReady;
    public SyncVar<int> score = new SyncVar<int>(0);
    public int playerID;
    public int playerNumber;

    [SerializeField] public int ballPosition;
    SyncVar<int> ballPositionSyncVar = new SyncVar<int>(0);
    
    public int answerProximity;

    [SerializeField] private GameManager GameManager;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        playerID = Convert.ToInt32(owner.Value.id);
    }

    public void FixedUpdate()
    {
        _sendBallPositionToServer((int) this.transform.position.x);
        ballPosition = ballPositionSyncVar.value;

    }

    // Sends the player's ball position to the server, which then updates the SyncVar and broadcasts it to all clients.
    [ServerRpc(Channel.Unreliable)]
    private void _sendBallPositionToServer(int position)
    {
        ballPositionSyncVar.value = position;
    }
}
