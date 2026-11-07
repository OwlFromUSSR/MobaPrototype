using UnityEngine;

namespace Moba
{
    /// <summary>Баланс и параметры юнита. Правится в инспекторе без изменения кода.</summary>
    [CreateAssetMenu(menuName = "MOBA/Unit Config", fileName = "UnitConfig")]
    public class UnitConfig : ScriptableObject
    {
        public string displayName = "Unit";
        public float maxHealth = 100f;
        public float moveSpeed = 3.5f;
        public float radius = 0.5f;

        [Header("Attack")]
        public float attackDamage = 10f;
        [Tooltip("Дальность считается от края цели, а не от её центра")]
        public float attackRange = 1.8f;
        public float attackCooldown = 1f;
        public float aggroRange = 8f;

        [Header("Ranged (optional)")]
        public Projectile projectilePrefab;
        public float projectileSpeed = 14f;
        public float projectileHeight = 1.2f;
    }
}
