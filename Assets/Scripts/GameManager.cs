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

    //Coco, this Observer's RPC is what is run to make sure everyone gets the same thing.
    //We will need to use Target RPCs to tell clients that their turn is here or not... I think.
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

        CountPlayers();

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
