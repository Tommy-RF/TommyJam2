// Handles network stuff for each player.
// Communicates with the GameManager script to manage player readiness.
// isReady is set by the PlayerPhysics script when a player enters or leaves the "Ready" trigger area.

using PurrNet;
using UnityEngine;
using PurrNet.Transports;

public class PlayerNetwork : PlayerIdentity <PlayerNetwork>
{
    public bool isReady;
    public int score;
    [SerializeField] public int ballPosition;
    SyncVar<int> ballPositionSyncVar = new SyncVar<int>(0);

    [SerializeField] private GameManager GameManager;

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
