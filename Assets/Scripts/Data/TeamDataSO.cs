using System.Collections.Generic;
using UnityEngine;
using CampoDeTerraFC.Data;

namespace CampoDeTerraFC.Data
{
    /// <summary>
    /// Dados de um time de futebol como ScriptableObject.
    /// Contém elenco, formação, cores e informações do time.
    /// </summary>
    [CreateAssetMenu(menuName = "Campo de Terra FC/Data/Team Data", fileName = "Team_")]
    public class TeamDataSO : ScriptableObject
    {
        // ===== IDENTIFICAÇÃO =====

        [Header("Identificação")]
        [Tooltip("Nome oficial do time.")]
        public string TeamName = "Vila Operária FC";

        [Tooltip("Apelido do time.")]
        public string Nickname = "Os Guerreiros";

        [Tooltip("Logo do time para UI.")]
        public Sprite Logo;

        // ===== CORES =====

        [Header("Cores do Uniforme")]
        [Tooltip("Cor primária do uniforme.")]
        public Color PrimaryColor = Color.red;

        [Tooltip("Cor secundária do uniforme.")]
        public Color SecondaryColor = Color.white;

        [Tooltip("Cor do uniforme alternativo (fora de casa).")]
        public Color AwayPrimaryColor = Color.gray;

        // ===== ELENCO =====

        [Header("Elenco")]
        [Tooltip("Lista de jogadores do time (máximo 11 para campo padrão).")]
        public List<PlayerDataSO> Squad = new List<PlayerDataSO>();

        [Tooltip("Jogadores reservas (para substituição).")]
        public List<PlayerDataSO> Bench = new List<PlayerDataSO>();

        // ===== FORMAÇÃO =====

        [Header("Formação e Estilo")]
        [Tooltip("Formação padrão do time.")]
        public FormationSO DefaultFormation;

        [Tooltip("Estilo de jogo preferencial.")]
        public PlayStyle PlayStyle = PlayStyle.Balanced;

        [Tooltip("Agressividade na marcação (0 = passivo, 1 = pressão total).")]
        [Range(0f, 1f)]
        public float PressIntensity = 0.5f;

        [Tooltip("Linha defensiva (0 = baixa, 1 = alta).")]
        [Range(0f, 1f)]
        public float DefensiveLine = 0.5f;

        // ===== ESTATÍSTICAS (CAMPEONATO) =====

        [Header("Estatísticas da Temporada")]
        [HideInInspector] public int GamesPlayed;
        [HideInInspector] public int Wins;
        [HideInInspector] public int Draws;
        [HideInInspector] public int Losses;
        [HideInInspector] public int GoalsScored;
        [HideInInspector] public int GoalsConceded;

        // ===== PROPRIEDADES CALCULADAS =====

        /// <summary>Pontos acumulados na tabela (3 por vitória, 1 por empate).</summary>
        public int Points => (Wins * 3) + Draws;

        /// <summary>Saldo de gols.</summary>
        public int GoalDifference => GoalsScored - GoalsConceded;

        /// <summary>Goleiro titular do time (primeiro da lista).</summary>
        public PlayerDataSO Goalkeeper => GetGoalkeeper();

        // ===== MÉTODOS =====

        /// <summary>
        /// Retorna o goleiro titular. Se não houver nenhum marcado como goleiro,
        /// usa o primeiro jogador do elenco como fallback.
        /// </summary>
        private PlayerDataSO GetGoalkeeper()
        {
            foreach (PlayerDataSO player in Squad)
            {
                if (player != null && player.Position == PlayerPosition.Goalkeeper)
                    return player;
            }

            return Squad.Count > 0 ? Squad[0] : null;
        }

        /// <summary>
        /// Retorna a nota geral do time (média das notas dos jogadores titulares).
        /// </summary>
        public int GetTeamOverallRating()
        {
            if (Squad.Count == 0) return 0;

            int total = 0;
            int count = 0;

            foreach (PlayerDataSO player in Squad)
            {
                if (player != null)
                {
                    total += player.GetOverallRating();
                    count++;
                }
            }

            return count > 0 ? total / count : 0;
        }

        /// <summary>
        /// Registra o resultado de uma partida nas estatísticas do time.
        /// </summary>
        /// <param name="scored">Gols marcados.</param>
        /// <param name="conceded">Gols sofridos.</param>
        public void RegisterMatchResult(int scored, int conceded)
        {
            GamesPlayed++;
            GoalsScored += scored;
            GoalsConceded += conceded;

            if (scored > conceded) Wins++;
            else if (scored == conceded) Draws++;
            else Losses++;
        }

        /// <summary>
        /// Reseta as estatísticas da temporada para um novo campeonato.
        /// </summary>
        public void ResetSeasonStats()
        {
            GamesPlayed = 0;
            Wins = 0;
            Draws = 0;
            Losses = 0;
            GoalsScored = 0;
            GoalsConceded = 0;
        }

        /// <summary>
        /// Retorna um resumo do time em string (debug).
        /// </summary>
        public override string ToString()
        {
            return $"{TeamName} | Pts: {Points} | SG: {GoalDifference} | {Wins}V {Draws}E {Losses}D";
        }
    }

    /// <summary>Estilos de jogo disponíveis para os times.</summary>
    public enum PlayStyle
    {
        Attacking,       // Mais atacantes, menos defensores
        Balanced,        // Equilibrado
        Defensive,       // Foco na defesa, contra-ataque
        Possession,      // Troca de passes, domínio
        Direct,          // Bola longa para o atacante
        HighPress        // Pressão intensa no adversário
    }
}
