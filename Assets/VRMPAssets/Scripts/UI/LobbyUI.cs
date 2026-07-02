using UnityEngine;
using TMPro;

namespace XRMultiplayer
{
    public class LobbyUI : MonoBehaviour
    {
        const string k_SessionName = "HandovAR-Session";
        const int k_PlayerCount = 2;

        [Header("Status Display")]
        [SerializeField] TMP_Text m_StatusText;

        void Start()
        {
            #if UNITY_EDITOR && HAS_PARRELSYNC
            if (ParrelSync.ClonesManager.IsClone())
            {
                XRINetworkGameManager.LocalPlayerName.Value = "Incoming";
                XRINetworkGameManager.LocalPlayerColor.Value = new Color(0.11f, 0.62f, 0.46f); // teal
            }
            else
            {
                XRINetworkGameManager.LocalPlayerName.Value = "Outgoing";
                XRINetworkGameManager.LocalPlayerColor.Value = new Color(0.85f, 0.35f, 0.19f); // coral
            }
            #else
            XRINetworkGameManager.LocalPlayerName.Value = "Nurse";
            XRINetworkGameManager.LocalPlayerColor.Value = new Color(0.5f, 0.3f, 0.8f); // purple
            #endif

            XRINetworkGameManager.Instance.OnConnectionFailedAction += OnConnectionFailed;
            XRINetworkGameManager.Instance.OnConnectionUpdated += OnConnectionUpdated;
            XRINetworkGameManager.Connected.Subscribe(OnConnected);

            SetStatus("Authenticating…");

            XRINetworkGameManager.CurrentConnectionState.Subscribe(OnConnectionStateChanged);

            if (XRINetworkGameManager.CurrentConnectionState.Value ==
                XRINetworkGameManager.ConnectionState.Authenticated)
            {
                OnConnectionStateChanged(XRINetworkGameManager.ConnectionState.Authenticated);
            }
        }

        void OnDestroy()
        {
            XRINetworkGameManager.Instance.OnConnectionFailedAction -= OnConnectionFailed;
            XRINetworkGameManager.Instance.OnConnectionUpdated -= OnConnectionUpdated;
            XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
            XRINetworkGameManager.CurrentConnectionState.Unsubscribe(OnConnectionStateChanged);
        }

        void OnConnectionStateChanged(XRINetworkGameManager.ConnectionState state)
        {
            if (state == XRINetworkGameManager.ConnectionState.Authenticated)
            {
                // Unsubscribe immediately — we only want to trigger once
                XRINetworkGameManager.CurrentConnectionState.Unsubscribe(OnConnectionStateChanged);
                SetStatus("Connecting…");
                XRINetworkGameManager.Instance.QuickJoinLobby();
            }
        }

        void OnConnectionUpdated(string update) => SetStatus(update);

        void OnConnected(bool connected)
        {
            if (connected)
            {
                SetStatus("Connected.");
                XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
            }
        }

        void OnConnectionFailed(string reason)
        {
            SetStatus("No session found, creating…");
            XRINetworkGameManager.Instance.CreateNewLobby(k_SessionName, isPrivate: true, k_PlayerCount);
        }

        void SetStatus(string msg)
        {
            if (m_StatusText != null) m_StatusText.text = msg;
            Debug.Log($"[LobbyUI] {msg}");
        }
    }
}