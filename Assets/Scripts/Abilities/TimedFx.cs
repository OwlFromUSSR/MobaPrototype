using UnityEngine;

namespace Moba
{
    /// <summary>Простой визуальный эффект: расширяется по горизонтали и удаляется.</summary>
    public class TimedFx : MonoBehaviour
    {
        float life = 0.4f;
        float age;
        float targetDiameter = 1f;
        float height;

        public void Init(float diameter, float lifetime)
        {
            targetDiameter = diameter;
            life = lifetime;
            height = transform.localScale.y;
            transform.localScale = new Vector3(0.1f, height, 0.1f);
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / life);
            float d = Mathf.Lerp(0.1f, targetDiameter, t);
            transform.localScale = new Vector3(d, height, d);
            if (age >= life) Destroy(gameObject);
        }
    }
}
