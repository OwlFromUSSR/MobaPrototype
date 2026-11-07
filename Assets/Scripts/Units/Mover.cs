using UnityEngine;
using UnityEngine.AI;

namespace Moba
{
    /// <summary>Обёртка над NavMeshAgent: движение, остановка, учёт замедления.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Mover : MonoBehaviour
    {
        NavMeshAgent agent;
        Unit unit;
        StatusEffects effects;
        Vector3 lastDestination;

        public NavMeshAgent Agent => agent;
        bool Ready => agent.enabled && agent.isOnNavMesh;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            unit = GetComponent<Unit>();
            effects = GetComponent<StatusEffects>();
            agent.speed = unit.config.moveSpeed;
        }

        void Update()
        {
            if (!agent.enabled) return;
            float mult = effects != null ? effects.SpeedMultiplier : 1f;
            agent.speed = unit.config.moveSpeed * mult;
        }

        public bool IsMoving =>
            Ready && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.05f);

        public bool MoveTo(Vector3 point)
        {
            if (!Ready) return false;

            bool sameDestination = (point - lastDestination).sqrMagnitude < 0.09f;
            if (!agent.isStopped && sameDestination && (agent.hasPath || agent.pathPending)) return true;

            lastDestination = point;
            agent.isStopped = false;
            return agent.SetDestination(point);
        }

        /// <summary>
        /// Идти к юниту, но не в его центр: центр здания лежит внутри «дыры» NavMesh,
        /// поэтому цель — точка у края юнита со стороны, откуда мы идём.
        /// </summary>
        public bool MoveToUnit(Unit target)
        {
            Vector3 toMe = transform.position - target.transform.position;
            toMe.y = 0f;
            if (toMe.sqrMagnitude < 0.01f) toMe = -transform.forward;
            toMe.Normalize();

            Vector3 destination = target.transform.position + toMe * (target.Radius + agent.radius + 0.3f);
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                destination = hit.position;

            return MoveTo(destination);
        }

        public void Stop()
        {
            if (!Ready) return;
            agent.isStopped = true;
            agent.ResetPath();
        }
    }
}
