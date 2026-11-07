using UnityEngine;

namespace Moba
{
    /// <summary>Камера «сверху под углом»: следует за героем с фиксированным смещением.</summary>
    public class TopDownCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 20f, -14f);
        public float smoothing = 8f;

        void Start()
        {
            transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            if (target != null) transform.position = target.position + offset;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, target.position + offset, t);
        }
    }
}
