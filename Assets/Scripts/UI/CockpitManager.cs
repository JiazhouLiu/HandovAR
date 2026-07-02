using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace XRMultiplayer
{
    /// <summary>
    /// Manages the personal cockpit for one local nurse.
    ///
    /// The cockpit is an egocentric, head-anchored region in front of the user.
    /// Widgets dock explicitly: the zone visualiser appears only while grabbing,
    /// and a widget docks only if released within arm's reach (distance-only check —
    /// no angle constraint, so participants in the elicitation study can dock
    /// anywhere around themselves naturally).
    ///
    /// Widgets self-register via CockpitWidgetRegistrar on the widget prefab.
    ///
    /// Defaults from:
    ///   Satriadi et al. (2020) Maps Around Me — spherical cap preference,
    ///     avg radius 2.07 m for far maps → 0.5 m for near/cockpit space.
    ///   Liu et al. (2023) DataDancing — cockpit radius 0.5 m, egocentric frame.
    ///   NASA-STD-3000 (via Maps Around Me) — 150° max vertical arc.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class CockpitManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────

        public static CockpitManager Instance { get; private set; }

        // ── Inspector fields ─────────────────────────────────────────────────

        [Header("Zone geometry")]
        [Tooltip("Radius in metres. 0.5 m = comfortable direct-grab reach for hand tracking.")]
        [SerializeField] float m_CockpitRadius = 0.5f;

        [Tooltip("Release distance threshold for docking. Slightly larger than radius to " +
                 "account for releasing at full arm extension.")]
        [SerializeField] float m_DockThreshold = 0.8f;

        [Tooltip("Head-space offset for the cockpit anchor. Y = -0.15 puts it at " +
                 "chin/chest height rather than face level.")]
        [SerializeField] Vector3 m_HeadOffset = new Vector3(0f, -0.15f, 0f);

        [Header("References")]
        [Tooltip("Prefab with CockpitZoneVisualiser component. Instantiated at runtime " +
                 "as a child of the cockpit anchor. Configure appearance on the prefab.")]
        [SerializeField] CockpitZoneVisualiser m_VisualiserPrefab;

        [Tooltip("The 'Widgets' transform used in FacilitatorClient — widgets are re-parented " +
                 "here on undock.")]
        [SerializeField] Transform m_WorldWidgetParent;

        [Tooltip("Optional. Enables cockpit event logging via WebSocket relay.")]
        [SerializeField] FacilitatorClient m_FacilitatorClient;

        [Header("Cooldown")]
        [Tooltip("Seconds after docking during which an undock cannot trigger. " +
                 "Prevents the phantom re-grab that occurs when the hand collider " +
                 "is still overlapping the pill after the dock snap.")]
        [SerializeField] float m_UndockCooldown = 0.4f;

        // ── Runtime state ────────────────────────────────────────────────────

        readonly Dictionary<GameObject, CockpitEntry> m_DockedWidgets = new();

        // Records Time.time when each widget was docked, for cooldown checks.
        readonly Dictionary<GameObject, float> m_DockTime = new();

        Transform             m_CockpitRoot;
        CockpitZoneVisualiser m_Visualiser;
        int                   m_ActiveGrabCount;
        // The widget root currently being grabbed, for proximity tracking.
        GameObject            m_GrabbedWidgetRoot;

        // ── Inner types ──────────────────────────────────────────────────────

        struct CockpitEntry
        {
            public string    WidgetId;
            public Transform OriginalParent;
        }

        // ── Unity lifecycle ──────────────────────────────────────────────────

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[CockpitManager] Duplicate — destroying extra instance.");
                Destroy(this);
                return;
            }
            Instance = this;

            m_CockpitRoot = new GameObject("CockpitRoot").transform;
            m_CockpitRoot.SetParent(null);

            // Instantiate visualiser prefab as child of cockpit root.
            // If no prefab assigned, cockpit works without visual feedback.
            if (m_VisualiserPrefab != null)
            {
                m_Visualiser = Instantiate(m_VisualiserPrefab, m_CockpitRoot);
                m_Visualiser.transform.localPosition = Vector3.zero;
                m_Visualiser.transform.localRotation = Quaternion.identity;
                m_Visualiser.Initialise(m_DockThreshold);
            }
            else
            {
                Debug.LogWarning("[CockpitManager] No visualiser prefab assigned — " +
                                 "cockpit zone will have no visual feedback.");
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (m_CockpitRoot != null) Destroy(m_CockpitRoot.gameObject);
        }

        void Update()
        {
            if (Camera.main == null) return;

            m_CockpitRoot.position =
                Camera.main.transform.position +
                Camera.main.transform.TransformDirection(m_HeadOffset);

            // Yaw only — zone faces the nurse's horizontal look direction
            // but doesn't tilt when they look up/down.
            Vector3 flatForward = Camera.main.transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude > 0.001f)
                m_CockpitRoot.rotation = Quaternion.LookRotation(flatForward.normalized);

            // Drive sphere appearance from widget proximity while grabbing.
            if (m_ActiveGrabCount > 0 && m_GrabbedWidgetRoot != null)
            {
                float dist = Vector3.Distance(m_GrabbedWidgetRoot.transform.position,
                                              m_CockpitRoot.position);
                m_Visualiser?.UpdateProximity(dist);
            }
        }

        // ── Registration API ─────────────────────────────────────────────────

        public void RegisterWidget(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab)
        {
            grab.selectEntered.AddListener(OnWidgetSelectEntered);
            grab.selectExited.AddListener(OnWidgetSelectExited);
        }

        public void UnregisterWidget(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab, GameObject widgetRoot)
        {
            grab.selectEntered.RemoveListener(OnWidgetSelectEntered);
            grab.selectExited.RemoveListener(OnWidgetSelectExited);
            m_DockedWidgets.Remove(widgetRoot);
            m_DockTime.Remove(widgetRoot);
        }

        // ── Grab event handlers ──────────────────────────────────────────────

        void OnWidgetSelectEntered(SelectEnterEventArgs args)
        {
            m_ActiveGrabCount++;
            // Cache the grabbed widget root for proximity tracking in Update().
            DashboardWidgetPoseReporter rep =
                args.interactableObject.transform
                    .GetComponentInParent<DashboardWidgetPoseReporter>();
            m_GrabbedWidgetRoot = rep != null ? rep.gameObject : null;
            m_Visualiser?.SetWidgetGrabbed(true);
        }

        void OnWidgetSelectExited(SelectExitEventArgs args)
        {
            m_ActiveGrabCount = Mathf.Max(0, m_ActiveGrabCount - 1);

            DashboardWidgetPoseReporter reporter =
                args.interactableObject.transform
                    .GetComponentInParent<DashboardWidgetPoseReporter>();

            // Resolve dock/undock logic first, then clean up visualiser.
            // This order matters: Dock() calls PlayDockConfirmation() which
            // needs SetWidgetGrabbed(false) to NOT have run yet (the flash
            // owns the sphere appearance until it completes).
            bool didDock = false;

            if (reporter != null)
            {
                GameObject widgetRoot = reporter.gameObject;
                bool wasDocked        = m_DockedWidgets.ContainsKey(widgetRoot);
                bool insideZone       = IsInsideZone(widgetRoot.transform.position);

                Debug.Log($"[CockpitManager] selectExited — wasDocked={wasDocked} " +
                          $"insideZone={insideZone} " +
                          $"distance={Vector3.Distance(widgetRoot.transform.position, m_CockpitRoot.position):F3}m");

                if (wasDocked)
                {
                    if (m_DockTime.TryGetValue(widgetRoot, out float dockedAt))
                    {
                        float elapsed = Time.time - dockedAt;
                        if (elapsed < m_UndockCooldown)
                        {
                            Debug.Log($"[CockpitManager] Undock suppressed — cooldown " +
                                      $"({elapsed:F2}s < {m_UndockCooldown}s).");
                            // Still need to clean up visualiser below.
                        }
                        else if (!insideZone)
                        {
                            Undock(widgetRoot, reporter);
                        }
                    }
                    else if (!insideZone)
                    {
                        Undock(widgetRoot, reporter);
                    }
                }
                else if (insideZone)
                {
                    Dock(widgetRoot, reporter);
                    didDock = true;
                }
            }

            // Clean up visualiser — but only if we didn't just dock.
            // Dock() calls PlayDockConfirmation() which owns the sphere
            // appearance for its flash duration; we must not override it.
            if (m_ActiveGrabCount == 0 && !didDock)
            {
                m_GrabbedWidgetRoot = null;
                m_Visualiser?.SetWidgetGrabbed(false);
            }
            else if (m_ActiveGrabCount == 0)
            {
                // Docked — flash is playing, just clear the tracked root.
                m_GrabbedWidgetRoot = null;
            }
        }

        // ── Zone test ────────────────────────────────────────────────────────

        /// <summary>
        /// Distance-only sphere check. No angle constraint — participants can dock
        /// anywhere around themselves, which is important for an elicitation study
        /// where we want to observe natural placement behaviour.
        /// </summary>
        bool IsInsideZone(Vector3 worldPos)
        {
            return Vector3.Distance(worldPos, m_CockpitRoot.position) <= m_DockThreshold;
        }

        // ── Dock / Undock ────────────────────────────────────────────────────

        void Dock(GameObject widgetRoot, DashboardWidgetPoseReporter reporter)
        {
            Transform originalParent = widgetRoot.transform.parent;
            widgetRoot.transform.SetParent(m_CockpitRoot, worldPositionStays: true);

            string widgetId = reporter.WidgetId;

            // Zero residual velocity from the grab so the widget doesn't drift.
            // Note: the pill collider should be set to Is Trigger = true on the
            // prefab — this prevents physics collision with the XR rig capsule
            // (which would trigger NetworkRigidbody ownership transfer and fling
            // the widget) while still allowing XRI grab detection to work normally.
            Rigidbody rb = widgetRoot.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            m_DockedWidgets[widgetRoot] = new CockpitEntry
            {
                WidgetId       = widgetId,
                OriginalParent = originalParent
            };

            // Record dock time for undock cooldown.
            m_DockTime[widgetRoot] = Time.time;

            // Zero out any rotation introduced by the grab transformer —
            // billboard will handle facing. Keep world position, clear rotation.
            widgetRoot.transform.rotation = Quaternion.identity;

            m_Visualiser?.PlayDockConfirmation();
            Debug.Log($"[CockpitManager] Docked '{widgetId}'.");

            m_FacilitatorClient?.SendCockpitEvent(
                widgetId,
                action:        "docked",
                localPosition: m_CockpitRoot.InverseTransformPoint(widgetRoot.transform.position)
            );
        }

        void Undock(GameObject widgetRoot, DashboardWidgetPoseReporter reporter)
        {
            if (!m_DockedWidgets.TryGetValue(widgetRoot, out CockpitEntry entry)) return;

            Transform returnParent = entry.OriginalParent != null
                                   ? entry.OriginalParent
                                   : m_WorldWidgetParent;

            widgetRoot.transform.SetParent(returnParent, worldPositionStays: true);

            // Zero velocity so the widget doesn't drift after undock.
            Rigidbody rb = widgetRoot.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            m_DockedWidgets.Remove(widgetRoot);
            m_DockTime.Remove(widgetRoot);

            Debug.Log($"[CockpitManager] Undocked '{entry.WidgetId}'.");

            m_FacilitatorClient?.SendCockpitEvent(
                entry.WidgetId,
                action:        "undocked",
                localPosition: widgetRoot.transform.position
            );
        }

        // ── Public API ───────────────────────────────────────────────────────

        public int   DockedCount => m_DockedWidgets.Count;
        public float Radius      => m_CockpitRadius;

        // ── Gizmos ───────────────────────────────────────────────────────────

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || m_CockpitRoot == null) return;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireSphere(m_CockpitRoot.position, m_CockpitRadius);

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.08f);
            Gizmos.DrawSphere(m_CockpitRoot.position, m_DockThreshold);
        }
#endif
    }
}