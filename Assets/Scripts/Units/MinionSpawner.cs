using System.Collections;
using UnityEngine;

namespace Moba
{
    /// <summary>Периодически спавнит волны миньонов своей команды. Размер волны растёт со временем.</summary>
    public class MinionSpawner : MonoBehaviour
    {
        public TeamId team;
        public GameObject meleePrefab;
        public GameObject rangedPrefab;
        public Material teamMaterial;
        public Transform spawnPoint;
        public Transform[] lane;

        public float firstWaveDelay = 6f;
        public float waveInterval = 25f;
        public int meleeCount = 3;
        public int rangedCount = 1;
        [Tooltip("Каждые N волн в составе появляется ещё один ближний миньон")]
        public int growEveryWaves = 3;
        public float spawnGap = 0.8f;

        int waveIndex;

        IEnumerator Start()
        {
            while (!MatchController.IsPlaying) yield return null;
            yield return new WaitForSeconds(firstWaveDelay);

            while (MatchController.Instance != null && MatchController.Instance.State != MatchState.Finished)
            {
                yield return SpawnWave();
                waveIndex++;
                yield return new WaitForSeconds(waveInterval);
            }
        }

        IEnumerator SpawnWave()
        {
            int melee = meleeCount + (growEveryWaves > 0 ? waveIndex / growEveryWaves : 0);
            int total = melee + rangedCount;

            for (int i = 0; i < total; i++)
            {
                while (!MatchController.IsPlaying) yield return null;

                GameObject prefab = i < melee ? meleePrefab : rangedPrefab;
                Vector3 offset = new Vector3(0f, 0f, ((i % 2 == 0) ? 1f : -1f) * 0.6f);
                GameObject go = Instantiate(prefab, spawnPoint.position + offset, spawnPoint.rotation);

                Unit unit = go.GetComponent<Unit>();
                unit.SetTeam(team, teamMaterial);
                go.GetComponent<MinionAI>().SetLane(lane);

                yield return new WaitForSeconds(spawnGap);
            }
        }
    }
}
