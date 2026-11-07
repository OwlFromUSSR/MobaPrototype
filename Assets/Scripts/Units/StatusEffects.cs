using UnityEngine;

namespace Moba
{
    /// <summary>Временные эффекты. Сейчас только замедление.</summary>
    public class StatusEffects : MonoBehaviour
    {
        float slowFactor = 1f;
        float slowEnd;

        public float SpeedMultiplier => Time.time < slowEnd ? slowFactor : 1f;

        public void ApplySlow(float factor, float duration)
        {
            if (Time.time >= slowEnd || factor < slowFactor) slowFactor = factor;
            slowEnd = Mathf.Max(slowEnd, Time.time + duration);
        }
    }
}
