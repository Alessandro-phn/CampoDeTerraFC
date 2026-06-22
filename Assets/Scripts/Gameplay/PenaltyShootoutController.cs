using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CampoDeTerraFC.Ball;
using CampoDeTerraFC.Config;
using CampoDeTerraFC.Core;
using CampoDeTerraFC.Core.Services;
using CampoDeTerraFC.Data;
using CampoDeTerraFC.Goalkeeper;

namespace CampoDeTerraFC.Gameplay
{
    /// <summary>
    /// Gerencia a disputa completa de pênaltis.
    /// Alterna cobranças entre os dois times, aplica morte súbita e determina o vencedor.
    /// Controla o estado do jogador (mira, força) e da IA do goleiro.
    /// </summary>
    public sealed class PenaltyShootoutController : MonoBehaviour
    {
        // ===== REFERÊNCIAS =====

        [Header("Referências de Cena")]
        [SerializeField] private BallController _ball;
        [SerializeField] private GoalkeeperController _goalkeeperA;
        [SerializeField] private GoalkeeperController _goalkeeperB;
        [SerializeField] private Transform _penaltySpotA;
        [SerializeField] private Transform _penaltySpotB;
        [SerializeField] private Transform _goalCenterA;
        [SerializeField] private Transform _goalCenterB;

        [Header("Configuração")]
        [SerializeField] private int _kicksPerTeam = 5;
        [SerializeField] private float _aimSweepSpeed = 45f;   // graus/segundo para a mira
        [SerializeField] private float _maxAimAngle = 35f;     // ângulo máximo da mira (graus)
        [SerializeField] private float _maxChargeTime = 2f;    // tempo máximo de carregamento

        // ===== ESTADO =====

        /// <summary>Fase atual da disputa.</summary>
        public PenaltyShootoutPhase Phase { get; private set; } = PenaltyShootoutPhase.NotStarted;

        /// <summary>Kicks do time A registrados.</summary>
        public List<PenaltyKickResult> ResultsTeamA { get; private set; } = new List<PenaltyKickResult>();

        /// <summary>Kicks do time B registrados.</summary>
        public List<PenaltyKickResult> ResultsTeamB { get; private set; } = new List<PenaltyKickResult>();

        /// <summary>Placar de gols em pênaltis do time A.</summary>
        public int ScoreA => CountGoals(ResultsTeamA);

        /// <summary>Placar de gols em pênaltis do time B.</summary>
        public int ScoreB => CountGoals(ResultsTeamB);

        /// <summary>Se é a vez do time A chutar.</summary>
        public bool IsTeamATurn { get; private set; } = true;

        /// <summary>Número da cobrança atual (1–5, depois morte súbita).</summary>
        public int CurrentKickNumber { get; private set; } = 0;

        // ===== CONTROLE DA MIRA (JOGADOR HUMANO) =====

        private float _currentAimAngle;     // ângulo atual da mira em graus
        private float _aimDirection;        // -1 = esquerda, 1 = direita
        private float _chargeAmount;        // 0-1
        private bool _isAiming;
        private bool _isCharging;
        private float _chargeStartTime;

        // ===== REFERÊNCIAS DE DADOS =====

        private GameConfigSO _config;

        // ===== EVENTOS =====

        public event Action<PenaltyKickResult> OnKickCompleted;
        public event Action<int> OnShootoutEnd;     // índice do vencedor (-1 = erro)
        public event Action<bool> OnAimChanged;     // true = virou a mira

        // ===== CONSTANTES =====

        private const float KICK_ANIMATION_DURATION = 1.2f;
        private const float GOALKEEPER_DIVE_DELAY = 0.1f;  // Goleiro mergulha levemente depois do chute
        private const float RESET_DELAY = 2.5f;

        // ===== UNITY LIFECYCLE =====

        private void Awake()
        {
            _config = Core.GameManager.Instance?.GetConfig();
        }

        private void Update()
        {
            if (Phase != PenaltyShootoutPhase.Aiming) return;

            UpdateAim();
        }

        // ===== INICIALIZAÇÃO =====

        /// <summary>
        /// Inicia a disputa de pênaltis.
        /// </summary>
        public void StartShootout()
        {
            ResultsTeamA.Clear();
            ResultsTeamB.Clear();
            CurrentKickNumber = 0;
            IsTeamATurn = true;

            Phase = PenaltyShootoutPhase.Aiming;

            Debug.Log("[PenaltyShootoutController] Disputa de pênaltis iniciada!");

            StartCoroutine(NextKickSequence());
        }

        // ===== SEQUÊNCIA DE COBRANÇA =====

