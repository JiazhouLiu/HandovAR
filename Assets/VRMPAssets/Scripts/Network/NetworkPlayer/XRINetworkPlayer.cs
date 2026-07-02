using UnityEngine;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using Unity.Collections;
using System;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine.XR.Templates.VRMultiplayer;

namespace XRMultiplayer
{
    public class XRINetworkPlayer : NetworkBehaviour
    {
        public static XRINetworkPlayer LocalPlayer;

        [Header("Avatar Transform References")]
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;

        public Action<string> onNameUpdated;
        public Action<Color> onColorUpdated;
        public Action onSpawnedLocal;
        public Action onSpawnedAll;
        public Action<XRINetworkPlayer> onDisconnected;

        public BindableVariable<bool> squelched = new(false);

        public float playerVoiceAmp => 0f;

        public string playerName => m_PlayerName.Value.ToString();
        readonly NetworkVariable<FixedString128Bytes> m_PlayerName = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public Color playerColor => m_PlayerColor.Value;
        readonly NetworkVariable<Color> m_PlayerColor = new(Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<int> platformType => m_PlatformType;
        readonly NetworkVariable<int> m_PlatformType = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [HideInInspector] public readonly NetworkVariable<bool> selfMuted = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [Header("Player Name Tag"), SerializeField] protected bool m_UpdateObjectName = true;

        [Header("Networked Hands"), SerializeField] protected GameObject[] m_handsObjects;

        [Header("Player Name Tag"), SerializeField] protected PlayerNameTag m_PlayerNameTag;

        protected Transform m_LeftHandOrigin, m_RightHandOrigin, m_HeadOrigin;
        protected XROrigin m_XROrigin;
        protected bool m_InitialConnected = false;

        protected virtual void OnEnable()
        {
            m_PlayerName.OnValueChanged += UpdatePlayerName;
            m_PlayerColor.OnValueChanged += UpdatePlayerColor;
        }

        protected virtual void OnDisable()
        {
            m_PlayerName.OnValueChanged -= UpdatePlayerName;
            m_PlayerColor.OnValueChanged -= UpdatePlayerColor;
        }

        protected virtual void LateUpdate()
        {
            if (!IsOwner) return;

            if (m_HeadOrigin != null)
                head.SetPositionAndRotation(m_HeadOrigin.position, m_HeadOrigin.rotation);

            if (m_LeftHandOrigin != null)
                leftHand.SetPositionAndRotation(m_LeftHandOrigin.position, m_LeftHandOrigin.rotation);

            if (m_RightHandOrigin != null)
                rightHand.SetPositionAndRotation(m_RightHandOrigin.position, m_RightHandOrigin.rotation);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (IsOwner)
            {
                XRINetworkGameManager.LocalPlayerName.Unsubscribe(UpdateLocalPlayerName);
                XRINetworkGameManager.LocalPlayerColor.Unsubscribe(UpdateLocalPlayerColor);
            }
            else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                XRINetworkGameManager.Instance.PlayerLeft(NetworkObject.OwnerClientId);
            }

            m_PlayerColor.OnValueChanged -= UpdatePlayerColor;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsLocalPlayer)
            {
                LocalPlayer = this;
                XRINetworkGameManager.Instance.OnLocalClientStarted(NetworkObject.OwnerClientId);

                m_PlatformType.Value = (int)XRPlatformUnderstanding.CurrentPlatform;

                m_XROrigin = FindFirstObjectByType<XROrigin>();
                if (m_XROrigin != null)
                    m_HeadOrigin = m_XROrigin.Camera.transform;
                else
                    Utils.Log("No XR Rig Available", 1);

                SetupLocalPlayer();
            }
            CompleteSetup();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            PlayerHudNotification.Instance.ShowText($"<b>{m_PlayerName.Value}</b> left");
            onDisconnected?.Invoke(this);
        }

        public void SetHandOrigins(Transform left, Transform right)
        {
            m_LeftHandOrigin = left;
            m_RightHandOrigin = right;
        }

        protected virtual void SetupLocalPlayer()
        {
            foreach (var hand in m_handsObjects)
                hand.SetActive(false);

            m_PlayerColor.Value = XRINetworkGameManager.LocalPlayerColor.Value;
            m_PlayerName.Value = new FixedString128Bytes(XRINetworkGameManager.LocalPlayerName.Value);
            XRINetworkGameManager.LocalPlayerColor.Subscribe(UpdateLocalPlayerColor);
            XRINetworkGameManager.LocalPlayerName.Subscribe(UpdateLocalPlayerName);

            onSpawnedLocal?.Invoke();
        }

        protected virtual void UpdateLocalPlayerColor(Color color)
        {
            m_PlayerColor.Value = XRINetworkGameManager.LocalPlayerColor.Value;
        }

        protected virtual void UpdateLocalPlayerName(string name)
        {
            m_PlayerName.Value = new FixedString128Bytes(XRINetworkGameManager.LocalPlayerName.Value);
        }

        void CompleteSetup()
        {
            XRINetworkGameManager.Instance.PlayerJoined(NetworkObject.OwnerClientId);
            UpdatePlayerColor(Color.white, m_PlayerColor.Value);
            UpdatePlayerName(new FixedString128Bytes(""), m_PlayerName.Value);

            WorldCanvas worldCanvas = FindFirstObjectByType<WorldCanvas>();
            if (worldCanvas != null)
            {
                Canvas localCanvas = m_PlayerNameTag.GetComponentInParent<Canvas>();
                worldCanvas.SetupPlayerNameTag(this, m_PlayerNameTag);
                Destroy(localCanvas.gameObject);
            }
            else
            {
                m_PlayerNameTag.SetupNameTag(this);
            }

            onSpawnedAll?.Invoke();
        }

        void UpdatePlayerName(FixedString128Bytes oldName, FixedString128Bytes currentName)
        {
            onNameUpdated?.Invoke(currentName.ToString());

            if (!m_InitialConnected && !string.IsNullOrEmpty(currentName.ToString()))
            {
                m_InitialConnected = true;
                if (!IsLocalPlayer)
                    PlayerHudNotification.Instance.ShowText($"<b>{playerName}</b> joined");
            }

            if (m_UpdateObjectName)
                gameObject.name = currentName.ToString();
        }

        void UpdatePlayerColor(Color oldColor, Color newColor)
        {
            onColorUpdated?.Invoke(newColor);
        }
    }
}