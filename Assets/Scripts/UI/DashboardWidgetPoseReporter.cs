using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    public class DashboardWidgetPoseReporter : NetworkBehaviour
    {
        [Header("Reporting")]
        [SerializeField] float m_SendInterval = 0.1f;
        [SerializeField] float m_MinMoveDistance = 0.03f;

        string m_WidgetId;
        FacilitatorClient m_Client;

        Vector3 m_LastSentPosition;
        float m_NextSendTime;

        public string WidgetId => m_WidgetId;

        public void Init(string widgetId, FacilitatorClient client)
        {
            m_WidgetId = widgetId;
            m_Client = client;
            m_LastSentPosition = transform.position;
        }

        void Update()
        {
            if (string.IsNullOrEmpty(m_WidgetId)) return;
            if (m_Client == null) return;
            if (Time.time < m_NextSendTime) return;

            // Only the owner should report movement, otherwise both clients may spam
            // slightly different poses for the same widget.
            if (IsSpawned && !IsOwner) return;

            Vector3 currentPosition = transform.position;

            if (Vector3.Distance(currentPosition, m_LastSentPosition) < m_MinMoveDistance)
            {
                return;
            }

            m_NextSendTime = Time.time + m_SendInterval;
            m_LastSentPosition = currentPosition;

            m_Client.SendWidgetPose(m_WidgetId, currentPosition);
        }
    }
}