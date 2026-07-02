using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    public class DashboardTextWidget : NetworkBehaviour
    {
        [Header("Text fields")]
        [SerializeField] TMP_Text m_TitleText;
        [SerializeField] TMP_Text m_BodyText;

        public string WidgetId { get; private set; }
        public string WidgetType { get; private set; }

        readonly NetworkVariable<FixedString128Bytes> m_Title =
            new NetworkVariable<FixedString128Bytes>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner
            );

        readonly NetworkVariable<FixedString4096Bytes> m_Body =
            new NetworkVariable<FixedString4096Bytes>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner
            );

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_Title.OnValueChanged += OnTitleChanged;
            m_Body.OnValueChanged += OnBodyChanged;

            ApplyText();
        }

        public override void OnNetworkDespawn()
        {
            m_Title.OnValueChanged -= OnTitleChanged;
            m_Body.OnValueChanged -= OnBodyChanged;

            base.OnNetworkDespawn();
        }

        public void Init(string widgetId, string widgetType, string label, string body)
        {
            WidgetId = widgetId;
            WidgetType = widgetType;

            string safeTitle = string.IsNullOrWhiteSpace(label)
                ? "Freeform Note"
                : label;

            string safeBody = string.IsNullOrWhiteSpace(body)
                ? ""
                : body;

            m_Title.Value = safeTitle;
            m_Body.Value = safeBody;

            ApplyText();

            gameObject.name = $"Widget_{widgetId}_{widgetType}";
        }

        void OnTitleChanged(FixedString128Bytes oldValue, FixedString128Bytes newValue)
        {
            ApplyText();
        }

        void OnBodyChanged(FixedString4096Bytes oldValue, FixedString4096Bytes newValue)
        {
            ApplyText();
        }

        void ApplyText()
        {
            if (m_TitleText != null)
            {
                string title = m_Title.Value.ToString();

                m_TitleText.text = string.IsNullOrWhiteSpace(title)
                    ? "Freeform Note"
                    : title;
            }

            if (m_BodyText != null)
            {
                m_BodyText.text = m_Body.Value.ToString();
            }
        }
    }
}