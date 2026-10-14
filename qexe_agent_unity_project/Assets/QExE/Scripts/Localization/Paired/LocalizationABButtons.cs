using System;
using TMPro;
using UnityEngine;

namespace QExE.Localization
{
    /// <summary>
    /// The A and B buttons of paired localization: two round buttons side by side above the dial
    /// (below the dial's counter and status text), in the dial's plane. They are pressed with the
    /// pointer ray (registered with <see cref="LocalizationPointer"/> as an
    /// <see cref="ILocalizationPressable"/>) and follow the dial.
    ///
    /// Built in the editor like the other localization visuals (see <see cref="LocalizationVisualBuilder"/>).
    /// Give the dial room for them with <see cref="LocalizationDial.textExtraOffset"/> (the setup menu does
    /// this; see <see cref="RequiredTextOffset"/>).
    /// </summary>
    [DefaultExecutionOrder(100)] // placed after the dial has moved
    public class LocalizationABButtons : LocalizationVisualBuilder, ILocalizationPressable
    {
        public enum ButtonState
        {
            /// <summary>Cannot be pressed (e.g. B before A is answered).</summary>
            Disabled,
            /// <summary>Can be pressed to play.</summary>
            Enabled,
            /// <summary>Can be pressed; this is the side being answered.</summary>
            Answering,
            /// <summary>Can be pressed to replay; its answer is locked (A while answering B).</summary>
            Answered
        }

        [Tooltip("The dial the buttons sit above. Found on this object if empty.")]
        public LocalizationDial dial;

        [Header("Layout (metres, in the dial's plane)")]
        public float buttonRadius = 0.028f;
        [Tooltip("Distance between the two button centres.")]
        public float buttonSpacing = 0.09f;
        [Tooltip("Gap between the dial's top edge (incl. the front tick) and the buttons.")]
        public float gapAboveDial = 0.015f;
        [Tooltip("Hits slightly outside a button still count, as a fraction of its radius.")]
        public float hitTolerance = 0.25f;
        public float lineWidth = 0.003f;
        public float labelHeight = 0.03f;

        [Header("Colours")]
        public Color fillColor = new Color(0.12f, 0.12f, 0.12f, 0.75f);
        public Color hoverFillColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        public Color enabledColor = new Color(1f, 1f, 1f, 0.9f);
        public Color disabledColor = new Color(1f, 1f, 1f, 0.2f);
        [Tooltip("Outline of the side that is being answered.")]
        public Color answeringColor = new Color(0.34f, 0.78f, 0.58f, 1f);
        [Tooltip("Outline of a side whose answer is locked (matches the grey reference marker).")]
        public Color answeredColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);
        [Tooltip("Fill of the side that is currently audible (while comparing A and B).")]
        public Color audibleFillColor = new Color(0.2f, 0.45f, 0.35f, 0.95f);

        /// <summary>Raised when an enabled, visible button is pressed: "A" or "B".</summary>
        public event Action<string> Pressed;

        public bool Visible { get; private set; } = true;

        /// <summary>The dial's textExtraOffset needed to keep the counter / status text above the buttons.</summary>
        public float RequiredTextOffset => gapAboveDial + 2f * buttonRadius + 0.01f;

        [SerializeField, HideInInspector] private Transform[] _fills = new Transform[2];
        [SerializeField, HideInInspector] private LineRenderer[] _outlines = new LineRenderer[2];
        [SerializeField, HideInInspector] private TextMeshPro[] _labels = new TextMeshPro[2];

        private readonly ButtonState[] _states = { ButtonState.Enabled, ButtonState.Disabled };
        private int _hovered = -1;
        private int _audible = -1;

        protected override string VisualsName => "AB Buttons Visuals";

        private static int Index(string side) => side == "B" ? 1 : 0;
        private static string Side(int index) => index == 1 ? "B" : "A";

        // ------------------------------------------------------------------------------------------
        // Runtime API
        // ------------------------------------------------------------------------------------------

        public void SetVisible(bool visible)
        {
            Visible = visible;
            if (visualsRoot != null) visualsRoot.gameObject.SetActive(visible);
            if (!visible && _hovered >= 0)
            {
                int previous = _hovered;
                _hovered = -1;
                ApplyLook(previous);   // don't come back in the hover colour
            }
        }

        public void SetButton(string side, ButtonState state)
        {
            int i = Index(side);
            if (_states[i] == state) return;
            _states[i] = state;
            ApplyLook(i);
        }

        public ButtonState GetButton(string side) => _states[Index(side)];

        /// <summary>Highlights the side that is currently audible ("A", "B"), or none (null).</summary>
        public void SetAudibleSide(string side)
        {
            int audible = side == "A" ? 0 : side == "B" ? 1 : -1;
            if (audible == _audible) return;
            int previous = _audible;
            _audible = audible;
            if (previous >= 0) ApplyLook(previous);
            if (audible >= 0) ApplyLook(audible);
        }

        // ------------------------------------------------------------------------------------------
        // ILocalizationPressable
        // ------------------------------------------------------------------------------------------

        public bool TryHit(Vector3 origin, Vector3 direction, out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;
            if (!Visible || visualsRoot == null || !visualsRoot.gameObject.activeInHierarchy)
                return false;
            return HitButton(origin, direction, out worldPoint) >= 0;
        }

