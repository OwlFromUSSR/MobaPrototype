using UnityEngine;

namespace Moba
{
    /// <summary>Атака по цели: проверка дальности, перезарядка, мгновенный урон или снаряд.</summary>
    public class Combat : MonoBehaviour
    {
        Unit unit;
        float nextAttackTime;

        void Awake()
        {
            unit = GetComponent<Unit>();
        }

        public bool InRange(Unit target)
        {
            return target != null && !target.IsDead &&
                   target.EdgeDistance(transform.position) <= unit.config.attackRange;
        }

        /// <summary>Возвращает true, если цель в зоне атаки (даже если атака ещё на перезарядке).</summary>
        public bool Attack(Unit target)
        {
            if (!InRange(target)) return false;

            FaceTowards(target.transform.position);
            if (Time.time < nextAttackTime) return true;
            nextAttackTime = Time.time + unit.config.attackCooldown;

            UnitConfig cfg = unit.config;
            if (cfg.projectilePrefab != null)
            {
                Vector3 origin = transform.position + Vector3.up * cfg.projectileHeight;
                Projectile p = Instantiate(cfg.projectilePrefab, origin, Quaternion.identity);
                p.LaunchHoming(unit, target, cfg.attackDamage, cfg.projectileSpeed);
            }
            else
            {
                target.Health.TakeDamage(cfg.attackDamage, unit);
            }
            return true;
        }

        void FaceTowards(Vector3 point)
        {
            Vector3 dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion look = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 720f * Time.deltaTime);
        }
    }
}
