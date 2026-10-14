using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QExE.Localization
{
    /// <summary>
    /// One pointing response, expressed as azimuth / elevation in degrees.
    /// Convention: azimuth 0 = straight ahead, positive = to the LEFT (counter-clockwise seen from
    /// above), range (-180, 180]. Elevation positive = up, range [-90, 90].
    /// </summary>
    [Serializable]
    public struct PointingSample
    {
        /// <summary>Direction relative to the reference frame (room / XR origin).</summary>
        public float Azimuth;
        public float Elevation;

        /// <summary>Direction relative to the participant's head at the moment of the response.</summary>
        public float AzimuthHead;
        public float ElevationHead;

        /// <summary>Head orientation relative to the reference frame at the moment of the response.</summary>
        public float HeadYaw;
        public float HeadPitch;

        /// <summary>Pointed position on the response sphere, world space.</summary>
        public Vector3 WorldPoint;
    }

    /// <summary>
    /// XR-toolkit independent pointing device for localization responses.
    ///
    /// Needs only two transforms: the pointer origin (any tracked controller / hand / gaze transform,
    /// pointing along its +Z axis) and the head (the camera). The pointed direction is where the
    /// pointer ray hits a virtual sphere centred on the head, which removes the parallax between hand
    /// and head. The response is confirmed with an Input System action (trigger by default).
    /// </summary>
    public class LocalizationPointer : MonoBehaviour
    {
        [Header("Tracking")]
        [Tooltip("Transform the participant points with (+Z is the pointing direction). Usually the right controller.")]
        public Transform pointerOrigin;

        [Tooltip("The participant's head, usually the main camera.")]
        public Transform head;

        [Tooltip("Only used when the scene has no LocalizationSpace: frame the azimuth/elevation are expressed in. Empty = world.")]
        public Transform referenceFrame;

        [Tooltip("Centre the response sphere on the listening position (LocalizationSpace) instead of the moving head.")]
        public bool centreOnListeningPosition = true;

        [Tooltip("Radius of the virtual response sphere around the head, in metres. Match the stimulus distance if known.")]
        [Min(0.1f)]
        public float sphereRadius = 2.0f;

        [Header("Input")]
        [Tooltip("Confirms a response. Either assign an action from an Input Action Asset or edit the embedded bindings.")]
        public InputActionProperty confirmAction = new InputActionProperty(CreateDefaultConfirmAction());

        [Header("Visuals")]
        public bool showRay = true;
        public bool showReticle = true;
        public float rayWidth = 0.005f;
        public float reticleSize = 0.05f;
        public Color armedColor = new Color(0.34f, 0.78f, 0.58f, 1f);
        public Color disarmedColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);

        [Header("Dial (optional)")]
        [Tooltip("Top-down azimuth dial. When the ray is on the dial, the trigger sets the dial instead of confirming.")]
        public LocalizationDial dial;

        /// <summary>Raised when the trigger is pressed while the pointer is armed (ray not on the dial).</summary>
        public event Action<PointingSample> Confirmed;

        /// <summary>
        /// Raised when the trigger is pressed while the ray is on the dial (armed or not):
        /// azimuth of the pressed point and whether it was inside the dial's centre zone.
        /// </summary>
        public event Action<float, bool> DialPressed;

        /// <summary>When armed, a confirm press produces a response. Visuals show the armed state.</summary>
        public bool Armed { get; private set; }

        /// <summary>Whether the pointer visuals are shown at all (e.g. hidden outside a block).</summary>
        public bool VisualsVisible { get; private set; } = true;

        private LineRenderer _line;
        private Transform _reticle;
        private Material _material;

        // Extra things the ray can press (e.g. the A/B buttons of paired localization). Empty by default.
        private readonly System.Collections.Generic.List<ILocalizationPressable> _pressables =
            new System.Collections.Generic.List<ILocalizationPressable>();
        private ILocalizationPressable _hoveredPressable;

        /// <summary>Adds a pressable that is hit-tested before the dial (see <see cref="ILocalizationPressable"/>).</summary>
        public void RegisterPressable(ILocalizationPressable pressable)
        {
            if (pressable != null && !_pressables.Contains(pressable)) _pressables.Add(pressable);
        }

        public void UnregisterPressable(ILocalizationPressable pressable)
        {
            if (pressable == null) return;
            _pressables.Remove(pressable);
            if (_hoveredPressable == pressable) _hoveredPressable = null;
            pressable.SetHover(false, Vector3.zero);
        }

        private static InputAction CreateDefaultConfirmAction()
        {
            var action = new InputAction("QExE Localization Confirm", InputActionType.Button);
            action.AddBinding("<XRController>{RightHand}/triggerPressed");
            action.AddBinding("<XRController>{RightHand}/trigger");
            action.AddBinding("<XRController>{LeftHand}/triggerPressed");
            action.AddBinding("<XRController>{LeftHand}/trigger");
            action.AddBinding("<Keyboard>/space"); // desktop testing
            return action;
        }

        private void Awake()
        {
            if (head == null && Camera.main != null)
                head = Camera.main.transform;
            if (dial == null)
                dial = GetComponent<LocalizationDial>();

            CreateVisuals();
            SetArmed(false);
        }

        private void OnEnable()
        {
            confirmAction.action?.Enable();
        }

        private void OnDisable()
        {
            // Only disable actions owned by this component; referenced asset actions may be shared.
            if (confirmAction.reference == null)
                confirmAction.action?.Disable();
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }

        public void SetArmed(bool armed)
        {
            Armed = armed;
            ApplyColor(armed ? armedColor : disarmedColor);
        }

        public void SetVisualsVisible(bool visible)
        {
            VisualsVisible = visible;
        }

        private void Update()
        {
            bool haveTracking = pointerOrigin != null && head != null;
            PointingSample sample = default;
            bool onDial = false;
            Vector3 dialPoint = Vector3.zero;
            float dialAzimuth = 0f;
            bool dialCentre = false;

            ILocalizationPressable pressable = null;
            Vector3 pressablePoint = Vector3.zero;

            if (haveTracking)
            {
                sample = ComputeSample();
                for (int i = 0; i < _pressables.Count && pressable == null; i++)
                {
                    if (_pressables[i] != null && _pressables[i].TryHit(pointerOrigin.position, pointerOrigin.forward, out pressablePoint))
                        pressable = _pressables[i];
                }
                onDial = pressable == null && dial != null && dial.isActiveAndEnabled &&
                         dial.Raycast(pointerOrigin.position, pointerOrigin.forward, out dialPoint, out dialAzimuth, out dialCentre);
            }

            if (_hoveredPressable != null && _hoveredPressable != pressable) _hoveredPressable.SetHover(false, Vector3.zero);
            if (pressable != null) pressable.SetHover(true, pressablePoint);
            _hoveredPressable = pressable;

            if (dial != null) dial.SetHover(onDial, dialPoint);
            bool onPressable = pressable != null;
            UpdateVisuals(haveTracking && (VisualsVisible || onDial || onPressable),
                          onPressable ? pressablePoint : onDial ? dialPoint : sample.WorldPoint);

            bool pressed = haveTracking && confirmAction.action != null && confirmAction.action.WasPressedThisFrame();
            if (!pressed)
                return;

            if (onPressable)
            {
                pressable.Press(pressablePoint);
            }
            else if (onDial)
            {
                DialPressed?.Invoke(dialAzimuth, dialCentre);
            }
            else if (Armed)
            {
                // The controller decides what a press means (mark a candidate or submit) and disarms.
                Confirmed?.Invoke(sample);
            }
        }

        /// <summary>Computes the current pointing direction without waiting for a confirm press.</summary>
        public PointingSample ComputeSample()
        {
            Vector3 centre = SphereCentre;
            Vector3 origin = pointerOrigin.position;
            Vector3 dir = pointerOrigin.forward;

            // Far intersection of the pointer ray with the sphere around the head. The pointer
            // normally starts inside the sphere, so there is always exactly one hit in front of it.
            Vector3 oc = origin - centre;
            float bHalf = Vector3.Dot(dir, oc);
            float disc = bHalf * bHalf - (oc.sqrMagnitude - sphereRadius * sphereRadius);
            float t = disc >= 0f ? -bHalf + Mathf.Sqrt(disc) : -1f;
            Vector3 point = t > 0f ? origin + dir * t : centre + dir * sphereRadius;

            return SampleFromDirection((point - centre).normalized);
        }

        /// <summary>Builds a sample for a world-space direction seen from the head.</summary>
        public PointingSample SampleFromDirection(Vector3 worldDirection)
        {
            Vector3 response = worldDirection.normalized;
            var sample = new PointingSample { WorldPoint = SphereCentre + response * sphereRadius };
            ToAzEl(ToReference(response), out sample.Azimuth, out sample.Elevation);
            ToAzEl(head.InverseTransformDirection(response), out sample.AzimuthHead, out sample.ElevationHead);
            ToAzEl(ToReference(head.forward), out sample.HeadYaw, out sample.HeadPitch);
            return sample;
        }

        /// <summary>World direction for an azimuth / elevation in the reference frame.</summary>
        public Vector3 DirectionFromAzEl(float azimuth, float elevation)
        {
            float el = elevation * Mathf.Deg2Rad;
            Vector3 local = LocalizationVisuals.AzimuthToLocalDirection(azimuth) * Mathf.Cos(el) + Vector3.up * Mathf.Sin(el);
            Transform frame = Frame;
            return frame != null ? frame.TransformDirection(local) : local;
        }

        /// <summary>Frame for azimuth / elevation: the LocalizationSpace if there is one, else referenceFrame.</summary>
        public Transform Frame
        {
            get
            {
                if (_space == null) _space = LocalizationSpace.Find(this);
                return _space != null ? _space.Frame : referenceFrame;
            }
        }

        /// <summary>Centre of the response sphere: the listening position, or the head.</summary>
        public Vector3 SphereCentre
        {
            get
            {
                if (_space == null) _space = LocalizationSpace.Find(this);
                return centreOnListeningPosition && _space != null ? _space.EyePosition : head.position;
            }
        }

        private LocalizationSpace _space;

        private Vector3 ToReference(Vector3 worldDirection)
        {
            Transform frame = Frame;
            return frame != null ? frame.InverseTransformDirection(worldDirection) : worldDirection;
        }

        /// <summary>Azimuth (left positive) and elevation in degrees of a direction vector.</summary>
        public static void ToAzEl(Vector3 v, out float azimuth, out float elevation)
        {
            v.Normalize();
            azimuth = Mathf.Atan2(-v.x, v.z) * Mathf.Rad2Deg;
            elevation = Mathf.Asin(Mathf.Clamp(v.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        // ------------------------------------------------------------------------------------------
        // Visuals (created at runtime so the component works without any extra assets)
        // ------------------------------------------------------------------------------------------

        private void CreateVisuals()
        {
            _material = new Material(Shader.Find("Sprites/Default"));

            var lineObject = new GameObject("LocalizationRay");
            lineObject.transform.SetParent(transform, false);
            _line = lineObject.AddComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.widthMultiplier = rayWidth;
            _line.sharedMaterial = _material;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;

            var reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticle.name = "LocalizationReticle";
            Destroy(reticle.GetComponent<Collider>());
            reticle.transform.SetParent(transform, false);
            reticle.transform.localScale = Vector3.one * reticleSize;
            var renderer = reticle.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _reticle = reticle.transform;
        }

        private void UpdateVisuals(bool visible, Vector3 endPoint)
        {
            _line.enabled = visible && showRay;
            _reticle.gameObject.SetActive(visible && showReticle);
            if (!visible)
                return;

            _line.widthMultiplier = rayWidth;
            _line.SetPosition(0, pointerOrigin.position);
            _line.SetPosition(1, endPoint);
            _reticle.position = endPoint;
            _reticle.localScale = Vector3.one * reticleSize;
        }

        private void ApplyColor(Color color)
        {
            if (_line != null)
            {
                _line.startColor = color;
                _line.endColor = color;
            }
            if (_material != null)
                _material.color = Color.white;
            if (_reticle != null)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_Color", color);
                _reticle.GetComponent<MeshRenderer>().SetPropertyBlock(block);
            }
        }
    }
}
