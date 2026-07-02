using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XRMultiplayer
{
    public class PlayerSlot : MonoBehaviour
    {
        public TMP_Text playerSlotName;
        public TMP_Text playerInitial;
        public Image playerIconImage;

        XRINetworkPlayer m_Player;
        internal ulong playerID = 0;

        public void Setup(XRINetworkPlayer player)
        {
            m_Player = player;
            m_Player.onColorUpdated += UpdateColor;
            m_Player.onNameUpdated += UpdateName;
        }

        void OnDestroy()
        {
            m_Player.onColorUpdated -= UpdateColor;
            m_Player.onNameUpdated -= UpdateName;
        }

        void UpdateColor(Color newColor)
        {
            playerIconImage.color = newColor;
        }

        void UpdateName(string newName)
        {
            if (string.IsNullOrEmpty(newName)) return;

            string displayName = newName;
            if (m_Player.IsLocalPlayer)
                displayName += " (You)";
            else if (m_Player.IsOwnedByServer)
                displayName += " (Host)";

            playerSlotName.text = displayName;
            playerInitial.text = newName.Substring(0, 1);
        }
    }
}