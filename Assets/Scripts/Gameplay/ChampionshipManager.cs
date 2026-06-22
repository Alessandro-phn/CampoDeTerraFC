using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CampoDeTerraFC.Data;
using CampoDeTerraFC.SaveSystem;
using CampoDeTerraFC.Core.Services;

namespace CampoDeTerraFC.Gameplay
{
    /// <summary>
    /// Gerencia a lógica completa de um campeonato.
    /// Suporta fase de grupos (liga round-robin) e mata-mata (quartas, semi, final).
    /// Calcula classificação, gera confrontos e persiste o progresso.
    /// </summary>
    public sealed class ChampionshipManager : MonoBehaviour
    {
        // ===== DADOS =====

        [Header("Times Participantes")]
        [SerializeField] private List<TeamDataSO> _participatingTeams = new List<TeamDataSO>();

        [Header("Configuração")]
        [Tooltip("Número de times no campeonato (8 ou 16).")]
        [SerializeField] private int _teamCount = 8;

        [Tooltip("Times que avançam da fase de grupos para o mata-mata.")]
        [SerializeField] private int _teamsAdvancing = 4;

        // ===== ESTADO =====

        /// <summary>Fase atual do campeonato.</summary>
        public ChampionshipPhase CurrentPhase { get; private set; } = ChampionshipPhase.GroupStage;

        /// <summary>Rodada atual na fase de grupos.</summary>
        public int CurrentRound { get; private set; } = 0;

        /// <summary>Total de rodadas na fase de grupos.</summary>
        public int TotalGroupRounds { get; private set; }

        /// <summary>Confrontos da rodada atual.</summary>
        public List<Fixture> CurrentFixtures { get; private set; } = new List<Fixture>();

        /// <summary>Tabela de classificação ordenada.</summary>
        public List<TeamDataSO> Standings { get; private set; } = new List<TeamDataSO>();

        // ===== MATA-MATA =====

        private List<Fixture> _knockoutBracket = new List<Fixture>();

        // ===== EVENTOS =====

        public event Action<List<Fixture>> OnRoundGenerated;
        public event Action<List<TeamDataSO>> OnStandingsUpdated;
        public event Action<ChampionshipPhase> OnPhaseChanged;
        public event Action<TeamDataSO> OnChampionCrowned;

        // ===== UNITY LIFECYCLE =====

        private void Awake()
        {
            TotalGroupRounds = _teamCount - 1; // Round-robin: N-1 rodadas
        }

        // ===== INICIALIZAÇÃO =====

        /// <summary>
        /// Inicializa o campeonato com os times participantes.
        /// </summary>
        public void Initialize(List<TeamDataSO> teams)
        {
            if (teams == null || teams.Count < 4)
            {
                Debug.LogError("[ChampionshipManager] Mínimo de 4 times para o campeonato.");
                return;
            }

            _participatingTeams = new List<TeamDataSO>(teams);
            _teamCount = _participatingTeams.Count;
            TotalGroupRounds = _teamCount - 1;

            // Reseta estatísticas de todos os times
            foreach (TeamDataSO team in _participatingTeams)
                team.ResetSeasonStats();

            Standings = new List<TeamDataSO>(_participatingTeams);
            CurrentPhase = ChampionshipPhase.GroupStage;
            CurrentRound = 0;

            Debug.Log($"[ChampionshipManager] Campeonato iniciado com {_teamCount} times.");

            GenerateNextRound();
        }

        // ===== FASE DE GRUPOS =====

        /// <summary>
        /// Gera os confrontos da próxima rodada usando algoritmo round-robin.
        /// </summary>
        public void GenerateNextRound()
        {
            if (CurrentPhase != ChampionshipPhase.GroupStage) return;

            CurrentRound++;

            if (CurrentRound > TotalGroupRounds)
            {
                StartKnockoutStage();
                return;
            }

            CurrentFixtures = GenerateRoundRobinFixtures(CurrentRound);
            OnRoundGenerated?.Invoke(CurrentFixtures);

            Debug.Log($"[ChampionshipManager] Rodada {CurrentRound} de {TotalGroupRounds} gerada.");
        }

