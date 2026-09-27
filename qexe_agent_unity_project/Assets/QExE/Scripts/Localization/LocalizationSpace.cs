using UnityEngine;
using UnityEngine.InputSystem;

namespace QExE.Localization
{
    /// <summary>
    /// The listening position of the localization test: a fixed point in the room where the
    /// participant sits. All localization visuals (head ring, dial, alignment target) are placed
    /// relative to it, and its forward direction defines "front" (azimuth 0) for the visuals and for
    /// the recorded azimuth / elevation.
    ///
    /// Put this on the QExELocalization object and move / rotate that object in the editor: the
    /// object's position is the floor point under the participant's head, <see cref="eyeHeight"/>
    /// above it is the centre of the ring, and the blue axis (forward) is the front.
    ///
    /// Optionally, the position can be calibrated to the participant's actual head position
    /// (key <see cref="calibrateKey"/> in Play mode, the component's context menu, or automatically
    /// when the participant presses Start).
    /// </summary>
    [ExecuteAlways]
    public class LocalizationSpace : MonoBehaviour
    {
        [Tooltip("Eye height above this object's position, metres (seated: about 1.2).")]
        public float eyeHeight = 1.2f;

        [Header("Calibration (Play mode)")]
        [Tooltip("Move the listening position to the participant's head when they press Start.")]
        public bool calibrateOnStart = false;
        [Tooltip("Also turn 'front' to the participant's current viewing direction when calibrating.")]
        public bool calibrateYaw = false;
        [Tooltip("Operator key that calibrates to the current head position.")]
        public Key calibrateKey = Key.C;
        [Tooltip("Head to calibrate to. Taken from the LocalizationPointer if empty.")]
        public Transform head;

        /// <summary>Centre of the listening position at eye height, world space.</summary>
        public Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

        /// <summary>Yaw-only rotation that defines front.</summary>
        public Quaternion FrontRotation => LocalizationVisuals.ReferenceYaw(transform);

        /// <summary>Frame for azimuth / elevation (this transform).</summary>
        public Transform Frame => transform;

        private void Update()
        {
            if (!Application.isPlaying) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[calibrateKey].wasPressedThisFrame)
                CalibrateToHead();
        }

        /// <summary>Moves the listening position (and optionally front) to the participant's head.</summary>
        [ContextMenu("Calibrate To Current Head")]
        public void CalibrateToHead()
        {
            Transform h = head;
            if (h == null)
            {
                var pointer = GetComponent<LocalizationPointer>();
                if (pointer != null) h = pointer.head;
            }
            if (h == null && Camera.main != null) h = Camera.main.transform;
            if (h == null)
            {
                Debug.LogWarning("QExE Localization: no head to calibrate to.");
                return;
            }

            transform.position = h.position - Vector3.up * eyeHeight;
            if (calibrateYaw)
            {
                Vector3 f = Vector3.ProjectOnPlane(h.forward, Vector3.up);
                if (f.sqrMagnitude > 1e-6f)
                    transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
            }
            Debug.Log($"<color=cyan><b>QExE:loc:</b></color> listening position calibrated to {EyePosition} (front yaw {transform.eulerAngles.y:F1}°)");
        }

        private void OnDrawGizmos()
        {
            Vector3 eye = EyePosition;
            Gizmos.color = new Color(0.34f, 0.78f, 0.58f, 1f);
            Gizmos.DrawWireSphere(eye, 0.12f);
            Gizmos.DrawLine(transform.position, eye);
            Vector3 front = FrontRotation * Vector3.forward;
            Gizmos.DrawLine(eye, eye + front * 0.6f);
            Gizmos.DrawWireSphere(eye + front * 0.6f, 0.03f);
        }

        /// <summary>The space in the scene (the one on the same object first).</summary>
        public static LocalizationSpace Find(Component context)
        {
            var space = context != null ? context.GetComponent<LocalizationSpace>() : null;
            return space != null ? space : FindFirstObjectByType<LocalizationSpace>();
        }
    }
}
