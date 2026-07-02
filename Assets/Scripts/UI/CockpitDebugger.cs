using UnityEngine;


namespace XRMultiplayer
{
    /// <summary>
    /// Temporary diagnostic component. Add to the same scene GameObject as
    /// CockpitManager, then check the Console while dragging a widget.
    ///
    /// Remove (or just disable) once the cockpit is working correctly.
    /// </summary>
    public class CockpitDebugger : MonoBehaviour
    {
        void Start()
        {
            // ── 1. Is CockpitManager present? ────────────────────────────────
            var mgr = CockpitManager.Instance;
            if (mgr == null)
            {
                Debug.LogError("[CockpitDebugger] CockpitManager.Instance is NULL. " +
                               "Is CockpitManager in the scene and enabled?");
            }
            else
            {
                Debug.Log($"[CockpitDebugger] CockpitManager found on '{mgr.gameObject.name}'.");
            }

            // ── 2. Are any CockpitWidgetRegistrars present? ──────────────────
            // At Start() widgets may not be spawned yet, so we count what exists now.
            var registrars = FindObjectsByType<CockpitWidgetRegistrar>(FindObjectsSortMode.None);
            Debug.Log($"[CockpitDebugger] CockpitWidgetRegistrar count at Start: {registrars.Length}. " +
                      "(Expect 0 here if widgets spawn later via dashboard.)");

            // ── 3. Are any XRGrabInteractables present? ──────────────────────
            var grabs = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None);
            Debug.Log($"[CockpitDebugger] XRGrabInteractable count at Start: {grabs.Length}.");

            foreach (var g in grabs)
            {
                var reporter = g.GetComponentInParent<DashboardWidgetPoseReporter>()
                            ?? g.GetComponentInChildren<DashboardWidgetPoseReporter>();
                var registrar = g.GetComponentInParent<CockpitWidgetRegistrar>()
                             ?? g.GetComponentInChildren<CockpitWidgetRegistrar>();

                Debug.Log($"[CockpitDebugger] XRGrabInteractable on '{g.gameObject.name}' " +
                          $"| PoseReporter: {(reporter != null ? reporter.gameObject.name : "MISSING")} " +
                          $"| Registrar: {(registrar != null ? registrar.gameObject.name : "MISSING")}");
            }

            // ── 4. Camera.main check ─────────────────────────────────────────
            if (Camera.main == null)
                Debug.LogError("[CockpitDebugger] Camera.main is NULL. " +
                               "CockpitManager cannot anchor without it. " +
                               "Tag your XR camera as 'MainCamera'.");
            else
                Debug.Log($"[CockpitDebugger] Camera.main = '{Camera.main.gameObject.name}'.");
        }

        void Update()
        {
            // ── 5. Poll every 2 s for late-spawned widgets ───────────────────
            if (Time.time < m_NextPollTime) return;
            m_NextPollTime = Time.time + 2f;

            var registrars = FindObjectsByType<CockpitWidgetRegistrar>(FindObjectsSortMode.None);
            var grabs      = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None);

            Debug.Log($"[CockpitDebugger] Poll — Registrars: {registrars.Length}, " +
                      $"XRGrabInteractables: {grabs.Length}, " +
                      $"Docked: {CockpitManager.Instance?.DockedCount ?? -1}");

            foreach (var g in grabs)
            {
                bool isSelecting = g.isSelected;
                if (!isSelecting) continue; // only log actively grabbed ones

                var reporter  = g.GetComponentInParent<DashboardWidgetPoseReporter>()
                             ?? g.GetComponentInChildren<DashboardWidgetPoseReporter>();
                var registrar = g.GetComponentInParent<CockpitWidgetRegistrar>()
                             ?? g.GetComponentInChildren<CockpitWidgetRegistrar>();

                Debug.Log($"[CockpitDebugger] GRABBED: '{g.gameObject.name}' " +
                          $"| PoseReporter: {(reporter != null ? "OK" : "MISSING")} " +
                          $"| Registrar: {(registrar != null ? "OK" : "MISSING")}");
            }
        }

        float m_NextPollTime;
    }
}