        /// <summary>
        /// Gera confrontos round-robin para uma rodada específica.
        /// Usa o algoritmo de rotação circular para garantir que todos joguem contra todos.
        /// </summary>
        private List<Fixture> GenerateRoundRobinFixtures(int round)
        {
            List<Fixture> fixtures = new List<Fixture>();
            int n = _participatingTeams.Count;

            // Algoritmo de rotação: fixa o primeiro time, rotaciona os outros
            List<TeamDataSO> rotation = new List<TeamDataSO>(_participatingTeams);

            // Aplica a rotação da rodada
            for (int r = 1; r < round; r++)
            {
                TeamDataSO last = rotation[rotation.Count - 1];
                rotation.RemoveAt(rotation.Count - 1);
                rotation.Insert(1, last); // Insere na posição 1 (mantém o índice 0 fixo)
            }

            // Cria os pares
            for (int i = 0; i < n / 2; i++)
            {
                Fixture fixture = new Fixture
                {
                    HomeTeam = rotation[i],
                    AwayTeam = rotation[n - 1 - i],
                    Round = round,
                    IsPlayed = false
                };

                fixtures.Add(fixture);
            }

            return fixtures;
        }

        /// <summary>
        /// Registra o resultado de um confronto e atualiza a tabela.
        /// </summary>
        public void RecordResult(Fixture fixture, int homeGoals, int awayGoals)
        {
            if (fixture.IsPlayed)
            {
                Debug.LogWarning("[ChampionshipManager] Este confronto já foi jogado.");
                return;
            }

            fixture.HomeGoals = homeGoals;
            fixture.AwayGoals = awayGoals;
            fixture.IsPlayed = true;

            // Atualiza as estatísticas dos times
            fixture.HomeTeam.RegisterMatchResult(homeGoals, awayGoals);
            fixture.AwayTeam.RegisterMatchResult(awayGoals, homeGoals);

            UpdateStandings();

            Debug.Log($"[ChampionshipManager] Resultado: {fixture.HomeTeam.TeamName} {homeGoals}x{awayGoals} {fixture.AwayTeam.TeamName}");
        }

        /// <summary>
        /// Simula automaticamente os confrontos que não são do jogador.
        /// </summary>
        public void SimulateNonPlayerFixtures(TeamDataSO playerTeam)
        {
            foreach (Fixture fixture in CurrentFixtures)
            {
                if (fixture.IsPlayed) continue;
                if (fixture.HomeTeam == playerTeam || fixture.AwayTeam == playerTeam) continue;

                // Simula resultado com base nos atributos dos times
                int homeGoals = SimulateGoals(fixture.HomeTeam, fixture.AwayTeam, isHome: true);
                int awayGoals = SimulateGoals(fixture.AwayTeam, fixture.HomeTeam, isHome: false);

                RecordResult(fixture, homeGoals, awayGoals);
            }
        }

        /// <summary>
        /// Simula a quantidade de gols que um time marcaria contra outro.
        /// </summary>
        private int SimulateGoals(TeamDataSO attacking, TeamDataSO defending, bool isHome)
        {
            float attackRating = attacking.GetTeamOverallRating() / 99f;
            float defenseRating = defending.GetTeamOverallRating() / 99f;
            float homeAdvantage = isHome ? 0.1f : 0f;

            float goalProbability = (attackRating - defenseRating * 0.8f + homeAdvantage + 0.5f);
            float avgGoals = Mathf.Clamp(goalProbability * 3f, 0.5f, 5f);

            // Distribuição de Poisson simplificada
            return Mathf.RoundToInt(avgGoals + UnityEngine.Random.Range(-1f, 1f));
        }

        // ===== CLASSIFICAÇÃO =====

        /// <summary>
        /// Atualiza e ordena a tabela de classificação.
        /// Critérios: Pontos → Saldo de gols → Gols marcados → Nome (alfabético).
        /// </summary>
        private void UpdateStandings()
        {
            Standings = _participatingTeams
                .OrderByDescending(t => t.Points)
                .ThenByDescending(t => t.GoalDifference)
                .ThenByDescending(t => t.GoalsScored)
                .ThenBy(t => t.TeamName)
                .ToList();

            OnStandingsUpdated?.Invoke(Standings);
        }

        // ===== MATA-MATA =====

