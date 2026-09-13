using PurrNet;
using UnityEngine;

public class GameManager : NetworkBehaviour
{

    public GameObject ExampleObject;
    public GameObject ReadyToPlayObject;

    public int PlayerCount;


    protected override void OnSpawned()
    {
        base.OnSpawned();
        CountPlayers();
        
    }

    public void CountPlayers()
    {
        PlayerCount = networkManager.playerCount;
        
    }

    public void ArePlayersReady()
    {
        CountPlayers();



    }


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

}
