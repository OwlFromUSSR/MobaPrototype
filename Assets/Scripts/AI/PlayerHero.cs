using UnityEngine;
using UnityEngine.AI;

namespace Moba
{
    /// <summary>
    /// Управление героем игрока.
    /// ПКМ по земле — идти, ПКМ по врагу — преследовать и атаковать,
    /// Q/W/E — способности. Стоя на месте, герой сам бьёт врагов в радиусе атаки.
    /// </summary>
    public class PlayerHero : MonoBehaviour
    {
        public Unit Unit { get; private set; }
        public HeroAbilities Abilities { get; private set; }
        public HeroRespawn Respawn { get; private set; }

        Mover mover;
        Combat combat;
        Camera cam;
        Unit attackTarget;
        bool moveOrder;

        void Awake()
        {
            Unit = GetComponent<Unit>();
            Abilities = GetComponent<HeroAbilities>();
            Respawn = GetComponent<HeroRespawn>();
            mover = GetComponent<Mover>();
            combat = GetComponent<Combat>();
        }

        void Update()
        {
            if (Unit.IsDead)
            {
                attackTarget = null;
                moveOrder = false;
                return;
            }
            if (!MatchController.IsPlaying || Abilities.IsDashing) return;

            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            if (GameInput.RightClickDown)
            {
                HandleRightClick();
            }

            for (int i = 0; i < 3; i++)
                if (GameInput.AbilityDown(i)) Abilities.TryCast(i, AimPoint());

            Act();
        }

        void HandleRightClick()
        {
            Ray ray = cam.ScreenPointToRay(GameInput.PointerPosition);

            RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                Unit u = h.collider.GetComponentInParent<Unit>();
                if (u != null && u != Unit && !u.IsDead && u.team != Unit.team)
                {
                    attackTarget = u;
                    moveOrder = false;
                    return;
                }
            }

            if (GroundPoint(ray, out Vector3 point))
            {
                attackTarget = null;
                if (NavMesh.SamplePosition(point, out NavMeshHit nav, 3f, NavMesh.AllAreas))
                    point = nav.position;
                mover.MoveTo(point);
                moveOrder = true;
            }
        }

        void Act()
        {
            if (attackTarget != null)
            {
                if (attackTarget.IsDead)
                {
                    attackTarget = null;
                }
                else if (combat.InRange(attackTarget))
                {
                    mover.Stop();
                    combat.Attack(attackTarget);
                    return;
                }
                else
                {
                    mover.MoveToUnit(attackTarget);
                    return;
                }
            }

            if (moveOrder && !mover.IsMoving) moveOrder = false;

            if (!moveOrder)
            {
                Unit auto = TargetFinder.NearestPrioritized(transform.position, Unit.team, Unit.config.attackRange);
                if (auto != null) combat.Attack(auto);
            }
        }

        Vector3 AimPoint()
        {
            Ray ray = cam.ScreenPointToRay(GameInput.PointerPosition);
            return GroundPoint(ray, out Vector3 p) ? p : transform.position + transform.forward;
        }

        static bool GroundPoint(Ray ray, out Vector3 point)
        {
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }
            point = Vector3.zero;
            return false;
        }
    }
}
