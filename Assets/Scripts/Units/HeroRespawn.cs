using System.Collections;
using UnityEngine;

namespace Moba
{
    /// <summary>После смерти прячет героя, ждёт и возрождает его на точке спавна с полным HP.</summary>
    public class HeroRespawn : MonoBehaviour
    {
        public Transform spawnPoint;
        public float respawnTime = 8f;

        public float Remaining { get; private set; }

        Unit unit;
        Mover mover;
        HeroAbilities abilities;
        Renderer[] renderers;
        Collider[] colliders;

        void Awake()
        {
            unit = GetComponent<Unit>();
            mover = GetComponent<Mover>();
            abilities = GetComponent<HeroAbilities>();
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
        }

        void Start()
        {
            unit.Health.Died += OnDied;
        }

        void OnDestroy()
        {
            if (unit != null && unit.Health != null) unit.Health.Died -= OnDied;
        }

        void OnDied(Unit killer)
        {
            StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            SetVisible(false);
            mover.Agent.enabled = false;

            Remaining = respawnTime;
            while (Remaining > 0f)
            {
                Remaining -= Time.deltaTime;
                yield return null;
            }

            transform.position = spawnPoint.position;
            mover.Agent.enabled = true;
            mover.Agent.Warp(spawnPoint.position);
            unit.Health.Revive();
            abilities.RestoreAll();
            SetVisible(true);
            Remaining = 0f;
        }

        void SetVisible(bool visible)
        {
            foreach (var r in renderers) r.enabled = visible;
            foreach (var c in colliders) c.enabled = visible;
        }
    }
}
