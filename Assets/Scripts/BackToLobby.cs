using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToLobby : MonoBehaviour
{
    public void backToLobby()
    {
        {
            SceneManager.LoadScene(0);
        }
    }
}
