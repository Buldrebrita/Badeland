using Badeland.Player;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Badeland.Networking
{
    /// <summary>
    /// Throwaway on-screen buttons for the network test: Host, Join and Disconnect, plus a status line.
    /// Replaced by a real lobby (Steam) later.
    /// </summary>
    public class NetworkMenu : MonoBehaviour
    {
        public string address = "127.0.0.1";
        public ushort port = 7777;

        GUIStyle _label;
        GUIStyle _button;

        void OnGUI()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;

            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                _label.normal.textColor = Color.white;
                _button = new GUIStyle(GUI.skin.button) { fontSize = 22 };
            }

            float x = Screen.width - 380f;

            if (!nm.IsClient && !nm.IsServer)
            {
                GUI.Label(new Rect(x, 15, 360, 34), "Online test", _label);
                address = GUI.TextField(new Rect(x, 55, 360, 34), address, _button);

                var transport = nm.GetComponent<UnityTransport>();

                if (GUI.Button(new Rect(x, 100, 175, 44), "Host", _button))
                {
                    // Listen on all addresses so friends on the same network can join too.
                    transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
                    nm.StartHost();
                }

                if (GUI.Button(new Rect(x + 185, 100, 175, 44), "Join", _button))
                {
                    transport.SetConnectionData(address, port);
                    nm.StartClient();
                }
                return;
            }

            string role = nm.IsHost ? "Hosting" : "Connected";
            GUI.Label(new Rect(x, 15, 360, 34), role + "  (" + PlayerController.All.Count + " player" + (PlayerController.All.Count == 1 ? "" : "s") + ")", _label);

            if (GUI.Button(new Rect(x, 55, 360, 44), "Disconnect", _button))
                nm.Shutdown();
        }
    }
}
