using UnityEngine;

namespace Moba
{
    /// <summary>Фонтан у базы: лечит героев своей команды и восполняет им ману.</summary>
    public class Fountain : MonoBehaviour
    {
        public TeamId team;
        public float radius = 8f;
        public float healPerSecond = 60f;
        public float manaPerSecond = 15f;

        void Update()
        {
            if (!MatchController.IsPlaying) return;

            foreach (var u in Unit.All)
            {
                if (u.team != team || u.kind != UnitKind.Hero || u.IsDead) continue;
                if (Unit.FlatDistance(u.transform.position, transform.position) > radius) continue;

                u.Health.Heal(healPerSecond * Time.deltaTime);
                var abilities = u.GetComponent<HeroAbilities>();
                if (abilities != null) abilities.RestoreMana(manaPerSecond * Time.deltaTime);
            }
        }
    }
}
