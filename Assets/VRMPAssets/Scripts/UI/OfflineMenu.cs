using UnityEngine;

namespace XRMultiplayer
{
    public class OfflineMenu : MonoBehaviour
    {
        // Assign in Inspector: "Outgoing Nurse" or "Incoming Nurse"
        // based on which headset this build is for, or leave as default
        [SerializeField] string m_PlayerName = "Nurse";
        [SerializeField] Color[] m_PlayerColors;

        void Awake()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.LocalPlayerName.Value = m_PlayerName;
            XRINetworkGameManager.LocalPlayerColor.Value =
                m_PlayerColors.Length > 0
                    ? m_PlayerColors[Random.Range(0, m_PlayerColors.Length)]
                    : Color.white;
        }

        void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
        }

        void OnConnected(bool connected)
        {
            gameObject.SetActive(!connected);
        }
    }
}