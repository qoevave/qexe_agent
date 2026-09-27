using TMPro;
using UnityEngine;

namespace QExE.Localization
{
    /// <summary>
    /// Top-down azimuth dial in front of and below the participant's eyes, with the trial counter and a
    /// short status line above it.
    ///
    /// The dial is a map of the horizontal plane seen from above: its top edge is the room's front
    /// (azimuth 0), left is left, the bottom edge is behind the participant. Aiming the pointer ray at
    /// the dial and pressing the trigger marks an azimuth (also while the stimulus plays); pressing the
    /// centre zone confirms the answer (see LocalizationTrialController).
    ///
    /// Fixed in the room relative to the listening position (see LocalizationSpace). Built in the editor (see
    /// <see cref="LocalizationVisualBuilder"/>): change the settings and the dial updates in the Scene view.
    /// </summary>
    public class LocalizationDial : LocalizationVisualBuilder
    {
        [Header("Placement (relative to the listening position's eye point and front)")]
        [Tooltip("Dial radius in metres.")]
        public float radius = 0.15f;
        public float forwardDistance = 0.45f;
        public float heightBelowHead = 0.35f;
        [Tooltip("0 = vertical facing the participant, 90 = lying flat.")]
        [Range(0f, 90f)]
        public float tilt = 50f;

        [Header("Interaction")]
        [Tooltip("Fraction of the radius around the centre that acts as the confirm button.")]
        [Range(0.05f, 0.5f)]
        public float centreZone = 0.25f;
        [Tooltip("Hits slightly outside the rim still count, as a fraction of the radius.")]
        public float rimTolerance = 0.15f;

        [Header("Look")]
        [Tooltip("Semi-transparent disc behind the dial lines.")]
        public bool showBackground = true;
        public float lineWidth = 0.004f;
        public Color backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.6f);
        public Color lineColor = new Color(1f, 1f, 1f, 0.85f);
        public Color frontColor = new Color(0.34f, 0.78f, 0.58f, 1f);
        public Color markerColor = new Color(1f, 0.75f, 0.2f, 1f);
        public Color submitReadyColor = new Color(0.34f, 0.78f, 0.58f, 1f);
        public Color submitIdleColor = new Color(1f, 1f, 1f, 0.3f);
        [Tooltip("Training feedback: colour of the correct direction.")]
        public Color targetColor = new Color(0.25f, 0.85f, 1f, 1f);

        [Header("Counter and status text (above the dial)")]
        public bool showCounter = true;
        public float counterHeight = 0.035f;
        public bool showStatus = true;
        public float statusHeight = 0.02f;
        public Color textColor = new Color(1f, 1f, 1f, 0.95f);
        [Tooltip("Distance of the text above the dial's top edge, in metres.")]
        public float textGap = 0.03f;

        public bool Visible { get; private set; } = true;
        public float? PreliminaryAzimuth { get; private set; }

        [SerializeField, HideInInspector] private LineRenderer _markerLine;
        [SerializeField, HideInInspector] private Transform _markerDot;
        [SerializeField, HideInInspector] private Transform _hoverDot;
        [SerializeField, HideInInspector] private LineRenderer _centreRing;
        [SerializeField, HideInInspector] private LineRenderer _targetLine;
        [SerializeField, HideInInspector] private Transform _targetDot;
        [SerializeField, HideInInspector] private TextMeshPro _counter;
        [SerializeField, HideInInspector] private TextMeshPro _status;

        protected override string VisualsName => "Dial Visuals";

        // ------------------------------------------------------------------------------------------
        // Runtime API
        // ------------------------------------------------------------------------------------------

        public void SetVisible(bool visible)
        {
            Visible = visible;
            if (visualsRoot != null) visualsRoot.gameObject.SetActive(visible);
        }

