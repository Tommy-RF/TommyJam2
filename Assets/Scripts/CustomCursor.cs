using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomCursor : NetworkBehaviour
{
    void Update()
    {
        if(isOwner)
        {
            Cursor.visible = false;
            Vector3 pos = Input.mousePosition;
            pos.z = 4f;
            transform.position = Camera.main.ScreenToWorldPoint(pos);
        }
        //Vector3 mousePos = Mouse.current.position.ReadValue();
        //mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        //mousePos.z = 0;
        //transform.position = mousePos;

    }
}
