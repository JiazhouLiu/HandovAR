using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using NativeWebSocket;

namespace XRMultiplayer
{
    public class FacilitatorClient : MonoBehaviour
    {
        public enum NurseRole
        {
            Outgoing,
            Incoming
        }

        [Header("WebSocket")]
        [SerializeField] string m_ServerUrl = "ws://localhost:8080";
        [SerializeField] float m_ReconnectDelay = 3f;

        [Header("Local nurse tracking")]
        [SerializeField] Transform m_LocalNurse;
        [SerializeField] NurseRole m_LocalRole = NurseRole.Outgoing;
        [SerializeField] float m_PoseSendInterval = 0.1f;
        [SerializeField] bool m_LogSentPoses = true;

        [Header("Widget spawning")]
        [SerializeField] GameObject m_WidgetPrefab;
        [SerializeField] Transform m_WidgetParent;

        WebSocket m_WebSocket;
        bool m_ShouldReconnect = true;
        float m_NextPoseSendTime;

        readonly Dictionary<string, NetworkObject> m_SpawnedWidgets = new();

        [System.Serializable]
        class WidgetMessage
        {
            public string action;
            public string widget_id;
            public string widget_type;
            public string reference_frame;
            public string label;
            public string data_key;
            public string target_client;
            public string facilitator_note;

            // Fields sent by the React dashboard.
            public string x_meters;
            public string y_meters;
            public string z_meters;
            public string value;
            public string x_axis;
            public string y_axis;
            public string description;
            public string items;
            public string body;
        }

        [System.Serializable]
        class NursePoseMessage
        {
            public string action;
            public string role;
            public float x_meters;
            public float y_meters;
            public float z_meters;
            public float yaw_degrees;
        }

        [System.Serializable]
        class WidgetPoseMessage
        {
            public string action;
            public string widget_id;
            public float x_meters;
            public float y_meters;
            public float z_meters;
            public string moved_by;
        }

        [System.Serializable]
        class CockpitEventMessage
        {
            public string action;          // always "cockpit_event"
            public string cockpit_action;  // "docked" | "undocked"
            public string widget_id;
            public string nurse_role;
            // Position at the moment of dock/undock.
            // For "docked":   local position relative to cockpit root (egocentric).
            // For "undocked": world position where the widget was released.
            public float x_meters;
            public float y_meters;
            public float z_meters;
            public float timestamp;        // Time.time at event
        }

        async void Start()
        {
            await Connect();
        }

        async System.Threading.Tasks.Task Connect()
        {
            while (m_ShouldReconnect)
            {
                m_WebSocket = new WebSocket(m_ServerUrl);

                m_WebSocket.OnOpen += () =>
                {
                    Debug.Log("[FacilitatorClient] Connected to relay");
                };

                m_WebSocket.OnError += (e) =>
                {
                    Debug.LogError($"[FacilitatorClient] Error: {e}");
                };

                m_WebSocket.OnClose += (e) =>
                {
                    Debug.Log("[FacilitatorClient] Disconnected — retrying in 3s");
                };

                m_WebSocket.OnMessage += (bytes) =>
                {
                    string msg = System.Text.Encoding.UTF8.GetString(bytes);
                    // Debug.Log($"[FacilitatorClient] Received: {msg}");
                    HandleMessage(msg);
                };

                await m_WebSocket.Connect();

                if (m_ShouldReconnect)
                {
                    await System.Threading.Tasks.Task.Delay((int)(m_ReconnectDelay * 1000));
                }
            }
        }

        void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            m_WebSocket?.DispatchMessageQueue();
#endif

            if (Time.time >= m_NextPoseSendTime)
            {
                m_NextPoseSendTime = Time.time + m_PoseSendInterval;
                SendLocalNursePose();
            }
        }

