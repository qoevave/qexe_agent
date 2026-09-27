using UnityEngine;

namespace QExE.Localization
{
    /// <summary>
    /// Stores a colour for a renderer that uses the shared localization material and applies it as a
    /// MaterialPropertyBlock. Property blocks are not saved with the scene, so this component
    /// re-applies the colour whenever the object is loaded or enabled (in the editor and in Play mode).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class LocalizationTint : MonoBehaviour
    {
        public Color color = Color.white;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        public void Set(Color newColor)
        {
            color = newColor;
            Apply();
        }

        public void Apply()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer == null) return;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(ColorId, color);      // Sprites/Default, legacy shaders
            block.SetColor(BaseColorId, color);  // URP Lit / Unlit, if a custom material is assigned
            renderer.SetPropertyBlock(block);
        }
    }
}
