using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QExE.Localization
{
    /// <summary>
    /// Base class for localization visuals that are built as real child objects in the scene.
    ///
    /// The visuals are generated in the editor (and saved with the scene), so they can be seen and
    /// inspected without entering Play mode. Changing a setting on the component in the Inspector
    /// regenerates them immediately in edit mode (live preview); in Play mode use "Rebuild Visuals". Use the component's context menu
    /// "Rebuild Visuals" to force it.
    ///
    /// Generated objects live under a child called "&lt;Component&gt; Visuals" and are replaced on every
    /// rebuild, so tweak them through the component's settings (or the shared material asset), not
    /// by editing the generated children directly.
    /// </summary>
    [ExecuteAlways]
    public abstract class LocalizationVisualBuilder : MonoBehaviour
    {
        [Tooltip("Material for all lines and markers. Empty = the shared QExE localization material.")]
        public Material lineMaterial;

        [SerializeField, HideInInspector]
        protected Transform visualsRoot;

        protected abstract string VisualsName { get; }

        /// <summary>Create all visual objects under <paramref name="root"/> and store references to them.</summary>
        protected abstract void Build(Transform root);

        public Material EffectiveMaterial => lineMaterial != null ? lineMaterial : LocalizationVisuals.SharedMaterial;

        protected virtual void OnEnable()
        {
            if (visualsRoot == null)
                Rebuild();
        }

        /// <summary>Destroys and recreates the generated visuals.</summary>
        [ContextMenu("Rebuild Visuals")]
        public void Rebuild()
        {
#if UNITY_EDITOR
            if (PrefabUtility.IsPartOfPrefabAsset(this))
                return;
#endif
            if (visualsRoot != null)
                DestroyObject(visualsRoot.gameObject);

            // Also remove orphans with the same name (e.g. after a lost reference).
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == VisualsName)
                    DestroyObject(child.gameObject);
            }

            visualsRoot = new GameObject(VisualsName).transform;
            visualsRoot.SetParent(transform, false);
            Build(visualsRoot);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                if (gameObject.scene.IsValid())
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            // Objects cannot be created inside OnValidate, so rebuild right after it.
            EditorApplication.delayCall -= DelayedRebuild;
            EditorApplication.delayCall += DelayedRebuild;
        }

        private void DelayedRebuild()
        {
            // Edit mode only: rebuilding in Play mode would reset the state of a running block.
            if (this == null || Application.isPlaying || !isActiveAndEnabled || PrefabUtility.IsPartOfPrefabAsset(this))
                return;
            Rebuild();
        }
#endif

        protected static void DestroyObject(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        // ------------------------------------------------------------------------------------------
        // Helpers for derived classes
        // ------------------------------------------------------------------------------------------

        protected LineRenderer Line(Transform parent, string name, float width, Color color, bool loop)
        {
            return LocalizationVisuals.CreateLine(parent, name, width, color, loop, false, EffectiveMaterial);
        }

        protected Transform Dot(Transform parent, string name, float size, Color color)
        {
            return LocalizationVisuals.CreateDot(parent, name, size, color, EffectiveMaterial);
        }

        /// <summary>Moves the visuals root, but only when it actually changes (keeps the scene clean in edit mode).</summary>
        protected static void Place(Transform t, Vector3 position, Quaternion rotation)
        {
            if ((t.position - position).sqrMagnitude > 1e-8f || Quaternion.Angle(t.rotation, rotation) > 0.01f)
                t.SetPositionAndRotation(position, rotation);
        }

        /// <summary>Head transform: explicit, else from the pointer, else the main camera.</summary>
        protected Transform ResolveHead(Transform explicitHead)
        {
            if (explicitHead != null) return explicitHead;
            var pointer = GetComponent<LocalizationPointer>();
            if (pointer != null && pointer.head != null) return pointer.head;
            return Camera.main != null ? Camera.main.transform : null;
        }

        /// <summary>Listening position at eye height (from LocalizationSpace; fallback: this object + 1.2 m).</summary>
        protected Vector3 EyePosition()
        {
            var space = LocalizationSpace.Find(this);
            return space != null ? space.EyePosition : transform.position + Vector3.up * 1.2f;
        }

        /// <summary>Yaw rotation that defines front (from LocalizationSpace; fallback: this object).</summary>
        protected Quaternion FrontRotation()
        {
            var space = LocalizationSpace.Find(this);
            return space != null ? space.FrontRotation : LocalizationVisuals.ReferenceYaw(transform);
        }

        /// <summary>Frame used for yaw / pitch (the LocalizationSpace; fallback: this object).</summary>
        protected Transform AngleFrame()
        {
            var space = LocalizationSpace.Find(this);
            return space != null ? space.Frame : transform;
        }
    }
}
