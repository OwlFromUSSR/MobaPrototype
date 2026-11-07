using UnityEngine;

namespace Moba
{
    /// <summary>
    /// Миньон идёт по вейпоинтам линии. Если в радиусе агро появился враг — атакует его,
    /// потеряв цель (умерла или ушла слишком далеко) — возвращается на линию.
    /// </summary>
    public class MinionAI : MonoBehaviour
    {
        Unit unit;
        Mover mover;
        Combat combat;
        Transform[] lane;
        int index;
        Unit target;
        float nextScan;

        void Awake()
        {
            unit = GetComponent<Unit>();
            mover = GetComponent<Mover>();
            combat = GetComponent<Combat>();
        }

        public void SetLane(Transform[] waypoints)
        {
            lane = waypoints;
            index = 0;
        }

        void Update()
        {
            if (!MatchController.IsPlaying || unit.IsDead) return;

            UpdateTarget();

            if (target != null)
            {
                if (combat.InRange(target))
                {
                    mover.Stop();
                    combat.Attack(target);
                }
                else
                {
                    mover.MoveToUnit(target);
                }
                return;
            }

            FollowLane();
        }

        void UpdateTarget()
        {
            float leash = unit.config.aggroRange * 1.3f;
            if (target != null && (target.IsDead || target.EdgeDistance(transform.position) > leash))
                target = null;

            if (Time.time < nextScan) return;
            nextScan = Time.time + 0.25f;

            if (target == null)
                target = TargetFinder.NearestPrioritized(transform.position, unit.team, unit.config.aggroRange);
        }

        void FollowLane()
        {
            if (lane == null || lane.Length == 0) return;

            Vector3 wp = lane[index].position;
            if (Unit.FlatDistance(transform.position, wp) < 1.5f && index < lane.Length - 1)
            {
                index++;
                wp = lane[index].position;
            }
            mover.MoveTo(wp);
        }
    }
}
