using UnityEngine;

namespace Moba
{
    /// <summary>
    /// Снаряд. Режим Homing — преследует цель (обычные дальние атаки).
    /// Режим Linear — летит по прямой и бьёт первого врага на пути (способность-скиллшот).
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        enum Mode { Homing, Linear }

        Mode mode;
        Unit source;
        Unit target;
        float damage;
        float speed;
        Vector3 direction;
        float remaining;
        float hitRadius;
        bool launched;

        public void LaunchHoming(Unit src, Unit tgt, float dmg, float spd)
        {
            mode = Mode.Homing;
            source = src;
            target = tgt;
            damage = dmg;
            speed = spd;
            launched = true;
        }

        public void LaunchLinear(Unit src, Vector3 dir, float dmg, float spd, float range, float radius)
        {
            mode = Mode.Linear;
            source = src;
            damage = dmg;
            speed = spd;
            direction = dir.normalized;
            remaining = range;
            hitRadius = radius;
            launched = true;
        }

        void Update()
        {
            if (!launched) return;
            if (mode == Mode.Homing) UpdateHoming(Time.deltaTime);
            else UpdateLinear(Time.deltaTime);
        }

        void UpdateHoming(float dt)
        {
            if (target == null || target.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 aim = target.transform.position + Vector3.up * 1f;
            transform.position = Vector3.MoveTowards(transform.position, aim, speed * dt);

            if (target.EdgeDistance(transform.position) <= 0.3f)
            {
                target.Health.TakeDamage(damage, source);
                Destroy(gameObject);
            }
        }

        void UpdateLinear(float dt)
        {
            float step = speed * dt;
            transform.position += direction * step;
            remaining -= step;

            TeamId ownTeam = source != null ? source.team : TeamId.Blue;
            foreach (var u in Unit.All)
            {
                if (u.team == ownTeam || u.IsDead) continue;
                if (Unit.FlatDistance(transform.position, u.transform.position) <= hitRadius + u.Radius)
                {
                    u.Health.TakeDamage(damage, source);
                    Destroy(gameObject);
                    return;
                }
            }

            if (remaining <= 0f) Destroy(gameObject);
        }
    }
}
