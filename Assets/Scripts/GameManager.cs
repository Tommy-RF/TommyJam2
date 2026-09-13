// Handles the server-side game state and works with the PlayerNetwork script to manage player readiness.

using PurrNet;
using PurrNet.Transports;
using UnityEngine;

public class GameManager : NetworkBehaviour
{

    public GameObject ExampleObject;
    public GameObject ReadyToPlayObject;

    public int PlayerCount;

    // The countdown is triggered when all players are ready (in the "Ready" area).
    private float countdownTime = 3f;
    private float countdownTimer;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        countdownTimer = countdownTime;
    }

    public void CheckPlayers()
    {
        foreach(var player in PlayerNetwork.allPlayers)
        {
            Debug.Log($"Player added: {player.Value.id}.");
        }
    }

    [ServerRpc(Channel.Unreliable)]
    public void FixedUpdate()
    {
        // Check if all players are ready. If they are, start the countdown. Otherwise, reset the countdown.
        var playerReadyCount = 0;
        foreach(var player in PlayerNetwork.allPlayers)
        {
            if(player.Value.isReady)
                {
                    playerReadyCount++;
                }

            if(playerReadyCount == PlayerNetwork.allPlayers.Count)
            {
                countdownTimer -= Time.deltaTime;
                if (countdownTimer <= 0)
                {
                    Debug.Log("Countdown finished!");
                    /* Inset transition logic here. */
                }
            }
            else
            {
                countdownTimer = countdownTime;
            }
        }
    }

    // We will need to use Target RPCs to tell clients that their turn is here or not... I think.
    /*
    [ObserversRpc]
    public void change_state(string state, int TurnPlayer)
    {
        if (state == "hide")
        {
            ExampleObject.SetActive(false);
        }

        if (state == "show")
        {
            ExampleObject.SetActive(true);
        }
    }

    public void CallStateShow()
    {
        change_state("show", 1);
    }

    public void CallStateHide()
    {
        change_state("hide", 1);
    }
    */
}
