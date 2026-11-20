using Unity.Netcode;
using UnityEngine;

public class QuickNetUI : MonoBehaviour
{
    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            GUILayout.BeginArea(new Rect(20, 20, 220, 120));
            if (GUILayout.Button("Start Host (Игрок 1)", GUILayout.Height(40)))
            {
                NetworkManager.Singleton.StartHost();
            }
            if (GUILayout.Button("Start Client (Игрок 2)", GUILayout.Height(40)))
            {
                NetworkManager.Singleton.StartClient();
            }
            GUILayout.EndArea();
        }
    }
}