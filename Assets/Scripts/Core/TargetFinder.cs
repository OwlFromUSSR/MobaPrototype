using System;
using UnityEngine;

namespace Moba
{
    /// <summary>Поиск целей по реестру Unit.All (без слоёв и физики).</summary>
    public static class TargetFinder
    {
        public static Unit Nearest(Vector3 from, TeamId myTeam, float range, Func<Unit, bool> filter = null)
        {
            Unit best = null;
            float bestDistance = float.MaxValue;

            foreach (var u in Unit.All)
            {
                if (u.team == myTeam || u.IsDead) continue;
                if (filter != null && !filter(u)) continue;

                float d = u.EdgeDistance(from);
                if (d <= range && d < bestDistance)
                {
                    best = u;
                    bestDistance = d;
                }
            }
            return best;
        }

        /// <summary>Сначала герои и миньоны, здания только если больше никого нет.</summary>
        public static Unit NearestPrioritized(Vector3 from, TeamId myTeam, float range)
        {
            Unit u = Nearest(from, myTeam, range, x => x.kind == UnitKind.Hero || x.kind == UnitKind.Minion);
            if (u != null) return u;
            return Nearest(from, myTeam, range);
        }

        /// <summary>Башня бьёт миньонов первыми, потом героев; здания не трогает.</summary>
        public static Unit ForTower(Vector3 from, TeamId myTeam, float range)
        {
            Unit u = Nearest(from, myTeam, range, x => x.kind == UnitKind.Minion);
            if (u != null) return u;
            return Nearest(from, myTeam, range, x => x.kind == UnitKind.Hero);
        }

        public static int CountAllies(Vector3 from, TeamId team, UnitKind kind, float range)
        {
            int count = 0;
            foreach (var u in Unit.All)
            {
                if (u.team != team || u.kind != kind || u.IsDead) continue;
                if (u.EdgeDistance(from) <= range) count++;
            }
            return count;
        }

        public static bool IsUnderEnemyTower(Vector3 pos, TeamId myTeam)
        {
            foreach (var u in Unit.All)
            {
                if (u.team == myTeam || u.kind != UnitKind.Tower || u.IsDead) continue;
                if (u.config != null && Unit.FlatDistance(u.transform.position, pos) <= u.config.attackRange)
                    return true;
            }
            return false;
        }
    }
}
