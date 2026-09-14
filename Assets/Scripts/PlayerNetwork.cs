// Handles network stuff for each player.
// Communicates with the GameManager script to manage player readiness.
// isReady is set by the PlayerPhysics script when a player enters or leaves the "Ready" trigger area.

using PurrNet;
using UnityEngine;

public class PlayerNetwork : PlayerIdentity <PlayerNetwork>
{
    public bool isReady;
    [SerializeField] private GameManager GameManager;
}
