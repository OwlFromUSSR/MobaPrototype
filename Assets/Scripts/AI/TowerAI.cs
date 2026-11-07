using UnityEngine;

namespace Moba
{
    /// <summary>Башня: стоит на месте и бьёт ближайшего врага в радиусе (миньоны в приоритете).</summary>
    public class TowerAI : MonoBehaviour
    {
        Unit unit;
        Combat combat;
        Unit target;
        float nextScan;

        void Awake()
        {
            unit = GetComponent<Unit>();
            combat = GetComponent<Combat>();
        }

        void Update()
        {
            if (!MatchController.IsPlaying || unit.IsDead) return;

            if (Time.time >= nextScan)
            {
                nextScan = Time.time + 0.2f;
                target = TargetFinder.ForTower(transform.position, unit.team, unit.config.attackRange);
            }

            if (target != null && !target.IsDead) combat.Attack(target);
        }
    }
}