        public void SetHover(bool hovering, Vector3 worldPoint)
        {
            int hovered = -1;
            if (hovering && visualsRoot != null)
                hovered = ButtonAtLocal(visualsRoot.InverseTransformPoint(worldPoint));
            if (hovered == _hovered) return;
            int previous = _hovered;
            _hovered = hovered;
            if (previous >= 0) ApplyLook(previous);
            if (hovered >= 0) ApplyLook(hovered);
        }

        public void Press(Vector3 worldPoint)
        {
            if (!Visible || visualsRoot == null) return;
            int i = ButtonAtLocal(visualsRoot.InverseTransformPoint(worldPoint));
            if (i < 0 || _states[i] == ButtonState.Disabled) return; // disabled buttons swallow the press
            Pressed?.Invoke(Side(i));
        }

        // ------------------------------------------------------------------------------------------

        private int HitButton(Vector3 origin, Vector3 direction, out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;
            var plane = new Plane(visualsRoot.forward, visualsRoot.position);
            var ray = new Ray(origin, direction);
            if (!plane.Raycast(ray, out float distance) || distance <= 0f)
                return -1;
            worldPoint = ray.GetPoint(distance);
            return ButtonAtLocal(visualsRoot.InverseTransformPoint(worldPoint));
        }

        private int ButtonAtLocal(Vector3 local)
        {
            float r = buttonRadius * (1f + Mathf.Max(0f, hitTolerance));
            for (int i = 0; i < 2; i++)
            {
                Vector2 c = ButtonCentre(i);
                if ((new Vector2(local.x, local.y) - c).sqrMagnitude <= r * r) return i;
            }
            return -1;
        }

        /// <summary>Local centre of a button: A on the left, B on the right (as seen by the participant).</summary>
        private Vector2 ButtonCentre(int index)
        {
            // Local -X is the participant's left (the dial's convention: -X = left).
            return new Vector2((index == 0 ? -0.5f : 0.5f) * buttonSpacing, 0f);
        }

        private void ApplyLook(int i)
        {
            if (_outlines == null || i >= _outlines.Length || _outlines[i] == null) return;
            Color line;
            float width = lineWidth;
            switch (_states[i])
            {
                case ButtonState.Disabled: line = disabledColor; break;
                case ButtonState.Answering: line = answeringColor; width = lineWidth * 2f; break;
                case ButtonState.Answered: line = answeredColor; break;
                default: line = enabledColor; break;
            }
            LocalizationVisuals.SetColor(_outlines[i], line);
            _outlines[i].widthMultiplier = width;
            if (_labels[i] != null) _labels[i].color = _states[i] == ButtonState.Disabled ? disabledColor : enabledColor;
            bool hover = i == _hovered && _states[i] != ButtonState.Disabled;
            Color fill = i == _audible ? audibleFillColor : hover ? hoverFillColor : fillColor;
            if (_fills[i] != null) LocalizationVisuals.SetColor(_fills[i], fill);
        }

        private void LateUpdate()
        {
            if (visualsRoot == null) return;
            if (dial == null) dial = GetComponent<LocalizationDial>();
            if (dial == null || dial.VisualsRoot == null) return;

            Transform d = dial.VisualsRoot;
            float y = dial.TopEdgeLocalY + gapAboveDial + buttonRadius;
            Place(visualsRoot, d.TransformPoint(new Vector3(0f, y, 0f)), d.rotation);
        }

        // ------------------------------------------------------------------------------------------
        // Build
        // ------------------------------------------------------------------------------------------

        protected override void Build(Transform root)
        {
            if (dial == null) dial = GetComponent<LocalizationDial>();
            _fills = new Transform[2];
            _outlines = new LineRenderer[2];
            _labels = new TextMeshPro[2];

            for (int i = 0; i < 2; i++)
            {
                var button = new GameObject("Button_" + Side(i)).transform;
                button.SetParent(root, false);
                Vector2 c = ButtonCentre(i);
                button.localPosition = new Vector3(c.x, c.y, 0f);

                // Filled disc (flattened cylinder, axis = local Z), just behind the outline.
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Fill";
                DestroyObject(disc.GetComponent<Collider>());
                disc.transform.SetParent(button, false);
                disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                disc.transform.localPosition = new Vector3(0f, 0f, 0.002f);
                disc.transform.localScale = new Vector3(buttonRadius * 2f, 0.0005f, buttonRadius * 2f);
                var discRenderer = disc.GetComponent<MeshRenderer>();
                discRenderer.sharedMaterial = EffectiveMaterial;
                discRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _fills[i] = disc.transform;

                var outline = Line(button, "Outline", lineWidth, enabledColor, true);
                const int segments = 40;
                outline.positionCount = segments;
                for (int k = 0; k < segments; k++)
                {
                    float a = k * Mathf.PI * 2f / segments;
                    outline.SetPosition(k, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * buttonRadius);
                }
                _outlines[i] = outline;

                var label = LocalizationVisuals.CreateLabel(button, "Label", Side(i), labelHeight, enabledColor);
                label.fontStyle = FontStyles.Bold;
                label.transform.localPosition = new Vector3(0f, 0f, -0.001f);
                _labels[i] = label;
            }

            visualsRoot = root;
            ApplyLook(0);
            ApplyLook(1);
            SetVisible(Application.isPlaying ? Visible : true);
        }
    }
}
