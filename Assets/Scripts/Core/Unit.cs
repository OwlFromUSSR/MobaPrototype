using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moba
{
    [Serializable]
    public class UnitStats
    {
        public int kills;
        public int deaths;
        public int minionKills;
        public int structureKills;
        public float damageDealt;

        public void RegisterKill(UnitKind victimKind)
        {
            if (victimKind == UnitKind.Hero) kills++;
            else if (victimKind == UnitKind.Minion) minionKills++;
            else structureKills++;
        }
    }

    /// <summary>
    /// Любой участвующий в бою объект: герой, миньон, башня, база.
    /// Все юниты регистрируются в статическом списке All, по которому ищутся цели.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Unit : MonoBehaviour
    {
        public static readonly List<Unit> All = new List<Unit>();

        /// <summary>(жертва, убийца)</summary>
        public static event Action<Unit, Unit> UnitDied;
        /// <summary>(урон, жертва, источник)</summary>
        public static event Action<float, Unit, Unit> UnitDamaged;

        public TeamId team;
        public UnitKind kind;
        public UnitConfig config;
        public Renderer[] tintedRenderers;

        public UnitStats Stats { get; } = new UnitStats();
        public Health Health { get; private set; }

        public bool IsDead => Health == null || Health.IsDead;
        public float Radius => config != null ? config.radius : 0.5f;
        public string DisplayName => config != null ? config.displayName : name;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
            UnitDied = null;
            UnitDamaged = null;
        }

        void Awake()
        {
            Health = GetComponent<Health>();
            Health.Init(config.maxHealth);
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
        }

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        public void SetTeam(TeamId newTeam, Material teamMaterial)
        {
            team = newTeam;
            if (teamMaterial == null || tintedRenderers == null) return;
            foreach (var r in tintedRenderers) r.sharedMaterial = teamMaterial;
        }

        /// <summary>Расстояние по горизонтали от точки до края юнита.</summary>
        public float EdgeDistance(Vector3 from)
        {
            return Mathf.Max(0f, FlatDistance(transform.position, from) - Radius);
        }

        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void OnDamaged(float amount, Unit source)
        {
            if (source != null) source.Stats.damageDealt += amount;
            UnitDamaged?.Invoke(amount, this, source);
        }

        void OnDied(Unit killer)
        {
            Stats.deaths++;
            if (killer != null) killer.Stats.RegisterKill(kind);
            UnitDied?.Invoke(this, killer);

            // Герои респавнятся (см. HeroRespawn), остальные удаляются.
            if (kind != UnitKind.Hero) Destroy(gameObject, 0.1f);
        }
    }
}
