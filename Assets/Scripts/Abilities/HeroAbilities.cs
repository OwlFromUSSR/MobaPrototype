using System.Collections;
using UnityEngine;

namespace Moba
{
    /// <summary>Мана, перезарядки и выполнение трёх способностей героя (слоты Q, W, E).</summary>
    public class HeroAbilities : MonoBehaviour
    {
        public AbilityData[] abilities = new AbilityData[3];
        public float maxMana = 100f;
        public float manaRegenPerSecond = 3f;

        public float Mana { get; private set; }
        public bool IsDashing { get; private set; }

        Unit unit;
        Mover mover;
        float[] readyAt;

        void Awake()
        {
            unit = GetComponent<Unit>();
            mover = GetComponent<Mover>();
            readyAt = new float[abilities.Length];
            Mana = maxMana;
        }

        void Update()
        {
            if (!unit.IsDead) Mana = Mathf.Min(maxMana, Mana + manaRegenPerSecond * Time.deltaTime);
        }

        public float CooldownRemaining(int slot)
        {
            return Mathf.Max(0f, readyAt[slot] - Time.time);
        }

        public void RestoreMana(float amount)
        {
            Mana = Mathf.Min(maxMana, Mana + amount);
        }

        public void RestoreAll()
        {
            Mana = maxMana;
            IsDashing = false;
            for (int i = 0; i < readyAt.Length; i++) readyAt[i] = 0f;
        }

        public bool TryCast(int slot, Vector3 aimPoint)
        {
            if (slot < 0 || slot >= abilities.Length) return false;
            AbilityData a = abilities[slot];
            if (a == null || unit.IsDead || !MatchController.IsPlaying || IsDashing) return false;
            if (Time.time < readyAt[slot] || Mana < a.manaCost) return false;

            Vector3 dir = aimPoint - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();

            Mana -= a.manaCost;
            readyAt[slot] = Time.time + a.cooldown;

            switch (a.type)
            {
                case AbilityType.Skillshot: CastSkillshot(a, dir); break;
                case AbilityType.Dash: StartCoroutine(Dash(a, dir)); break;
                case AbilityType.Shockwave: CastShockwave(a); break;
            }
            return true;
        }

        void CastSkillshot(AbilityData a, Vector3 dir)
        {
            transform.rotation = Quaternion.LookRotation(dir);
            if (a.projectilePrefab == null) return;

            Vector3 origin = transform.position + Vector3.up * 1.2f + dir * 0.8f;
            Projectile p = Instantiate(a.projectilePrefab, origin, Quaternion.identity);
            p.LaunchLinear(unit, dir, a.damage, a.speed, a.range, a.radius);
        }

        IEnumerator Dash(AbilityData a, Vector3 dir)
        {
            mover.Stop();
            transform.rotation = Quaternion.LookRotation(dir);
            IsDashing = true;

            float elapsed = 0f;
            float velocity = a.range / Mathf.Max(0.01f, a.duration);
            while (elapsed < a.duration && !unit.IsDead && mover.Agent.enabled && mover.Agent.isOnNavMesh)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                mover.Agent.Move(dir * velocity * dt);
                yield return null;
            }
            IsDashing = false;
        }

        void CastShockwave(AbilityData a)
        {
            foreach (var u in Unit.All)
            {
                if (u.team == unit.team || u.IsDead) continue;
                if (Unit.FlatDistance(transform.position, u.transform.position) > a.radius + u.Radius) continue;

                u.Health.TakeDamage(a.damage, unit);
                var fx = u.GetComponent<StatusEffects>();
                if (fx != null) fx.ApplySlow(a.slowFactor, a.slowDuration);
            }

            if (a.effectPrefab != null)
            {
                GameObject go = Instantiate(a.effectPrefab, transform.position + Vector3.up * 0.1f, Quaternion.identity);
                if (go.TryGetComponent(out TimedFx timed)) timed.Init(a.radius * 2f, 0.4f);
            }
        }
    }
}
