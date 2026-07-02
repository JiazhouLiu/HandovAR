using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using TMPro;
using System;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace XRMultiplayer
{
    [DefaultExecutionOrder(100)]
    public class PlayerOptions : MonoBehaviour
    {
        [SerializeField] InputActionReference m_ToggleMenuAction;
        [SerializeField] AudioMixer m_Mixer;

        [Header("Panels")]
        [SerializeField] GameObject m_HostRoomPanel;
        [SerializeField] GameObject m_ClientRoomPanel;
        [SerializeField] GameObject[] m_OfflineWarningPanels;
        [SerializeField] GameObject[] m_OnlinePanels;
        [SerializeField] GameObject[] m_Panels;
        [SerializeField] Toggle[] m_PanelToggles;

        [Header("Text Components")]
        [SerializeField] TMP_Text m_SnapTurnText;
        [SerializeField] TMP_Text m_RoomCodeText;
        [SerializeField] TMP_Text m_TimeText;
        [SerializeField] TMP_Text[] m_RoomNameText;
        [SerializeField] TMP_InputField m_RoomNameInputField;
        [SerializeField] TMP_Text[] m_PlayerCountText;

        [Header("Player Options")]
        [SerializeField] Vector2 m_MinMaxMoveSpeed = new Vector2(2.0f, 10.0f);
        [SerializeField] Vector2 m_MinMaxTurnAmount = new Vector2(15.0f, 180.0f);
        [SerializeField] float m_SnapTurnUpdateAmount = 15.0f;

        DynamicMoveProvider m_MoveProvider;
        SnapTurnProvider m_TurnProvider;
        UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController m_TunnelingVignetteController;

        private void Awake()
        {
            m_MoveProvider = FindFirstObjectByType<DynamicMoveProvider>();
            m_TurnProvider = FindFirstObjectByType<SnapTurnProvider>();
            m_TunnelingVignetteController = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController>();

            XRINetworkGameManager.Connected.Subscribe(ConnectOnline);
            XRINetworkGameManager.ConnectedRoomName.Subscribe(UpdateRoomName);
            XRINetworkGameManager.Instance.OnSessionOwnerPromoted += UpdateHostVisuals;

            ConnectOnline(false);

            if (m_ToggleMenuAction != null)
                m_ToggleMenuAction.action.performed += ctx => ToggleMenu();
            else
                Utils.Log("No toggle menu action assigned to OptionsPanel", 1);
        }

        private void UpdateHostVisuals(ulong newHostId)
        {
            m_HostRoomPanel.SetActive(NetworkManager.Singleton.LocalClientId == newHostId);
            m_ClientRoomPanel.SetActive(NetworkManager.Singleton.LocalClientId != newHostId);
        }

        void OnEnable()
        {
            TogglePanel(0);
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(ConnectOnline);
            XRINetworkGameManager.ConnectedRoomName.Unsubscribe(UpdateRoomName);
            XRINetworkGameManager.Instance.OnSessionOwnerPromoted -= UpdateHostVisuals;
        }

        private void Update()
        {
            m_TimeText.text = $"{DateTime.Now:h:mm}<size=4><voffset=1em>{DateTime.Now:tt}</size></voffset>";
        }

        void ConnectOnline(bool connected)
        {
            foreach (var go in m_OfflineWarningPanels)
                go.SetActive(!connected);

            foreach (var go in m_OnlinePanels)
                go.SetActive(connected);

            if (connected)
            {
                if (XRINetworkPlayer.LocalPlayer != null)
                {
                    m_HostRoomPanel.SetActive(XRINetworkPlayer.LocalPlayer.IsSessionOwner);
                    m_ClientRoomPanel.SetActive(!XRINetworkPlayer.LocalPlayer.IsSessionOwner);
                }
                UpdateRoomName(XRINetworkGameManager.ConnectedRoomName.Value);
            }
            else
            {
                ToggleMenu(false);
            }
        }
        
        public void TogglePanel(int panelID)
        {
            for (int i = 0; i < m_Panels.Length; i++)
            {
                m_PanelToggles[i].SetIsOnWithoutNotify(panelID == i);
                m_Panels[i].SetActive(i == panelID);
            }
        }

        public void ToggleMenu(bool overrideToggle = false, bool overrideValue = false)
        {
            if (overrideToggle)
                gameObject.SetActive(overrideValue);
            else
                ToggleMenu();
            TogglePanel(0);
        }

        public void ToggleMenu()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void LogOut()
        {
            XRINetworkGameManager.Instance.Disconnect();
        }

        public void QuickJoin()
        {
            XRINetworkGameManager.Instance.QuickJoinLobby();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void SetVolumeLevel(float sliderValue)
        {
            m_Mixer.SetFloat("MainVolume", Mathf.Log10(sliderValue) * 20);
        }

        // Room Options
        public void UpdateRoomPrivacy(bool toggle)
        {
            XRINetworkGameManager.Instance.sessionManager.UpdateRoomPrivacy(toggle);
        }

        public void SubmitNewRoomName(string text)
        {
            XRINetworkGameManager.Instance.sessionManager.UpdateLobbyName(text);
        }

        void UpdateRoomName(string newValue)
        {
            m_RoomCodeText.text = $"Room Code: {XRINetworkGameManager.ConnectedRoomCode}";
            foreach (var t in m_RoomNameText)
                t.text = XRINetworkGameManager.ConnectedRoomName.Value;
            m_RoomNameInputField.text = XRINetworkGameManager.ConnectedRoomName.Value;
        }

        // Player Options
        public void SetHandOrientation(bool toggle)
        {
            if (toggle)
                m_MoveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HandRelative;
        }

        public void SetHeadOrientation(bool toggle)
        {
            if (toggle)
                m_MoveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HeadRelative;
        }

        public void SetMoveSpeed(float speedPercent)
        {
            m_MoveProvider.moveSpeed = Mathf.Lerp(m_MinMaxMoveSpeed.x, m_MinMaxMoveSpeed.y, speedPercent);
        }

        public void UpdateSnapTurn(int dir)
        {
            float newTurnAmount = Mathf.Clamp(
                m_TurnProvider.turnAmount + (m_SnapTurnUpdateAmount * dir),
                m_MinMaxTurnAmount.x,
                m_MinMaxTurnAmount.y);
            m_TurnProvider.turnAmount = newTurnAmount;
            m_SnapTurnText.text = $"{newTurnAmount}°";
        }

        public void ToggleTunnelingVignette(bool toggle)
        {
            m_TunnelingVignetteController.gameObject.SetActive(toggle);
        }

        public void ToggleFlight(bool toggle)
        {
            var gravityProvider = m_MoveProvider.GetComponent<GravityProvider>();
            if (gravityProvider != null)
                gravityProvider.enabled = !toggle;
            m_MoveProvider.enableFly = toggle;
        }
    }
}