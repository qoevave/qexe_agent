using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QExE.Localization
{
    /// <summary>
    /// Helpers to draw lines, markers and labels for the localization visuals. Works in any render
    /// pipeline (uses the built-in Sprites/Default shader with vertex / tint colours).
    /// </summary>
    public static class LocalizationVisuals
    {
        public const string SharedMaterialPath = "Assets/QExE/Materials/Localization/QExE_LocalizationLines.mat";

        private static Material _sharedMaterial;

        /// <summary>
        /// Shared line material. In the editor it is a material asset (so generated objects saved in
        /// the scene keep it); in a build without that asset a runtime material is created.
        /// </summary>
        public static Material SharedMaterial
        {
            get
            {
                if (_sharedMaterial != null) return _sharedMaterial;
#if UNITY_EDITOR
                _sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SharedMaterialPath);
                if (_sharedMaterial == null && !Application.isPlaying)
                {
                    var folder = System.IO.Path.GetDirectoryName(SharedMaterialPath).Replace('\\', '/');
                    EnsureFolder(folder);
                    _sharedMaterial = new Material(Shader.Find("Sprites/Default")) { name = "QExE_LocalizationLines" };
                    AssetDatabase.CreateAsset(_sharedMaterial, SharedMaterialPath);
                    AssetDatabase.SaveAssets();
                }
#endif
                if (_sharedMaterial == null)
                    _sharedMaterial = new Material(Shader.Find("Sprites/Default")) { name = "QExE Localization Lines (runtime)" };
                return _sharedMaterial;
            }
        }

#if UNITY_EDITOR
        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
#endif

        public static LineRenderer CreateLine(Transform parent, string name, float width, Color color, bool loop,
                                              bool worldSpace = false, Material material = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = worldSpace;
            line.loop = loop;
            line.widthMultiplier = width;
            line.sharedMaterial = material != null ? material : SharedMaterial;
            line.startColor = color;
            line.endColor = color;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 0;
            return line;
        }

        public static void SetColor(LineRenderer line, Color color)
        {
            if (line == null) return;
            line.startColor = color;
            line.endColor = color;
        }

        public static Transform CreateDot(Transform parent, string name, float size, Color color, Material material = null)
        {
            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = name;
            var collider = dot.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
            dot.transform.SetParent(parent, false);
            dot.transform.localScale = Vector3.one * size;
            var renderer = dot.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material != null ? material : SharedMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SetColor(dot.transform, color);
            return dot.transform;
        }

        /// <summary>
        /// Colours a mesh object (dot, disc). Uses a <see cref="LocalizationTint"/> component so the
        /// colour survives saving and reloading the scene (property blocks alone are not saved).
        /// </summary>
        public static void SetColor(Transform dot, Color color)
        {
            if (dot == null || dot.GetComponent<Renderer>() == null) return;
            var tint = dot.GetComponent<LocalizationTint>();
            if (tint == null) tint = dot.gameObject.AddComponent<LocalizationTint>();
            tint.Set(color);
        }

        /// <summary>
        /// World-space TextMeshPro label, readable when looking along the parent's +Z axis.
        /// <paramref name="height"/> is the approximate line height in metres.
        /// </summary>
        public static TextMeshPro CreateLabel(Transform parent, string name, string text, float height, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = height * 10f;   // TMP 3D: font size 10 is about 1 unit tall
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(height * 12f, height * 1.5f);
            var renderer = label.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return label;
        }

        /// <summary>
        /// Unit direction for an azimuth in the horizontal plane of a frame whose forward is +Z.
        /// Azimuth convention: 0 = front, positive = left.
        /// </summary>
        public static Vector3 AzimuthToLocalDirection(float azimuthDeg)
        {
            float a = azimuthDeg * Mathf.Deg2Rad;
            return new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
        }

        /// <summary>Yaw-only rotation of the reference frame (world if null).</summary>
        public static Quaternion ReferenceYaw(Transform referenceFrame)
        {
            if (referenceFrame == null) return Quaternion.identity;
            Vector3 f = Vector3.ProjectOnPlane(referenceFrame.forward, Vector3.up);
            return f.sqrMagnitude < 1e-6f ? Quaternion.identity : Quaternion.LookRotation(f.normalized, Vector3.up);
        }

        /// <summary>Yaw (left positive) and pitch (up positive) of a transform's forward in the reference frame, degrees.</summary>
        public static void YawPitch(Transform t, Transform referenceFrame, out float yaw, out float pitch)
        {
            Vector3 f = referenceFrame != null ? referenceFrame.InverseTransformDirection(t.forward) : t.forward;
            f.Normalize();
            yaw = Mathf.Atan2(-f.x, f.z) * Mathf.Rad2Deg;
            pitch = Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg;
        }
    }
}
