using Unity.XR.CoreUtils;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;

namespace XRMultiplayer
{
    public class OfflinePlayerAvatar : MonoBehaviour
    {
        public static BindableVariable<float> voiceAmp = new BindableVariable<float>();

        [SerializeField] Transform m_HeadTransform;
        [SerializeField] SkinnedMeshRenderer m_HeadRend;

        Transform m_HeadOrigin;

        void Start()
        {
            XROrigin rig = FindFirstObjectByType<XROrigin>();
            m_HeadOrigin = rig.Camera.transform;
        }

        void OnEnable()
        {
            XRINetworkGameManager.LocalPlayerColor.Subscribe(UpdatePlayerColor);
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
        }

        void OnDisable()
        {
            XRINetworkGameManager.LocalPlayerColor.Unsubscribe(UpdatePlayerColor);
            XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
        }

        void OnConnected(bool connected)
        {
            gameObject.SetActive(!connected);
        }

        void LateUpdate()
        {
            m_HeadTransform.SetPositionAndRotation(m_HeadOrigin.position, m_HeadOrigin.rotation);
        }

        void UpdatePlayerColor(Color color)
        {
            m_HeadRend.materials[2].color = color;
        }
    }
}