        async void SendLocalNursePose()
        {
            if (m_LocalNurse == null) return;
            if (m_WebSocket == null) return;
            if (m_WebSocket.State != WebSocketState.Open) return;

            Vector3 pos = m_LocalNurse.position;
            float yaw = m_LocalNurse.eulerAngles.y;

            NursePoseMessage msg = new NursePoseMessage
            {
                action = "nurse_pose",
                role = GetRoleString(),
                x_meters = pos.x,
                y_meters = pos.y,
                z_meters = pos.z,
                yaw_degrees = yaw
            };

            string json = JsonUtility.ToJson(msg);
            await m_WebSocket.SendText(json);

            if (m_LogSentPoses)
            {
                Debug.Log($"[FacilitatorClient] Sent local nurse pose: {json}");
            }
        }

        public async void SendWidgetPose(string widgetId, Vector3 position)
        {
            if (m_WebSocket == null) return;
            if (m_WebSocket.State != WebSocketState.Open) return;
            if (string.IsNullOrEmpty(widgetId)) return;

            WidgetPoseMessage msg = new WidgetPoseMessage
            {
                action = "widget_pose",
                widget_id = widgetId,
                x_meters = position.x,
                y_meters = position.y,
                z_meters = position.z,
                moved_by = GetRoleString()
            };

            string json = JsonUtility.ToJson(msg);
            await m_WebSocket.SendText(json);

            Debug.Log($"[FacilitatorClient] Sent widget pose: {json}");
        }

        public async void SendCockpitEvent(string widgetId, string action, Vector3 localPosition)
        {
            if (m_WebSocket == null) return;
            if (m_WebSocket.State != WebSocketState.Open) return;
            if (string.IsNullOrEmpty(widgetId)) return;

            CockpitEventMessage msg = new CockpitEventMessage
            {
                action         = "cockpit_event",
                cockpit_action = action,
                widget_id      = widgetId,
                nurse_role     = GetRoleString(),
                x_meters       = localPosition.x,
                y_meters       = localPosition.y,
                z_meters       = localPosition.z,
                timestamp      = Time.time
            };

            string json = JsonUtility.ToJson(msg);
            await m_WebSocket.SendText(json);

            Debug.Log($"[FacilitatorClient] Sent cockpit event: {json}");
        }

        string GetRoleString()
        {
            switch (m_LocalRole)
            {
                case NurseRole.Outgoing:
                    return "outgoing";

                case NurseRole.Incoming:
                    return "incoming";

                default:
                    return "unknown";
            }
        }

        void HandleMessage(string json)
        {
            var msg = JsonUtility.FromJson<WidgetMessage>(json);
            if (msg == null) return;

            switch (msg.action)
            {
                case "spawn":
                    Debug.Log(
                        $"[FacilitatorClient] Spawn: {msg.widget_type} " +
                        $"ID: {msg.widget_id} " +
                        $"Frame: {msg.reference_frame} " +
                        $"Label: {msg.label} " +
                        $"Position: ({msg.x_meters}, {msg.y_meters}, {msg.z_meters})"
                    );

                    bool shouldHandleDashboardSpawn = m_LocalRole == NurseRole.Outgoing;

                    Debug.Log(
                        $"[FacilitatorClient] Spawn authority check: " +
                        $"Role={m_LocalRole}, " +
                        $"ShouldHandle={shouldHandleDashboardSpawn}, " +
                        $"IsServer={NetworkManager.Singleton.IsServer}, " +
                        $"IsHost={NetworkManager.Singleton.IsHost}, " +
                        $"IsClient={NetworkManager.Singleton.IsClient}, " +
                        $"IsConnectedClient={NetworkManager.Singleton.IsConnectedClient}"
                    );

                    if (!shouldHandleDashboardSpawn)
                    {
                        Debug.Log("[FacilitatorClient] Ignoring spawn on non-Outgoing instance.");
                        break;
                    }

                    SpawnWidget(msg);
                    break;

                case "delete":
                    Debug.Log($"[FacilitatorClient] Delete: {msg.widget_id}");

                    if (!NetworkManager.Singleton.IsServer)
                    {
                        Debug.Log("[FacilitatorClient] Ignoring delete on client. Server will despawn the NetworkObject.");
                        break;
                    }

                    if (m_SpawnedWidgets.TryGetValue(msg.widget_id, out var obj))
                    {
                        if (obj != null && obj.IsSpawned)
                        {
                            obj.Despawn();
                        }

                        m_SpawnedWidgets.Remove(msg.widget_id);
                    }
                    break;

                case "nurse_pose":
                    // Usually ignored by Unity.
                    // React dashboard is the main receiver for these.
                    break;

                case "widget_pose":
                    // Usually ignored by Unity.
                    // React dashboard is the main receiver for these.
                    break;

                case "cockpit_event":
                    // Logged by relay for study analysis. Unity ignores it.
                    break;

                default:
                    Debug.LogWarning($"[FacilitatorClient] Unknown action: {msg.action}");
                    break;
            }
        }