        /// <summary>Set (or clear with null) the marked azimuth.</summary>
        public void SetPreliminary(float? azimuth)
        {
            PreliminaryAzimuth = azimuth;
            if (_markerLine == null) return;
            _markerLine.enabled = azimuth.HasValue;
            _markerDot.gameObject.SetActive(azimuth.HasValue);
            if (!azimuth.HasValue) return;

            Vector3 d = DialDirection(azimuth.Value);
            _markerLine.positionCount = 2;
            _markerLine.SetPosition(0, d * radius * centreZone);
            _markerLine.SetPosition(1, d * radius);
            _markerDot.localPosition = d * radius;
        }

        /// <summary>Training feedback: show (or hide with null) the correct azimuth.</summary>
        public void SetTarget(float? azimuth)
        {
            if (_targetLine == null) return;
            _targetLine.enabled = azimuth.HasValue;
            _targetDot.gameObject.SetActive(azimuth.HasValue);
            if (!azimuth.HasValue) return;

            Vector3 d = DialDirection(azimuth.Value);
            _targetLine.positionCount = 2;
            _targetLine.SetPosition(0, d * radius * centreZone);
            _targetLine.SetPosition(1, d * radius * 1.08f);
            _targetDot.localPosition = d * radius * 1.08f;
        }

        /// <summary>Highlights the centre zone when pressing it would confirm an answer.</summary>
        public void SetSubmitReady(bool ready)
        {
            if (_centreRing == null) return;
            LocalizationVisuals.SetColor(_centreRing, ready ? submitReadyColor : submitIdleColor);
            _centreRing.widthMultiplier = lineWidth * (ready ? 2.5f : 1f);
        }

        public void SetHover(bool hovering, Vector3 worldPoint)
        {
            if (_hoverDot == null) return;
            _hoverDot.gameObject.SetActive(hovering && Visible);
            if (hovering) _hoverDot.position = worldPoint;
        }

        public void SetCounter(string text)
        {
            if (_counter != null && _counter.text != text) _counter.text = text;
        }

        public void SetStatus(string text)
        {
            if (_status != null && _status.text != text) _status.text = text;
        }

        /// <summary>
        /// Intersects a ray with the dial. Returns true if it hits the dial (with a small rim tolerance)
        /// and gives the world hit point, the azimuth it represents and whether it is in the centre zone.
        /// </summary>
        public bool Raycast(Vector3 origin, Vector3 direction, out Vector3 worldPoint, out float azimuth, out bool inCentre)
        {
            worldPoint = Vector3.zero;
            azimuth = 0f;
            inCentre = false;
            if (!Visible || visualsRoot == null || !visualsRoot.gameObject.activeInHierarchy)
                return false;

            var plane = new Plane(visualsRoot.forward, visualsRoot.position);
            var ray = new Ray(origin, direction);
            if (!plane.Raycast(ray, out float distance) || distance <= 0f)
                return false;

            worldPoint = ray.GetPoint(distance);
            Vector3 local = visualsRoot.InverseTransformPoint(worldPoint);
            float r = new Vector2(local.x, local.y).magnitude;
            if (r > radius * (1f + rimTolerance))
                return false;

            azimuth = Mathf.Atan2(-local.x, local.y) * Mathf.Rad2Deg;
            inCentre = r < radius * centreZone;
            return true;
        }

        private void LateUpdate()
        {
            if (visualsRoot == null) return;

            Quaternion yaw = FrontRotation();
            Vector3 position = EyePosition() + yaw * Vector3.forward * forwardDistance + Vector3.down * heightBelowHead;
            // Tilt the disc away from the participant: local +Y (azimuth 0) points forward and up.
            Place(visualsRoot, position, yaw * Quaternion.Euler(tilt, 0f, 0f));
        }

