using PurrNet;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextSaver : NetworkBehaviour
{
    public TMP_InputField inputField;
    public string ClientString;
    public SyncList<string> myList = new(true);

    protected override void OnSpawned()
    {
        //Subscribing to changes made to the list
        myList.onChanged += OnListChanged;
    }
    protected override void OnDespawned()
    {
        myList.onChanged -= OnListChanged;
    }

    private void OnListChanged(SyncListChange<string> change)
    {
        //This is called for everyone when the list changes.
        //It will log out the value, index and operation
        Debug.Log($"List updated: {change}");
    }


    private void ChangeMyList()
    {
        //This will change or add a value
        myList[0] = "Example";

        //This will remove the value
        myList.Remove("Example");

        //This will remove the entry at the given index
        myList.RemoveAt(0);

        //This will clear the list
        myList.Clear();

        //This will insert a value at the given index
        myList.Insert(1, "420f");

        //This will mark the index as dirty
        myList.SetDirty(0);
    }


    public void ClientTextSubmit()
    {
        if (!isServer)
        {
            string ClientString = inputField.text;
        }
        SubmitText();
    }

    [ServerRpc]
    public void SubmitText()
    {
        if (isServer)
        {
            string ClientString = inputField.text;
            myList.Add(ClientString);
        }

        if (!isServer)
        {
            myList.Add(ClientString);
        }
    }


}

