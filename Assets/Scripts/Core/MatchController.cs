using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Moba
{
    public enum MatchState { Preparing, Playing, Finished }

    [Serializable]
    public class TeamStats
    {
        public int heroKills;
        public int minionKills;
        public int structureKills;
        public float damageDealt;
    }

    /// <summary>
    /// Жизненный цикл одной игровой сессии: обратный отсчёт → игра → итоги.
    /// Матч заканчивается разрушением базы одной из команд.
    /// </summary>
    public class MatchController : MonoBehaviour
    {
        public static MatchController Instance { get; private set; }
        public static bool IsPlaying =>
            Instance != null && Instance.State == MatchState.Playing && !Instance.Paused;

        public TeamId playerTeam = TeamId.Blue;
        public float countdownSeconds = 3f;

        public MatchState State { get; private set; } = MatchState.Preparing;
        public bool Paused { get; private set; }
        public float Elapsed { get; private set; }
        public float CountdownRemaining { get; private set; }
        public TeamId Winner { get; private set; }
        public TeamStats[] Teams { get; private set; }

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            CountdownRemaining = countdownSeconds;
            Teams = new[] { new TeamStats(), new TeamStats() };
            Unit.UnitDied += OnUnitDied;
            Unit.UnitDamaged += OnUnitDamaged;
        }

        void OnDestroy()
        {
            Unit.UnitDied -= OnUnitDied;
            Unit.UnitDamaged -= OnUnitDamaged;
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        void Update()
        {
            switch (State)
            {
                case MatchState.Preparing:
                    CountdownRemaining -= Time.deltaTime;
                    if (CountdownRemaining <= 0f) State = MatchState.Playing;
                    break;

                case MatchState.Playing:
                    if (GameInput.PausePressed)
                    {
                        Paused = !Paused;
                        Time.timeScale = Paused ? 0f : 1f;
                    }
                    if (!Paused) Elapsed += Time.deltaTime;
                    break;

                case MatchState.Finished:
                    if (GameInput.RestartPressed) Restart();
                    break;
            }
        }

        void OnUnitDamaged(float amount, Unit victim, Unit source)
        {
            if (source != null) Teams[(int)source.team].damageDealt += amount;
        }

        void OnUnitDied(Unit victim, Unit killer)
        {
            if (killer != null)
            {
                TeamStats ts = Teams[(int)killer.team];
                if (victim.kind == UnitKind.Hero) ts.heroKills++;
                else if (victim.kind == UnitKind.Minion) ts.minionKills++;
                else ts.structureKills++;
            }

            if (victim.kind == UnitKind.Base && State == MatchState.Playing)
                EndMatch(victim.team.Opposite());
        }

        void EndMatch(TeamId winner)
        {
            State = MatchState.Finished;
            Winner = winner;

            foreach (var u in Unit.All)
            {
                var mover = u.GetComponent<Mover>();
                if (mover != null) mover.Stop();
            }
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
