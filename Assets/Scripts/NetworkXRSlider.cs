using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;

namespace XRMultiplayer
{
    [RequireComponent(typeof(XRSlider))]
    public class NetworkXRSlider : NetworkBehaviour
    {
        // Sync float 0-1
        private NetworkVariable<float> netValue = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private XRSlider slider;

        private void Awake()
        {
            slider = GetComponent<XRSlider>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                netValue.Value = slider.Value;
            }
            else
            {
                slider.Value = netValue.Value;
            }

            slider.onValueChange.AddListener(OnLocalSlide);
            netValue.OnValueChanged += OnNetValueChanged;
        }

        public override void OnNetworkDespawn()
        {
            slider.onValueChange.RemoveListener(OnLocalSlide);
            netValue.OnValueChanged -= OnNetValueChanged;
        }

        // UI -> Network
        private void OnLocalSlide(float val)
        {
            if (IsOwner)
            {
                netValue.Value = val;
            }
            else
            {
                RequestSlideServerRpc(val);
            }
        }

        // Network -> UI
        private void OnNetValueChanged(float prev, float current)
        {
            // Avoid feedback loop if we are the one dragging it
            if (Mathf.Abs(slider.Value - current) > Mathf.Epsilon)
            {
                slider.onValueChange.RemoveListener(OnLocalSlide);
                slider.Value = current;
                slider.onValueChange.AddListener(OnLocalSlide);
            }
        }

        [Rpc(SendTo.Server)]
        void RequestSlideServerRpc(float val)
        {
            netValue.Value = val;
        }
    }
}