        /// <summary>
        /// Sequência completa de uma cobrança: posicionamento → mira → chute → defesa → resultado.
        /// </summary>
        private IEnumerator NextKickSequence()
        {
            CurrentKickNumber++;

            // Posiciona a bola no pênalti certo
            Vector3 spot = IsTeamATurn
                ? _penaltySpotA.position
                : _penaltySpotB.position;

            _ball.transform.position = spot;
            _ball.DetachFromPlayer();

            Phase = PenaltyShootoutPhase.Aiming;
            _currentAimAngle = 0f;
            _chargeAmount = 0f;
            _isAiming = true;

            // Ativa o goleiro correto
            GoalkeeperController activeGoalkeeper = IsTeamATurn ? _goalkeeperB : _goalkeeperA;
            activeGoalkeeper?.EnterPenaltyMode();

            // Espera o jogador humano chutar (ou timeout da IA)
            yield return WaitForKick();
        }

        /// <summary>
        /// Aguarda o input de chute (humano) ou executa o chute automático (IA).
        /// </summary>
        private IEnumerator WaitForKick()
        {
            // Para jogador humano: input detectado no HandleShootInput()
            // Para IA: espera um tempo e chuta automaticamente
            bool isHumanKick = IsTeamATurn; // Simplificado: Time A = humano

            if (!isHumanKick)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 2.5f));
                ExecuteAIKick();
            }
            // Se humano, aguarda até HandleShootInput() ser chamado pelo InputManager
        }

        // ===== CONTROLES DO JOGADOR =====

        /// <summary>
        /// Atualiza a mira do jogador (balança de lado a lado).
        /// Chamado pelo InputManager via MoveInput.
        /// </summary>
        public void SetAimDirection(float horizontal)
        {
            if (Phase != PenaltyShootoutPhase.Aiming) return;
            _aimDirection = horizontal;
        }

        /// <summary>
        /// Inicia o carregamento de força (botão de chute pressionado).
        /// </summary>
        public void StartCharge()
        {
            if (Phase != PenaltyShootoutPhase.Aiming || !_isAiming) return;

            _isCharging = true;
            _chargeStartTime = Time.time;
        }

        /// <summary>
        /// Finaliza o chute ao soltar o botão.
        /// </summary>
        public void ReleaseShoot()
        {
            if (!_isCharging) return;

            float elapsed = Time.time - _chargeStartTime;
            _chargeAmount = Mathf.Clamp01(elapsed / _maxChargeTime);
            _isCharging = false;
            _isAiming = false;

            StartCoroutine(ExecuteHumanKick());
        }

        // ===== EXECUÇÃO DO CHUTE =====

        /// <summary>
        /// Executa o chute do jogador humano com os parâmetros de mira e força capturados.
        /// </summary>
        private IEnumerator ExecuteHumanKick()
        {
            Phase = PenaltyShootoutPhase.Kicking;

            // Converte ângulo de mira em direção de chute
            float angleRad = _currentAimAngle * Mathf.Deg2Rad;
            Vector3 kickDir = new Vector3(
                Mathf.Sin(angleRad),
                0.25f + (1f - _chargeAmount) * 0.15f,  // Mais força = mais rasteiro
                IsTeamATurn ? 1f : -1f).normalized;

            // Aplica precisão baseada na força carregada (muita força = menos precisão)
            float precisionNoise = (1f - _chargeAmount) * 0.05f;
            kickDir += new Vector3(
                UnityEngine.Random.Range(-precisionNoise, precisionNoise),
                UnityEngine.Random.Range(-precisionNoise * 0.5f, precisionNoise * 0.5f),
                0f);

            kickDir.Normalize();

            float force = Mathf.Lerp(12f, 25f, _chargeAmount);
            _ball.ApplyShoot(kickDir, force, _currentAimAngle * 0.3f);

            yield return StartCoroutine(ResolveKick());
        }

        /// <summary>
        /// A IA determina mira e força e chuta automaticamente.
        /// </summary>
        private void ExecuteAIKick()
        {
            Phase = PenaltyShootoutPhase.Kicking;

            // IA escolhe um canto aleatório com bias para longe do centro
            float aiAngle = UnityEngine.Random.Range(-_maxAimAngle, _maxAimAngle);
            float aiCharge = UnityEngine.Random.Range(0.6f, 1f);

            float angleRad = aiAngle * Mathf.Deg2Rad;
            Vector3 kickDir = new Vector3(
                Mathf.Sin(angleRad),
                0.25f,
                IsTeamATurn ? 1f : -1f).normalized;

            float force = Mathf.Lerp(12f, 22f, aiCharge);
            _ball.ApplyShoot(kickDir, force, aiAngle * 0.2f);

            StartCoroutine(ResolveKick());
        }

        /// <summary>
        /// Aguarda a bola parar e determina o resultado (gol ou defesa).
        /// </summary>
        private IEnumerator ResolveKick()
        {
            // Aguarda animação de chute e a bola atingir o gol ou ser defendida
            yield return new WaitForSeconds(KICK_ANIMATION_DURATION);

            // Determina resultado baseado na posição da bola e ação do goleiro
            bool isGoal = DetermineKickOutcome();

            PenaltyKickResult result = new PenaltyKickResult
            {
                TeamIndex = IsTeamATurn ? 0 : 1,
                KickNumber = CurrentKickNumber,
                IsGoal = isGoal
            };

            if (IsTeamATurn) ResultsTeamA.Add(result);
            else ResultsTeamB.Add(result);

            OnKickCompleted?.Invoke(result);

            Debug.Log($"[PenaltyShootout] Cobrança {CurrentKickNumber} - Time {result.TeamIndex}: {(isGoal ? "GOL!" : "DEFENDIDO!")}");

            yield return new WaitForSeconds(RESET_DELAY);

            // Verifica se a disputa acabou
            if (CheckShootoutEnd()) yield break;

            // Alterna o time
            IsTeamATurn = !IsTeamATurn;
            Phase = PenaltyShootoutPhase.Aiming;

            StartCoroutine(NextKickSequence());
        }

        // ===== LÓGICA DE RESULTADO =====

        /// <summary>
        /// Determina se a cobrança resultou em gol.
        /// Cruza a mira do cobrador com a decisão do goleiro.
        /// </summary>
        private bool DetermineKickOutcome()
        {
            // Base: 70% de chance de gol em pênalti (média real da FIFA)
            float baseGoalProbability = 0.70f;

            // Modifica por força: muito fraco = fácil de defender
            float forceMod = Mathf.Lerp(-0.15f, 0.1f, _chargeAmount);

            // Modifica por ângulo: centro = mais fácil de defender
            float angleMod = Mathf.Abs(_currentAimAngle) / _maxAimAngle * 0.1f;

            float finalProbability = Mathf.Clamp01(baseGoalProbability + forceMod + angleMod);

            return UnityEngine.Random.value < finalProbability;
        }

        /// <summary>
        /// Verifica se a disputa terminou e determina o vencedor.
        /// </summary>
        private bool CheckShootoutEnd()
        {
            int kicked = Mathf.Max(ResultsTeamA.Count, ResultsTeamB.Count);

            // Verificação de término antecipado (quando um time já não pode mais alcançar o outro)
            if (kicked >= _kicksPerTeam || kicked > _kicksPerTeam)
            {
                bool canTeamAWin = ScoreA + (_kicksPerTeam - ResultsTeamA.Count) >= ScoreB;
                bool canTeamBWin = ScoreB + (_kicksPerTeam - ResultsTeamB.Count) >= ScoreA;

                if (!canTeamAWin || !canTeamBWin)
                {
                    int winner = ScoreA > ScoreB ? 0 : ScoreB > ScoreA ? 1 : -1;

                    if (winner != -1)
                    {
                        EndShootout(winner);
                        return true;
                    }
                }
            }

            // Ambos os times cobraram todas as 5
            if (ResultsTeamA.Count >= _kicksPerTeam && ResultsTeamB.Count >= _kicksPerTeam)
            {
                if (ScoreA != ScoreB)
                {
                    EndShootout(ScoreA > ScoreB ? 0 : 1);
                    return true;
                }
                // Morte súbita: continua
                Debug.Log("[PenaltyShootout] Morte súbita!");
            }

            return false;
        }

        private void EndShootout(int winnerIndex)
        {
            Phase = PenaltyShootoutPhase.Finished;
            OnShootoutEnd?.Invoke(winnerIndex);

            Debug.Log($"[PenaltyShootout] FIM! Vencedor: Time {winnerIndex} | {ScoreA}x{ScoreB}");
        }

        // ===== ATUALIZAÇÃO DA MIRA =====

        private void UpdateAim()
        {
            if (!_isAiming) return;

            // Mira balança de acordo com o input
            _currentAimAngle += _aimDirection * _aimSweepSpeed * Time.deltaTime;
            _currentAimAngle = Mathf.Clamp(_currentAimAngle, -_maxAimAngle, _maxAimAngle);

            // Força do carregamento
            if (_isCharging)
            {
                float elapsed = Time.time - _chargeStartTime;
                _chargeAmount = Mathf.Clamp01(elapsed / _maxChargeTime);
            }
        }

        // ===== UTILITÁRIOS =====

        private int CountGoals(List<PenaltyKickResult> results)
        {
            int count = 0;
            foreach (PenaltyKickResult r in results)
                if (r.IsGoal) count++;
            return count;
        }

        /// <summary>
        /// Retorna o ângulo atual da mira (para a UI exibir).
        /// </summary>
        public float GetCurrentAimAngle() => _currentAimAngle;

        /// <summary>
        /// Retorna a força carregada atual (0-1).
        /// </summary>
        public float GetChargeAmount() => _chargeAmount;
    }

    // ===== TIPOS =====

    /// <summary>Fases da disputa de pênaltis.</summary>
    public enum PenaltyShootoutPhase
    {
        NotStarted,
        Aiming,
        Kicking,
        Resolving,
        Finished
    }

    /// <summary>Resultado de uma cobrança de pênalti.</summary>
    [Serializable]
    public struct PenaltyKickResult
    {
        public int TeamIndex;
        public int KickNumber;
        public bool IsGoal;
    }
}
