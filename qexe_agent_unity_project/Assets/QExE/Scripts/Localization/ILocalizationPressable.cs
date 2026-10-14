using UnityEngine;

namespace QExE.Localization
{
    /// <summary>
    /// Something the <see cref="LocalizationPointer"/> ray can press besides the dial, e.g. the A/B
    /// buttons of paired localization. Register it with <see cref="LocalizationPointer.RegisterPressable"/>.
    /// Pressables are tested before the dial; a press on a pressable never marks a direction.
    /// </summary>
    public interface ILocalizationPressable
    {
        /// <summary>Hit test against a world-space ray. Returns true and the hit point if the ray is on a pressable part.</summary>
        bool TryHit(Vector3 origin, Vector3 direction, out Vector3 worldPoint);

        /// <summary>Hover feedback (called every frame with the current hit state).</summary>
        void SetHover(bool hovering, Vector3 worldPoint);

        /// <summary>The trigger was pressed while the ray was on it, at <paramref name="worldPoint"/>.</summary>
        void Press(Vector3 worldPoint);
    }
}
