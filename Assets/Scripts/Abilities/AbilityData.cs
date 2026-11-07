using UnityEngine;

namespace Moba
{
    public enum AbilityType { Skillshot, Dash, Shockwave }

    /// <summary>Параметры способности героя. Правятся в инспекторе.</summary>
    [CreateAssetMenu(menuName = "MOBA/Ability Data", fileName = "AbilityData")]
    public class AbilityData : ScriptableObject
    {
        public string abilityName = "Ability";
        public AbilityType type;
        public float cooldown = 5f;
        public float manaCost = 20f;

        [Header("Effect")]
        public float damage = 50f;
        [Tooltip("Skillshot: дальность полёта. Dash: длина рывка.")]
        public float range = 12f;
        [Tooltip("Skillshot: радиус попадания. Shockwave: радиус волны.")]
        public float radius = 1f;
        public float speed = 20f;
        [Tooltip("Dash: длительность рывка в секундах.")]
        public float duration = 0.2f;
        [Range(0.1f, 1f)] public float slowFactor = 0.5f;
        public float slowDuration = 2f;

        [Header("Visuals")]
        public Projectile projectilePrefab;
        public GameObject effectPrefab;
    }
}
