using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Visualises the cockpit dock zone as a wireframe sphere.
    ///
    /// This component lives on a prefab so all appearance properties
    /// (material, line width, sorting order, colours, timing) are
    /// editable in the Inspector without touching code.
    ///
    /// Setup: create a prefab with this component on the root. Add
    /// LineRenderer child GameObjects for each ring — 6 latitude + 4
    /// longitude = 10 total is a good default. Assign them to
    /// m_LatitudeRings and m_LongitudeRings. Configure material,
    /// sorting layer/order, and line width directly on those LineRenderers.
    /// CockpitManager will instantiate the prefab and call Initialise().
    ///
    /// The script only drives: position data (ring geometry), colour,
    /// and enabled state. Everything else is yours to configure.
    /// </summary>
    public class CockpitZoneVisualiser : MonoBehaviour
    {
        // ── Inspector — geometry ──────────────────────────────────────────────

        [Header("Ring references")]
        [Tooltip("Horizontal rings. Assign LineRenderer children here. " +
                 "Positions are computed from m_Radius — everything else " +
                 "(material, width, sorting) configure directly on the LineRenderer.")]
        [SerializeField] LineRenderer[] m_LatitudeRings;

        [Tooltip("Vertical great-circle rings.")]
        [SerializeField] LineRenderer[] m_LongitudeRings;

        [Tooltip("Sphere radius in metres. Should match CockpitManager.m_DockThreshold.")]
        [SerializeField] float m_Radius = 0.8f;

        [Tooltip("Points per ring. More = smoother. 48 is a good default.")]
        [SerializeField] int m_Segments = 48;

        [Tooltip("Latitude ring elevations in degrees, bottom to top. " +
                 "Count must match m_LatitudeRings array length.")]
        [SerializeField] float[] m_LatitudeElevations = { -70f, -40f, -10f, 20f, 50f, 80f };

        [Tooltip("Longitude ring yaw offsets in degrees. " +
                 "Count must match m_LongitudeRings array length.")]
        [SerializeField] float[] m_LongitudeYaws = { 0f, 45f, 90f, 135f };

        // ── Inspector — colours ───────────────────────────────────────────────

        [Header("Colours")]
        [Tooltip("Sphere colour when widget is approaching but outside dock zone.")]
        [SerializeField] Color m_ColourNeutral  = new Color(0.094f, 0.439f, 0.714f, 1f); // #1870B6
        [Tooltip("Sphere colour when widget is inside dock zone — will dock on release.")]
        [SerializeField] Color m_ColourWillDock = new Color(0.125f, 0.588f, 0.953f, 1f); // #2096F3
        [Tooltip("Brief confirmation flash colour on successful dock.")]
        [SerializeField] Color m_ColourFlash    = new Color(1.000f, 1.000f, 1.000f, 1f);

        // ── Inspector — timing ────────────────────────────────────────────────

        [Header("Timing")]
        [Tooltip("Distance multiplier beyond dock threshold at which sphere starts " +
                 "fading in. 1.8 = sphere appears at 1.8 × dock threshold distance.")]
        [SerializeField] float m_OuterRadiusMultiplier = 1.8f;

        [Tooltip("Alpha fade speed (higher = snappier fade in/out).")]
        [SerializeField] float m_FadeSpeed = 6f;

        [Tooltip("Pulse speed in radians/sec when widget is in will-dock state.")]
        [SerializeField] float m_PulseSpeed = 3.5f;

        [Tooltip("Pulse depth — how much alpha varies during pulse. 0 = no pulse.")]
        [SerializeField][Range(0f, 1f)] float m_PulseDepth = 0.35f;

        [Tooltip("Dock confirmation flash duration in seconds.")]
        [SerializeField] float m_FlashDuration = 0.25f;

        // ── Runtime state ─────────────────────────────────────────────────────

        float m_DockThreshold;
        float m_OuterRadius;
        float m_CurrentAlpha;
        bool  m_Flashing;
        float m_FlashTimer;
        bool  m_WidgetGrabbed;

        // ── Initialisation ────────────────────────────────────────────────────

        /// <summary>
        /// Called by CockpitManager after instantiating the prefab.
        /// Builds ring geometry and hides all renderers.
        /// </summary>
        public void Initialise(float dockThreshold)
        {
            m_DockThreshold = dockThreshold;
            m_OuterRadius   = dockThreshold * m_OuterRadiusMultiplier;
            m_Radius        = dockThreshold; // keep radius in sync with threshold

            BuildRingGeometry();
            ApplyColour(m_ColourNeutral, 0f);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetWidgetGrabbed(bool grabbed)
        {
            m_WidgetGrabbed = grabbed;
            if (!grabbed)
            {
                m_LastDistance = float.MaxValue;
                m_CurrentAlpha = 0f;
                m_Flashing     = false;
                m_FlashTimer   = 0f;
                ApplyColour(m_ColourNeutral, 0f);
            }
        }

        public void UpdateProximity(float distanceToAnchor)
        {
            if (m_Flashing)      return;
            if (!m_WidgetGrabbed) return;

            m_LastDistance = distanceToAnchor;

            bool insideZone  = distanceToAnchor <= m_DockThreshold;
            bool insideOuter = distanceToAnchor <= m_OuterRadius;

            float targetAlpha = insideOuter
                ? Mathf.InverseLerp(m_OuterRadius, m_DockThreshold * 0.5f, distanceToAnchor)
                : 0f;
            targetAlpha    = Mathf.Clamp01(targetAlpha);
            m_CurrentAlpha = Mathf.Lerp(m_CurrentAlpha, targetAlpha, m_FadeSpeed * Time.deltaTime);

            if (insideZone)
            {
                float pulse = 1f - m_PulseDepth *
                              (0.5f + 0.5f * Mathf.Sin(Time.time * m_PulseSpeed));
                ApplyColour(m_ColourWillDock, m_CurrentAlpha * pulse);
            }
            else
            {
                ApplyColour(m_ColourNeutral, m_CurrentAlpha);
            }
        }

        public void PlayDockConfirmation()
        {
            m_Flashing   = true;
            m_FlashTimer = 0f;
            ApplyColour(m_ColourFlash, 1f);
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        void Update()
        {
            if (!m_Flashing) return;

            m_FlashTimer += Time.deltaTime;
            float t = m_FlashTimer / m_FlashDuration;

            if (t >= 1f)
            {
                m_Flashing      = false;
                m_WidgetGrabbed = false;
                m_CurrentAlpha  = 0f;
                ApplyColour(m_ColourFlash, 0f);
            }
            else
            {
                ApplyColour(m_ColourFlash, Mathf.Lerp(1f, 0f, t));
            }
        }

        // ── Internal ─────────────────────────────────────────────────────────

        float m_LastDistance = float.MaxValue;

        void ApplyColour(Color colour, float alpha)
        {
            colour.a = alpha;
            bool visible = alpha > 0.001f;

            if (m_LatitudeRings != null)
                foreach (var lr in m_LatitudeRings)
                    if (lr != null) { lr.enabled = visible; lr.startColor = colour; lr.endColor = colour; }

            if (m_LongitudeRings != null)
                foreach (var lr in m_LongitudeRings)
                    if (lr != null) { lr.enabled = visible; lr.startColor = colour; lr.endColor = colour; }
        }

        void BuildRingGeometry()
        {
            // Latitude rings — horizontal circles at different elevations.
            if (m_LatitudeRings != null)
            {
                for (int i = 0; i < m_LatitudeRings.Length; i++)
                {
                    if (m_LatitudeRings[i] == null) continue;
                    float elev       = (i < m_LatitudeElevations.Length
                                       ? m_LatitudeElevations[i] : 0f) * Mathf.Deg2Rad;
                    float ringRadius = m_Radius * Mathf.Cos(elev);
                    float y          = m_Radius * Mathf.Sin(elev);

                    var lr = m_LatitudeRings[i];
                    lr.useWorldSpace  = false;
                    lr.positionCount  = m_Segments + 1;
                    for (int s = 0; s <= m_Segments; s++)
                    {
                        float a = (float)s / m_Segments * Mathf.PI * 2f;
                        lr.SetPosition(s, new Vector3(Mathf.Sin(a) * ringRadius,
                                                      y,
                                                      Mathf.Cos(a) * ringRadius));
                    }
                }
            }

            // Longitude rings — vertical great circles at different yaw offsets.
            if (m_LongitudeRings != null)
            {
                for (int i = 0; i < m_LongitudeRings.Length; i++)
                {
                    if (m_LongitudeRings[i] == null) continue;
                    float yaw = (i < m_LongitudeYaws.Length
                                ? m_LongitudeYaws[i] : 0f) * Mathf.Deg2Rad;

                    var lr = m_LongitudeRings[i];
                    lr.useWorldSpace  = false;
                    lr.positionCount  = m_Segments + 1;
                    for (int s = 0; s <= m_Segments; s++)
                    {
                        float a  = (float)s / m_Segments * Mathf.PI * 2f;
                        float x0 = Mathf.Sin(a) * m_Radius;
                        float y0 = Mathf.Cos(a) * m_Radius;
                        lr.SetPosition(s, new Vector3(x0 * Mathf.Cos(yaw),
                                                      y0,
                                                      x0 * Mathf.Sin(yaw)));
                    }
                }
            }
        }
    }
}