using UnityEngine;


namespace XRMultiplayer
{
    /// <summary>
    /// Add this component to the widget prefab root.
    ///
    /// Registers the widget's XRGrabInteractable with CockpitManager so the
    /// cockpit can respond to grab/release events on this widget.
    ///
    /// Registration is attempted in both OnEnable and Start:
    ///   - OnEnable covers runtime-spawned widgets (dashboard spawn) where
    ///     CockpitManager is already initialised.
    ///   - Start covers scene-placed widgets that wake before CockpitManager
    ///     despite [DefaultExecutionOrder(-50)], and NGO-spawned widgets whose
    ///     OnEnable fires on the network thread before the main-thread Awake order
    ///     is guaranteed.
    /// A guard flag prevents double-registration.
    /// </summary>
    public class CockpitWidgetRegistrar : MonoBehaviour
    {
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable m_Grab;
        GameObject         m_WidgetRoot;
        bool               m_Registered;

        // ── Registration helpers ─────────────────────────────────────────────

        void TryRegister()
        {
            if (m_Registered) return;

            if (CockpitManager.Instance == null)
            {
                // Will retry from Start() or the next call site.
                return;
            }

            // Find the XRGrabInteractable — expected on a child (the pill).
            m_Grab = GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (m_Grab == null)
            {
                Debug.LogWarning($"[CockpitWidgetRegistrar] No XRGrabInteractable found " +
                                 $"on '{gameObject.name}' or its children.");
                return;
            }

            // Widget root = GameObject with DashboardWidgetPoseReporter.
            var reporter = GetComponentInChildren<DashboardWidgetPoseReporter>()
                        ?? GetComponentInParent<DashboardWidgetPoseReporter>();
            m_WidgetRoot = reporter != null ? reporter.gameObject : gameObject;

            CockpitManager.Instance.RegisterWidget(m_Grab);
            m_Registered = true;

            Debug.Log($"[CockpitWidgetRegistrar] Registered '{gameObject.name}' with CockpitManager.");
        }

        // ── Unity lifecycle ──────────────────────────────────────────────────

        void OnEnable()  => TryRegister();

        // Start runs after all Awakes, so CockpitManager.Instance is guaranteed
        // to be set by the time Start executes — even for scene-placed widgets.
        void Start()     => TryRegister();

        void OnDisable()
        {
            if (!m_Registered) return;
            if (CockpitManager.Instance == null) return;
            if (m_Grab == null) return;

            CockpitManager.Instance.UnregisterWidget(m_Grab, m_WidgetRoot);
            m_Registered = false;
        }
    }
}