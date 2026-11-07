using UnityEngine;

namespace Moba
{
    /// <summary>
    /// Бот-герой. Состояния: Lane (идёт по линии и сражается) и Retreat (отступает к фонтану).
    /// Правила осторожности: без поддержки 2+ миньонов не лезет под вражескую башню
    /// и не атакует здания.
    /// </summary>
    public class BotHero : MonoBehaviour
    {
        enum State { Lane, Retreat }

        public Transform[] lane;
        public Transform fountainPoint;
        public float retreatBelow = 0.3f;
        public float returnAbove = 0.85f;

        State state = State.Lane;
        Unit unit;
        Mover mover;
        Combat combat;
        HeroAbilities abilities;
        int index;
        float nextThink;

        void Awake()
        {
            unit = GetComponent<Unit>();
            mover = GetComponent<Mover>();
            combat = GetComponent<Combat>();
            abilities = GetComponent<HeroAbilities>();
        }

        void Update()
        {
            if (!MatchController.IsPlaying) return;
            if (unit.IsDead)
            {
                state = State.Lane;
                index = 0;
                return;
            }
            if (abilities.IsDashing || Time.time < nextThink) return;
            nextThink = Time.time + 0.1f;

            Vector3 pos = transform.position;
            float hp = unit.Health.Percent;
            if (state == State.Lane && hp < retreatBelow) state = State.Retreat;
            else if (state == State.Retreat && hp >= returnAbove) state = State.Lane;

            if (state == State.Retreat)
            {
                Retreat(pos);
                return;
            }

            bool supported = TargetFinder.CountAllies(pos, unit.team, UnitKind.Minion, 10f) >= 2;
            Unit target = PickTarget(pos, supported);
            if (target != null) Engage(target);
            else Advance(pos, supported);
        }

        Unit PickTarget(Vector3 pos, bool supported)
        {
            float range = unit.config.aggroRange;
            Unit t = TargetFinder.Nearest(pos, unit.team, range, u =>
                (u.kind == UnitKind.Hero || u.kind == UnitKind.Minion) &&
                (supported || !TargetFinder.IsUnderEnemyTower(u.transform.position, unit.team)));

            if (t == null && supported)
                t = TargetFinder.Nearest(pos, unit.team, range, u => u.kind == UnitKind.Tower || u.kind == UnitKind.Base);
            return t;
        }

        void Engage(Unit target)
        {
            Vector3 pos = transform.position;
            if (target.kind == UnitKind.Hero)
            {
                float d = target.EdgeDistance(pos);
                AbilityData q = abilities.abilities[0];
                AbilityData e = abilities.abilities[2];
                if (q != null && d <= q.range * 0.9f) abilities.TryCast(0, target.transform.position);
                if (e != null && d <= e.radius) abilities.TryCast(2, target.transform.position);
            }

            if (combat.InRange(target))
            {
                mover.Stop();
                combat.Attack(target);
            }
            else
            {
                mover.MoveToUnit(target);
            }
        }

        void Retreat(Vector3 pos)
        {
            Vector3 home = fountainPoint.position;
            Unit threat = TargetFinder.Nearest(pos, unit.team, 8f, u => u.kind == UnitKind.Hero);
            if (threat != null) abilities.TryCast(1, home);
            mover.MoveTo(home);
        }

        void Advance(Vector3 pos, bool supported)
        {
            if (lane == null || lane.Length == 0) return;

            index = Mathf.Clamp(index, 0, lane.Length - 1);
            Vector3 wp = lane[index].position;
            if (Unit.FlatDistance(pos, wp) < 2f && index < lane.Length - 1)
            {
                index++;
                wp = lane[index].position;
            }

            if (!supported && TargetFinder.IsUnderEnemyTower(wp, unit.team))
            {
                mover.Stop();
                return;
            }
            mover.MoveTo(wp);
        }
    }
}
