using UnityEngine;

namespace QExE.Localization
{
    /// <summary>
    /// Head orientation indicator and alignment check.
    ///
    /// Shows a target straight ahead of the listening position (azimuth 0, elevation 0, see
    /// LocalizationSpace) whose size matches the tolerance, and a dot showing where the head points
    /// (drawn from the listening position, so it meets the target exactly when the head is aligned). When both yaw and
    /// pitch are within the tolerance the target turns green; once that has been held for
    /// <see cref="holdMs"/> the participant counts as aligned and the stimulus may play.
    ///
    /// Tolerance and hold time are sent by Max with each trial (config: alignmentToleranceDeg,
    /// alignmentHoldMs); the values here are the defaults / editor preview.
    /// Built in the editor (see <see cref="LocalizationVisualBuilder"/>).
    /// </summary>
    public class LocalizationAlignment : LocalizationVisualBuilder
    {
        [Tooltip("Participant's head (main camera). Taken from the LocalizationPointer if empty.")]
        public Transform head;

        [Header("Check")]
        [Tooltip("Allowed deviation of head yaw and pitch from 0, degrees.")]
        public float toleranceDeg = 5f;
        [Tooltip("How long the head must stay within tolerance before counting as aligned, ms.")]
        public float holdMs = 500f;

        [Header("Look")]
        [Tooltip("Distance of the target and gaze dot from the eyes, metres.")]
        public float distance = 1.5f;
        public float lineWidth = 0.006f;
        public float gazeDotSize = 0.025f;
        public Color idleColor = new Color(1f, 1f, 1f, 0.8f);
        public Color withinColor = new Color(1f, 0.85f, 0.3f, 1f);
        public Color alignedColor = new Color(0.34f, 0.78f, 0.58f, 1f);
        public Color gazeColor = new Color(1f, 1f, 1f, 0.95f);

        /// <summary>Head yaw (left positive) and pitch (up positive) relative to the reference frame, degrees.</summary>
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        /// <summary>Currently within tolerance.</summary>
        public bool IsWithin { get; private set; }

        /// <summary>Within tolerance for at least holdMs.</summary>
        public bool IsAligned { get; private set; }

        [SerializeField, HideInInspector] private Transform _target;
        [SerializeField, HideInInspector] private LineRenderer _targetRing;
        [SerializeField, HideInInspector] private LineRenderer _crossH;
        [SerializeField, HideInInspector] private LineRenderer _crossV;
        [SerializeField, HideInInspector] private Transform _gazeDot;

        private double _withinSince = -1;
        private Color _lastColor;

        protected override string VisualsName => "Alignment Visuals";

        public void Configure(float tolerance, float hold)
        {
            if (tolerance > 0f) toleranceDeg = tolerance;
            if (hold >= 0f) holdMs = hold;
            ScaleTarget();
            ResetHold();
        }

        public void ResetHold()
        {
            _withinSince = -1;
            IsAligned = false;
        }

        public void SetVisible(bool visible)
        {
            if (visualsRoot != null) visualsRoot.gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            Transform h = ResolveHead(head);
            if (h == null) return;

            LocalizationVisuals.YawPitch(h, AngleFrame(), out float yaw, out float pitch);
            Yaw = yaw;
            Pitch = pitch;
            IsWithin = Mathf.Abs(yaw) <= toleranceDeg && Mathf.Abs(pitch) <= toleranceDeg;

            double now = Time.realtimeSinceStartupAsDouble;
            if (!IsWithin) _withinSince = -1;
            else if (_withinSince < 0) _withinSince = now;
            IsAligned = IsWithin && (now - _withinSince) * 1000.0 >= holdMs;

            if (visualsRoot == null) return;

            // Fixed at the listening position; the target sits straight ahead. The gaze dot is drawn from the
            // listening position along the head direction, so it meets the target exactly when aligned.
            Vector3 eye = EyePosition();
            Place(visualsRoot, eye, FrontRotation());
            if (_gazeDot != null)
                _gazeDot.position = eye + h.forward * distance;

            Color c = IsAligned ? alignedColor : IsWithin ? withinColor : idleColor;
            if (c != _lastColor)
            {
                _lastColor = c;
                LocalizationVisuals.SetColor(_targetRing, c);
                LocalizationVisuals.SetColor(_crossH, c);
                LocalizationVisuals.SetColor(_crossV, c);
            }
        }

        private void ScaleTarget()
        {
            if (_target == null) return;
            // Target ring radius corresponds to the tolerance angle at the given distance.
            float r = distance * Mathf.Tan(Mathf.Max(0.5f, toleranceDeg) * Mathf.Deg2Rad);
            _target.localScale = new Vector3(r, r, 1f);
        }

        protected override void Build(Transform root)
        {
            // Target: unit circle + cross in its own transform, scaled to the tolerance.
            _target = new GameObject("Target").transform;
            _target.SetParent(root, false);
            _target.localPosition = Vector3.forward * distance;

            _targetRing = Line(_target, "ToleranceRing", lineWidth, idleColor, true);
            _targetRing.positionCount = 48;
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2f / 48f;
                _targetRing.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));
            }

            // Cross hairs extend beyond the ring so the target is visible at small tolerances.
            _crossH = Line(_target, "CrossH", lineWidth, idleColor, false);
            _crossH.positionCount = 2;
            _crossH.SetPosition(0, new Vector3(-2f, 0f, 0f));
            _crossH.SetPosition(1, new Vector3(2f, 0f, 0f));
            _crossV = Line(_target, "CrossV", lineWidth, idleColor, false);
            _crossV.positionCount = 2;
            _crossV.SetPosition(0, new Vector3(0f, -2f, 0f));
            _crossV.SetPosition(1, new Vector3(0f, 2f, 0f));

            _gazeDot = Dot(root, "GazeDot", gazeDotSize, gazeColor);
            _gazeDot.localPosition = Vector3.forward * distance;

            _lastColor = idleColor;
            ScaleTarget();
        }
    }
}
