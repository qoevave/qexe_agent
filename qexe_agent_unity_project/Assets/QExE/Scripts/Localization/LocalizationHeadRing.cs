using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace QExE.Localization
{
    /// <summary>
    /// Horizontal circle around the listening position (see <see cref="LocalizationSpace"/>) that gives
    /// a spatial reference. It is fixed in the room: the participant sits in its centre, and its front
    /// tick is the front defined by the LocalizationSpace. Major ticks every <see cref="majorTickSpacing"/> degrees,
    /// optional minor ticks in between (<see cref="minorTickSpacing"/>), a highlighted front tick, optional labels, and a marker at the currently marked azimuth.
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

        public float ringWidth = 0.012f;

        [Header("Ticks")]
        [Tooltip("Degrees between major ticks. Rounded so the ticks divide the circle evenly.")]
        [Range(5f, 180f), FormerlySerializedAs("tickSpacing")]
        public float majorTickSpacing = 30f;
        [Tooltip("Major tick height in metres (vertical, centred on the ring).")]
        [FormerlySerializedAs("tickLength")]
        public float majorTickLength = 0.15f;
        [Tooltip("Major tick line width in metres.")]
        [FormerlySerializedAs("tickWidth")]
        public float majorTickWidth = 0.015f;

        [Tooltip("Degrees between minor ticks. 0 = no minor ticks. Minor ticks that fall on a major tick are skipped.")]
        [Range(0f, 90f)]
        public float minorTickSpacing = 0f;
        [Tooltip("Minor tick height in metres.")]
        public float minorTickLength = 0.07f;
        [Tooltip("Minor tick line width in metres.")]
        public float minorTickWidth = 0.01f;
        [Tooltip("Colour of the minor ticks (major ticks use circleColor).")]
        public Color minorTickColor = new Color(1f, 1f, 1f, 0.5f);

        [Tooltip("The front tick (azimuth 0, frontColor) is this much taller than a major tick.")]
        public float frontTickScale = 2.5f;
        [Tooltip("The front tick is this much wider than a major tick.")]
        public float frontTickWidthScale = 1.5f;

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

        [Tooltip("Reference marker (paired localization: the locked A answer while answering B).")]
        public Color referenceColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);

        [SerializeField, HideInInspector] private LineRenderer _referenceMarker;
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

        /// <summary>
        /// Shows (or hides with null) a neutral reference marker, e.g. the locked A answer in paired
        /// localization. Created on first use if the visuals were built before this marker existed.
        /// </summary>
        public void SetReferenceMarker(float? azimuth)
        {
            if (_referenceMarker == null)
            {
                if (!azimuth.HasValue || visualsRoot == null) return;
                _referenceMarker = Line(visualsRoot, "ReferenceDirection", markerWidth, referenceColor, false);
            }
            _referenceMarker.enabled = azimuth.HasValue;
            if (!azimuth.HasValue) return;

            Vector3 p = LocalizationVisuals.AzimuthToLocalDirection(azimuth.Value) * _builtRadius;
            _referenceMarker.positionCount = 2;
            _referenceMarker.SetPosition(0, p + Vector3.down * markerHeight * 0.4f);
            _referenceMarker.SetPosition(1, p + Vector3.up * markerHeight * 0.4f);
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

            BuildTicks(root, r);

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

            _referenceMarker = Line(root, "ReferenceDirection", markerWidth, referenceColor, false);
            _referenceMarker.enabled = false;
            _marker = Line(root, "MarkedDirection", markerWidth, markerColor, false);
            _marker.enabled = false;
            _targetMarker = Line(root, "TargetDirection", markerWidth, targetColor, false);
            _targetMarker.enabled = false;
        }

        /// <summary>
        /// Major ticks every <see cref="majorTickSpacing"/> degrees (the one at 0 is the front tick),
        /// minor ticks every <see cref="minorTickSpacing"/> degrees in between. Spacings are rounded so
        /// the ticks divide 360 evenly. Ticks are vertical lines centred on the ring.
        /// </summary>
        private void BuildTicks(Transform root, float r)
        {
            float[] majors = EvenAngles(majorTickSpacing);
            float[] minors = minorTickSpacing > 0f ? EvenAngles(minorTickSpacing) : new float[0];

            foreach (float az in majors)
            {
                if (IsFront(az))
                    AddTick(root, r, "Tick_Front", 0f, majorTickLength * frontTickScale,
                            majorTickWidth * frontTickWidthScale, frontColor);
                else
                    AddTick(root, r, "Tick_Major_" + AngleName(az), az, majorTickLength, majorTickWidth, circleColor);
            }

            foreach (float az in minors)
            {
                if (IsFront(az) || OnAny(az, majors)) continue;
                AddTick(root, r, "Tick_Minor_" + AngleName(az), az, minorTickLength, minorTickWidth, minorTickColor);
            }
        }

        private void AddTick(Transform root, float r, string name, float azimuth, float height, float width, Color color)
        {
            var tick = Line(root, name, Mathf.Max(0f, width), color, false);
            Vector3 p = LocalizationVisuals.AzimuthToLocalDirection(azimuth) * r;
            tick.positionCount = 2;
            tick.SetPosition(0, p + Vector3.down * height * 0.5f);
            tick.SetPosition(1, p + Vector3.up * height * 0.5f);
        }

        /// <summary>Angles 0..360 (exclusive) spaced as close to <paramref name="spacing"/> as divides the circle evenly.</summary>
        private static float[] EvenAngles(float spacing)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(360f / Mathf.Max(1f, spacing)));
            var angles = new float[count];
            for (int i = 0; i < count; i++)
                angles[i] = i * 360f / count;
            return angles;
        }

        private static bool IsFront(float az)
        {
            return Mathf.Abs(Mathf.DeltaAngle(az, 0f)) < 0.01f;
        }

        private static bool OnAny(float az, float[] angles)
        {
            foreach (float a in angles)
                if (Mathf.Abs(Mathf.DeltaAngle(az, a)) < 0.01f) return true;
            return false;
        }

        private static string AngleName(float az)
        {
            return az.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
