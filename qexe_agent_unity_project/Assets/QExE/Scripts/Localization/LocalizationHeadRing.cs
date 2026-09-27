using TMPro;
using UnityEngine;

namespace QExE.Localization
{
    /// <summary>
    /// Horizontal circle around the listening position (see <see cref="LocalizationSpace"/>) that gives
    /// a spatial reference. It is fixed in the room: the participant sits in its centre, and its front
    /// tick is the front defined by the LocalizationSpace. Tick marks every <see cref="tickSpacing"/> degrees, a
    /// highlighted front tick, optional labels, and a marker at the currently marked azimuth.
    ///
    /// Built in the editor (see <see cref="LocalizationVisualBuilder"/>): change the settings below and
    /// the ring updates in the Scene view.
    /// </summary>
    public class LocalizationHeadRing : LocalizationVisualBuilder
    {
        [Header("Shape")]
        [Tooltip("Ring radius in metres. 0 = use the pointer's sphere radius.")]
        public float radius = 0f;

        [Tooltip("Vertical offset relative to the listening position's eye height. Slightly below (e.g. -0.2) makes it read as a circle rather than a horizon line.")]
        public float verticalOffset = -0.2f;

        [Range(5f, 90f)]
        public float tickSpacing = 30f;
        public float ringWidth = 0.012f;
        public float tickWidth = 0.015f;
        public float tickLength = 0.15f;
        [Tooltip("The front tick is this much taller than the others.")]
        public float frontTickScale = 2.5f;

        [Header("Labels")]
        public bool showLabels = true;
        [Tooltip("Labels every 90 degrees (0 = front, 90 = left, 180 = behind, -90 = right).")]
        public string[] labels = { "0°", "90°", "180°", "-90°" };
        public float labelHeight = 0.1f;
        public float labelAboveRing = 0.18f;

        [Header("Colours")]
        public Color circleColor = new Color(1f, 1f, 1f, 0.7f);
        public Color frontColor = new Color(0.34f, 0.78f, 0.58f, 1f);
        public Color labelColor = new Color(1f, 1f, 1f, 0.9f);

        [Header("Marked direction")]
        public Color markerColor = new Color(1f, 0.75f, 0.2f, 1f);
        public float markerHeight = 0.5f;
        public float markerWidth = 0.025f;
        [Tooltip("Training feedback: colour of the correct direction.")]
        public Color targetColor = new Color(0.25f, 0.85f, 1f, 1f);

        [SerializeField, HideInInspector] private LineRenderer _marker;
        [SerializeField, HideInInspector] private LineRenderer _targetMarker;
        [SerializeField, HideInInspector] private float _builtRadius;

        protected override string VisualsName => "HeadRing Visuals";

        public float EffectiveRadius
        {
            get
            {
                if (radius > 0f) return radius;
                var pointer = GetComponent<LocalizationPointer>();
                return pointer != null ? pointer.sphereRadius : 2f;
            }
        }

        /// <summary>Show the marker at this azimuth (reference frame), or hide it with null.</summary>
        public void SetMarker(float? azimuth)
        {
            if (_marker == null) return;
            _marker.enabled = azimuth.HasValue;
            if (!azimuth.HasValue) return;

            Vector3 p = LocalizationVisuals.AzimuthToLocalDirection(azimuth.Value) * _builtRadius;
            _marker.positionCount = 2;
            _marker.SetPosition(0, p + Vector3.down * markerHeight * 0.5f);
            _marker.SetPosition(1, p + Vector3.up * markerHeight * 0.5f);
        }

        /// <summary>Training feedback: show the correct azimuth on the ring (null hides it).</summary>
        public void SetTargetMarker(float? azimuth)
        {
            if (_targetMarker == null) return;
            _targetMarker.enabled = azimuth.HasValue;
            if (!azimuth.HasValue) return;

            Vector3 p = LocalizationVisuals.AzimuthToLocalDirection(azimuth.Value) * _builtRadius;
            _targetMarker.positionCount = 2;
            _targetMarker.SetPosition(0, p + Vector3.down * markerHeight * 0.6f);
            _targetMarker.SetPosition(1, p + Vector3.up * markerHeight * 0.6f);
        }

        public void SetVisible(bool visible)
        {
            if (visualsRoot != null) visualsRoot.gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            if (visualsRoot == null) return;
            Place(visualsRoot, EyePosition() + Vector3.up * verticalOffset, FrontRotation());
        }

        protected override void Build(Transform root)
        {
            float r = EffectiveRadius;
            _builtRadius = r;

            var circle = Line(root, "Circle", ringWidth, circleColor, true);
            const int segments = 180;
            circle.positionCount = segments;
            for (int i = 0; i < segments; i++)
                circle.SetPosition(i, LocalizationVisuals.AzimuthToLocalDirection(i * 360f / segments) * r);

            int count = Mathf.Max(1, Mathf.RoundToInt(360f / tickSpacing));
            for (int i = 0; i < count; i++)
            {
                float az = i * 360f / count;
                bool front = i == 0;
                float h = front ? tickLength * frontTickScale : tickLength;
                var tick = Line(root, "Tick_" + Mathf.RoundToInt(az), front ? tickWidth * 1.5f : tickWidth,
                                front ? frontColor : circleColor, false);
                Vector3 p = LocalizationVisuals.AzimuthToLocalDirection(az) * r;
                tick.positionCount = 2;
                tick.SetPosition(0, p + Vector3.down * h * 0.5f);
                tick.SetPosition(1, p + Vector3.up * h * 0.5f);
            }

            if (showLabels && labels != null)
            {
                for (int i = 0; i < labels.Length && i < 4; i++)
                {
                    if (string.IsNullOrEmpty(labels[i])) continue;
                    float az = i * 90f;
                    Vector3 dir = LocalizationVisuals.AzimuthToLocalDirection(az);
                    TextMeshPro label = LocalizationVisuals.CreateLabel(root, "Label_" + Mathf.RoundToInt(az), labels[i],
                                                                        labelHeight, i == 0 ? frontColor : labelColor);
                    label.transform.localPosition = dir * r + Vector3.up * labelAboveRing;
                    // Face the centre (readable from the participant's position).
                    label.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);
                }
            }

            _marker = Line(root, "MarkedDirection", markerWidth, markerColor, false);
            _marker.enabled = false;
            _targetMarker = Line(root, "TargetDirection", markerWidth, targetColor, false);
            _targetMarker.enabled = false;
        }
    }
}
