using System;
using System.Collections.Generic;
using UnityEngine;
using CampoDeTerraFC.Data;
using CampoDeTerraFC.Player;

namespace CampoDeTerraFC.Managers
{
    /// <summary>
    /// Gerencia um time em campo: instanciação, posicionamento, seleção de jogador controlado.
    /// Não é MonoBehaviour — é instanciado e gerenciado pelo MatchController.
    /// </summary>
    public sealed class TeamManager
    {
        // ===== DADOS =====

        private readonly TeamDataSO _teamData;
        private readonly int _teamIndex;

        // ===== JOGADORES EM CAMPO =====

        /// <summary>Lista de controladores dos jogadores em campo.</summary>
        public List<PlayerController> PlayersOnField { get; private set; } = new List<PlayerController>();

        /// <summary>Controlador do goleiro.</summary>
        public PlayerController Goalkeeper { get; private set; }

        /// <summary>Jogador atualmente controlado pelo humano (null = IA controla todos).</summary>
        public PlayerController HumanControlledPlayer { get; private set; }

        // ===== ESTADO =====

        private bool _sideInverted;

        // ===== EVENTOS =====

        /// <summary>Disparado ao mudar o jogador controlado pelo humano.</summary>
        public event Action<PlayerController> OnControlledPlayerChanged;

        // ===== CONSTRUTOR =====

        public TeamManager(TeamDataSO teamData, int teamIndex)
        {
            _teamData = teamData ?? throw new ArgumentNullException(nameof(teamData));
            _teamIndex = teamIndex;
        }

        // ===== SPAWN =====

        /// <summary>
        /// Instancia os jogadores do time nas posições da formação.
        /// </summary>
        /// <param name="spawnPoints">Pontos de spawn base (podem ser sobrescritos pela formação).</param>
        /// <param name="formation">Formação tática.</param>
        public void SpawnPlayers(Transform[] spawnPoints, FormationSO formation)
        {
            if (_teamData.Squad == null || _teamData.Squad.Count == 0)
            {
                Debug.LogError($"[TeamManager] Time {_teamData.TeamName} não tem jogadores no elenco.");
                return;
            }

            PlayersOnField.Clear();

            int count = Mathf.Min(_teamData.Squad.Count, spawnPoints?.Length ?? 0);

            for (int i = 0; i < count; i++)
            {
                PlayerDataSO playerData = _teamData.Squad[i];
                if (playerData == null || playerData.PlayerPrefab == null) continue;

                Vector3 spawnPos = spawnPoints != null && i < spawnPoints.Length
                    ? spawnPoints[i].position
                    : Vector3.zero;

                GameObject playerObj = GameObject.Instantiate(playerData.PlayerPrefab, spawnPos, Quaternion.identity);
                playerObj.name = $"[Team{_teamIndex}] {playerData.PlayerName}";

                PlayerController controller = playerObj.GetComponent<PlayerController>();
                if (controller == null)
                {
                    Debug.LogError($"[TeamManager] Prefab de {playerData.PlayerName} não tem PlayerController.");
                    GameObject.Destroy(playerObj);
                    continue;
                }

                controller.Initialize(playerData, _teamIndex);
                PlayersOnField.Add(controller);

                if (playerData.Position == PlayerPosition.Goalkeeper)
                    Goalkeeper = controller;
            }

            // O primeiro jogador de campo (não goleiro) começa controlado pelo humano
            if (_teamIndex == 0)
                SelectInitialHumanPlayer();

            Debug.Log($"[TeamManager] {_teamData.TeamName}: {PlayersOnField.Count} jogadores em campo.");
        }

        /// <summary>
        /// Seleciona o atacante mais avançado como jogador inicial do humano.
        /// </summary>
        private void SelectInitialHumanPlayer()
        {
            PlayerController best = null;

            foreach (PlayerController player in PlayersOnField)
            {
                if (player == Goalkeeper) continue;

                if (best == null ||
                    player.Data?.Position == PlayerPosition.Attacker ||
                    player.Data?.Position == PlayerPosition.Midfielder)
                {
                    best = player;
                }
            }

            if (best != null)
                SwitchHumanControl(best);
        }