        /// <summary>
        /// Inicia a fase de mata-mata com os times classificados.
        /// </summary>
        private void StartKnockoutStage()
        {
            UpdateStandings();

            List<TeamDataSO> qualifiedTeams = Standings.Take(_teamsAdvancing).ToList();

            Debug.Log($"[ChampionshipManager] Fase de grupos encerrada. {qualifiedTeams.Count} times avançam.");

            CurrentPhase = ChampionshipPhase.Quarterfinals;
            OnPhaseChanged?.Invoke(CurrentPhase);

            GenerateKnockoutBracket(qualifiedTeams);
        }

        /// <summary>
        /// Gera o bracket do mata-mata (1° vs último, 2° vs penúltimo, etc.).
        /// </summary>
        private void GenerateKnockoutBracket(List<TeamDataSO> teams)
        {
            _knockoutBracket.Clear();
            CurrentFixtures.Clear();

            int half = teams.Count / 2;
            for (int i = 0; i < half; i++)
            {
                Fixture fixture = new Fixture
                {
                    HomeTeam = teams[i],
                    AwayTeam = teams[teams.Count - 1 - i],
                    Round = CurrentRound,
                    Phase = CurrentPhase,
                    IsKnockout = true
                };

                _knockoutBracket.Add(fixture);
                CurrentFixtures.Add(fixture);
            }

            OnRoundGenerated?.Invoke(CurrentFixtures);
        }

        /// <summary>
        /// Avança o mata-mata para a próxima fase com os vencedores.
        /// </summary>
        public void AdvanceKnockoutStage()
        {
            List<TeamDataSO> winners = new List<TeamDataSO>();

            foreach (Fixture fixture in _knockoutBracket)
            {
                if (!fixture.IsPlayed) continue;

                if (fixture.HomeGoals > fixture.AwayGoals)
                    winners.Add(fixture.HomeTeam);
                else if (fixture.AwayGoals > fixture.HomeGoals)
                    winners.Add(fixture.AwayTeam);
                else
                {
                    // Empate no mata-mata: pênaltis ou golden goal (por ora sorteia)
                    winners.Add(UnityEngine.Random.value > 0.5f ? fixture.HomeTeam : fixture.AwayTeam);
                }
            }

            if (winners.Count == 1)
            {
                // Campeão!
                CrownChampion(winners[0]);
                return;
            }

            // Avança a fase
            CurrentPhase = CurrentPhase switch
            {
                ChampionshipPhase.Quarterfinals => ChampionshipPhase.Semifinals,
                ChampionshipPhase.Semifinals => ChampionshipPhase.Final,
                _ => ChampionshipPhase.Finished
            };

            OnPhaseChanged?.Invoke(CurrentPhase);
            GenerateKnockoutBracket(winners);
        }

        private void CrownChampion(TeamDataSO champion)
        {
            CurrentPhase = ChampionshipPhase.Finished;
            OnPhaseChanged?.Invoke(CurrentPhase);
            OnChampionCrowned?.Invoke(champion);

            Debug.Log($"[ChampionshipManager] 🏆 CAMPEÃO: {champion.TeamName}!");
        }

        // ===== PERSISTÊNCIA =====

        public void SaveProgress()
        {
            SaveManager saveManager = FindObjectOfType<SaveManager>();
            saveManager?.SaveChampionship();
        }
    }

    // ===== ESTRUTURAS =====

    /// <summary>Um confronto entre dois times.</summary>
    [Serializable]
    public class Fixture
    {
        public TeamDataSO HomeTeam;
        public TeamDataSO AwayTeam;
        public int HomeGoals;
        public int AwayGoals;
        public int Round;
        public bool IsPlayed;
        public bool IsKnockout;
        public ChampionshipPhase Phase;

        public override string ToString() =>
            $"{HomeTeam?.TeamName} {HomeGoals}x{AwayGoals} {AwayTeam?.TeamName}" +
            (IsPlayed ? "" : " (pendente)");
    }

    /// <summary>Fases do campeonato.</summary>
    public enum ChampionshipPhase
    {
        GroupStage,
        Quarterfinals,
        Semifinals,
        ThirdPlace,
        Final,
        Finished
    }
}