        // Dial plane is the root's local XY plane; +Y = front (azimuth 0), -X = left (+90).
        private static Vector3 DialDirection(float azimuthDeg)
        {
            float a = azimuthDeg * Mathf.Deg2Rad;
            return new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0f);
        }

        // ------------------------------------------------------------------------------------------
        // Build
        // ------------------------------------------------------------------------------------------

        protected override void Build(Transform root)
        {
            // Semi-transparent background disc (a flattened cylinder whose axis is local Z).
            if (showBackground)
                BuildBackground(root);

            BuildLines(root);
        }

        private void BuildBackground(Transform root)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Background";
            DestroyObject(disc.GetComponent<Collider>());
            disc.transform.SetParent(root, false);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disc.transform.localPosition = new Vector3(0f, 0f, 0.002f); // just behind the lines
            disc.transform.localScale = new Vector3(radius * 2.1f, 0.0005f, radius * 2.1f);
            var discRenderer = disc.GetComponent<MeshRenderer>();
            discRenderer.sharedMaterial = EffectiveMaterial;
            discRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            LocalizationVisuals.SetColor(disc.transform, backgroundColor);
        }

        private void BuildLines(Transform root)
        {
            var circle = Line(root, "Circle", lineWidth, lineColor, true);
            const int segments = 96;
            circle.positionCount = segments;
            for (int i = 0; i < segments; i++)
                circle.SetPosition(i, DialDirection(i * 360f / segments) * radius);

            for (int az = 0; az < 360; az += 30)
            {
                bool front = az == 0;
                bool cardinal = az % 90 == 0;
                var tick = Line(root, "Tick_" + az, lineWidth * (front ? 2f : cardinal ? 1.5f : 1f),
                                front ? frontColor : lineColor, false);
                Vector3 d = DialDirection(az);
                tick.positionCount = 2;
                tick.SetPosition(0, d * radius * (front ? 0.65f : cardinal ? 0.78f : 0.88f));
                tick.SetPosition(1, d * radius * (front ? 1.12f : 1f));
            }

            // "You" at the centre: a small head with a nose pointing to the front.
            var headCircle = Line(root, "Head", lineWidth, lineColor, true);
            headCircle.positionCount = 24;
            for (int i = 0; i < 24; i++)
                headCircle.SetPosition(i, DialDirection(i * 15f) * radius * 0.08f);
            var nose = Line(root, "Nose", lineWidth, frontColor, false);
            nose.positionCount = 3;
            nose.SetPosition(0, DialDirection(25f) * radius * 0.08f);
            nose.SetPosition(1, DialDirection(0f) * radius * 0.14f);
            nose.SetPosition(2, DialDirection(-25f) * radius * 0.08f);

            _centreRing = Line(root, "CentreZone", lineWidth, submitIdleColor, true);
            _centreRing.positionCount = 48;
            for (int i = 0; i < 48; i++)
                _centreRing.SetPosition(i, DialDirection(i * 7.5f) * radius * centreZone);

            _markerLine = Line(root, "MarkedLine", lineWidth * 2.5f, markerColor, false);
            _markerDot = Dot(root, "MarkedDot", radius * 0.12f, markerColor);
            _hoverDot = Dot(root, "HoverDot", radius * 0.06f, Color.white);
            _targetLine = Line(root, "TargetLine", lineWidth * 2.5f, targetColor, false);
            _targetDot = Dot(root, "TargetDot", radius * 0.12f, targetColor);

            float y = radius * 1.12f + textGap;
            if (showStatus)
            {
                _status = LocalizationVisuals.CreateLabel(root, "Status", "Listen", statusHeight, textColor);
                _status.transform.localPosition = new Vector3(0f, y + statusHeight * 0.5f, -0.001f);
                _status.rectTransform.sizeDelta = new Vector2(radius * 4f, statusHeight * 1.6f);
                y += statusHeight * 1.4f;
            }
            if (showCounter)
            {
                _counter = LocalizationVisuals.CreateLabel(root, "Counter", "Trial 1 / 60", counterHeight, textColor);
                _counter.fontStyle = FontStyles.Bold;
                _counter.transform.localPosition = new Vector3(0f, y + counterHeight * 0.5f, -0.001f);
            }

            SetPreliminary(null);
            SetTarget(null);
            SetHover(false, Vector3.zero);
            SetSubmitReady(false);
            visualsRoot = root;
            SetVisible(Application.isPlaying ? Visible : true);
        }
    }
}
