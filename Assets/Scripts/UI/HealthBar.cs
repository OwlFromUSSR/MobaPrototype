using UnityEngine;

namespace Moba
{
    /// <summary>
    /// Полоска здоровья над юнитом из двух квадов (фон + заливка).
    /// Всегда повёрнута к камере. Квады создаёт Editor-билдер сцены.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        public Transform fill;
        public float width = 1.6f;
        public float height = 0.18f;

        Health health;
        Camera cam;

        void Start()
        {
            health = GetComponentInParent<Health>();
        }

        void LateUpdate()
        {
            if (health == null || fill == null) return;
            if (cam == null) cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;

            float p = Mathf.Clamp01(health.Percent);
            fill.localScale = new Vector3(Mathf.Max(0.0001f, width * p), height, 1f);
            fill.localPosition = new Vector3(-width * (1f - p) * 0.5f, 0f, -0.01f);
        }
    }
}