        float ParseFloat(string value, float fallback)
        {
            if (float.TryParse(value, out float result))
            {
                return result;
            }

            return fallback;
        }

        void SpawnWidget(WidgetMessage msg)
        {
            if (string.IsNullOrEmpty(msg.widget_id))
            {
                Debug.LogWarning("[FacilitatorClient] Cannot spawn widget: missing widget_id.");
                return;
            }

            if (m_SpawnedWidgets.ContainsKey(msg.widget_id))
            {
                Debug.LogWarning($"[FacilitatorClient] Widget already exists: {msg.widget_id}");
                return;
            }

            if (m_WidgetPrefab == null)
            {
                Debug.LogWarning("[FacilitatorClient] Cannot spawn widget: no widget prefab assigned.");
                return;
            }

            float x = ParseFloat(msg.x_meters, 0f);
            float y = ParseFloat(msg.y_meters, 1.5f);
            float z = ParseFloat(msg.z_meters, 0f);

            GameObject obj = Instantiate(
                m_WidgetPrefab,
                new Vector3(x, y, z),
                Quaternion.identity,
                m_WidgetParent
            );

            NetworkObject networkObject = obj.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogWarning("[FacilitatorClient] Spawned widget prefab has no NetworkObject component.");
                Destroy(obj);
                return;
            }

            DashboardTextWidget textWidget = obj.GetComponentInChildren<DashboardTextWidget>();

            if (textWidget != null)
            {
                textWidget.Init(
                    msg.widget_id,
                    msg.widget_type,
                    msg.label,
                    msg.body
                );
            }
            else
            {
                Debug.LogWarning("[FacilitatorClient] Spawned widget prefab has no DashboardTextWidget component.");
            }

            DashboardWidgetPoseReporter poseReporter =
                obj.GetComponentInChildren<DashboardWidgetPoseReporter>();

            if (poseReporter != null)
            {
                poseReporter.Init(msg.widget_id, this);
            }
            else
            {
                Debug.LogWarning("[FacilitatorClient] Spawned widget prefab has no DashboardWidgetPoseReporter component.");
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                networkObject.Spawn();
                Debug.Log("[FacilitatorClient] Spawned widget as NetworkObject.");
            }
            else
            {
                Debug.LogWarning(
                    "[FacilitatorClient] NetworkManager is not connected. " +
                    "Widget spawned locally only for testing."
                );
            }

            m_SpawnedWidgets[msg.widget_id] = networkObject;

            Debug.Log(
                $"[FacilitatorClient] Spawned widget object: {obj.name} " +
                $"ID: {msg.widget_id} " +
                $"at ({x}, {y}, {z})"
            );
        }

        async void OnDestroy()
        {
            m_ShouldReconnect = false;

            if (m_WebSocket != null)
            {
                await m_WebSocket.Close();
            }
        }
    }
}