        // ===== CONTROLE DO JOGADOR =====

        /// <summary>
        /// Transfere o controle humano para o jogador mais próximo da bola.
        /// </summary>
        /// <param name="ballPosition">Posição atual da bola.</param>
        public void SwitchToNearestPlayer(Vector3 ballPosition)
        {
            PlayerController nearest = GetNearestPlayerToBall(ballPosition);
            if (nearest != null && nearest != HumanControlledPlayer)
                SwitchHumanControl(nearest);
        }

        /// <summary>
        /// Transfere o controle humano para o próximo jogador na lista (Tab/R1).
        /// </summary>
        public void SwitchToNextPlayer()
        {
            if (PlayersOnField.Count <= 1) return;

            int currentIndex = HumanControlledPlayer != null
                ? PlayersOnField.IndexOf(HumanControlledPlayer)
                : -1;

            // Procura o próximo jogador de campo (pula o goleiro)
            for (int i = 1; i <= PlayersOnField.Count; i++)
            {
                int nextIndex = (currentIndex + i) % PlayersOnField.Count;
                PlayerController candidate = PlayersOnField[nextIndex];

                if (candidate != Goalkeeper)
                {
                    SwitchHumanControl(candidate);
                    return;
                }
            }
        }

        /// <summary>
        /// Aplica o controle humano ao jogador especificado e remove dos outros.
        /// </summary>
        private void SwitchHumanControl(PlayerController newPlayer)
        {
            HumanControlledPlayer?.ReleaseControl();

            HumanControlledPlayer = newPlayer;
            HumanControlledPlayer.TakeControl();

            OnControlledPlayerChanged?.Invoke(newPlayer);

            Debug.Log($"[TeamManager] Controle: {newPlayer.Data?.PlayerName}");
        }

        // ===== POSICIONAMENTO =====

        /// <summary>
        /// Retorna todos os jogadores às posições da formação.
        /// </summary>
        public void RepositionToFormation(FormationSO formation)
        {
            if (formation == null) return;

            for (int i = 0; i < PlayersOnField.Count && i < formation.Positions.Count; i++)
            {
                // TODO: converter posição normalizada para coordenada mundial
                // usando os bounds do campo quando o FieldController estiver implementado
                Debug.Log($"[TeamManager] Reposicionando {PlayersOnField[i].Data?.PlayerName}");
            }
        }

        /// <summary>
        /// Inverte o lado do time (para o segundo tempo).
        /// </summary>
        public void InvertSide()
        {
            _sideInverted = !_sideInverted;
            // As posições de formação são espelhadas — tratado em RepositionToFormation
        }

        // ===== UTILITÁRIOS =====

        /// <summary>
        /// Retorna o jogador do time mais próximo de uma posição.
        /// </summary>
        /// <param name="position">Posição de referência.</param>
        /// <param name="excludeGoalkeeper">Se true, exclui o goleiro da busca.</param>
        public PlayerController GetNearestPlayerToBall(Vector3 position, bool excludeGoalkeeper = true)
        {
            PlayerController nearest = null;
            float minDist = float.MaxValue;

            foreach (PlayerController player in PlayersOnField)
            {
                if (excludeGoalkeeper && player == Goalkeeper) continue;

                float dist = Vector3.Distance(player.transform.position, position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = player;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Retorna todos os jogadores de campo (sem o goleiro).
        /// </summary>
        public List<PlayerController> GetOutfieldPlayers()
        {
            List<PlayerController> outfield = new List<PlayerController>();
            foreach (PlayerController p in PlayersOnField)
            {
                if (p != Goalkeeper)
                    outfield.Add(p);
            }
            return outfield;
        }
    }
}
