using System;
using UnityEngine;

namespace Moba
{
    public class Health : MonoBehaviour
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsDead { get; private set; }
        public float Percent => Max > 0f ? Current / Max : 0f;

        /// <summary>(фактический урон, источник)</summary>
        public event Action<float, Unit> Damaged;
        /// <summary>(убийца, может быть null)</summary>
        public event Action<Unit> Died;

        public void Init(float max)
        {
            Max = max;
            Current = max;
            IsDead = false;
        }

        public void TakeDamage(float amount, Unit source)
        {
            if (IsDead || amount <= 0f) return;

            float applied = Mathf.Min(amount, Current);
            Current -= applied;
            Damaged?.Invoke(applied, source);

            if (Current <= 0f)
            {
                Current = 0f;
                IsDead = true;
                Died?.Invoke(source);
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(Max, Current + amount);
        }

        public void Revive()
        {
            IsDead = false;
            Current = Max;
        }
    }
}
