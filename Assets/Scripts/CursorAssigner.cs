using PurrNet;
using TMPro;
using UnityEngine;

public class CursorAssigner : NetworkBehaviour
{
    public TMP_Text PlayerNumber;
    public Color P1;
    public Color P2;
    public Color P3;
    public Color P4;
    protected override void OnSpawned()
    {
        var colors = new[] { P1, P2, P3, P4 };
        int index = (int)(owner.Value.id % (ulong)colors.Length -1);
        GetComponent<SpriteRenderer>().color = colors[index];

        PlayerNumber.SetText($"{owner.Value.id}");
    }

}
