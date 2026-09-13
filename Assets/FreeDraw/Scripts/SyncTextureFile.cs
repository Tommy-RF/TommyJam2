using PurrNet;
using UnityEngine;

public class SyncTextureFile : NetworkBehaviour
{
    [SerializeField] private SyncTextureAsset _paintableSurface;

    protected override void OnSpawned()
    {
        Debug.Log("I'm Enabled!");
        _paintableSurface.onDataChanged += OnAvatarChanged;
    }

    protected override void OnDespawned()
    {
        _paintableSurface.onDataChanged -= OnAvatarChanged;
    }

    public void OnAvatarChanged(Texture2D texture)
    {
        Debug.Log("Texture Changed!");
        _paintableSurface.assetToSync = texture;
    }



}

