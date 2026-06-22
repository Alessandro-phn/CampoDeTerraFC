using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CampoDeTerraFC.Ball;
using CampoDeTerraFC.Core;
using CampoDeTerraFC.Core.Events;
using CampoDeTerraFC.Managers;

namespace CampoDeTerraFC.Gameplay
{
    /// <summary>
    /// Controla o fluxo completo de uma partida.
    /// FUSÃO: Lógica completa do Sprint1 (kickoff, halftime, prorrogação, set pieces, fase final)
    ///        + referências diretas do Unity2022 (sem ServiceLocator no Start, FindObjectOfType robusto).
    /// </summary>
    public sealed class MatchController : MonoBehaviour
    {
        // ===== REFERÊNCIAS (injetadas pelo ProjectSetup ou serializadas) =====
        [Header("Referências")]
        [SerializeField] private BallController _ball;
        [SerializeField] private Transform      _ballSpawnPoint;

        [Header("Posições de Spawn")]
        [SerializeField] private Transform[] _spawnPointsA;
        [SerializeField] private Transform[] _spawnPointsB;

        [Header("Eventos (ScriptableObject)")]
        [SerializeField] private GoalEventSO  _onGoalScored;
        [SerializeField] private GameEventSO  _onMatchStart;
        [SerializeField] private GameEventSO  _onMatchEnd;

        [Header("Configuração")]
        [SerializeField] private float _matchDurationSeconds = 600f;
        [SerializeField] private bool  _enableExtraTime      = true;
        [SerializeField] private float _extraTimeDuration    = 180f;

        // ===== SUB-SISTEMAS =====
        private ScoreManager  _score;
        private TimerManager  _timer;
        private RulesEngine   _rules;
        private UI.HUDController _hud;

        // ===== ESTADO =====
        public MatchState  State   { get; private set; } = MatchState.NotStarted;
        public MatchPhase  Phase   { get; private set; } = MatchPhase.FirstHalf;

        private bool _processingGoal;
        private bool _matchActive;

        // ===== POSIÇÕES PADRÃO (fallback quando SpawnPoints não configurados) =====
        private static readonly Vector3[] DefaultPosA =
        {
            new Vector3(-6f, 0.9f, -20f), new Vector3(6f, 0.9f, -20f),
            new Vector3(0f,  0.9f, -14f), new Vector3(-5f, 0.9f, -8f), new Vector3(5f, 0.9f, -8f)
        };
        private static readonly Vector3[] DefaultPosB =
        {
            new Vector3(-6f, 0.9f, 20f), new Vector3(6f, 0.9f, 20f),
            new Vector3(0f,  0.9f, 14f), new Vector3(-5f, 0.9f,  8f), new Vector3(5f, 0.9f,  8f)
        };

        // ===== UNITY LIFECYCLE =====
        private void Start()
        {
            _ball  = _ball  ?? FindObjectOfType<BallController>();
            _score = FindObjectOfType<ScoreManager>();
            _timer = FindObjectOfType<TimerManager>();
            _rules = FindObjectOfType<RulesEngine>();
            _hud   = FindObjectOfType<UI.HUDController>();

            if (_ball == null)  Debug.LogError("[MatchController] BallController não encontrado.");
            if (_score == null) Debug.LogError("[MatchController] ScoreManager não encontrado.");
            if (_timer == null) Debug.LogError("[MatchController] TimerManager não encontrado.");

            StartCoroutine(KickoffSequence());
        }

        private void OnDestroy()
        {
            if (_timer != null) _timer.OnTimerExpired -= HandleTimerExpired;
        }

        // ===== INJEÇÃO PELO ProjectSetup =====
        public void SetBall(BallController ball) => _ball = ball;

        // ===== KICKOFF =====
        private IEnumerator KickoffSequence()
        {
            _matchActive = false;
            ChangeState(MatchState.Kickoff);
            SetInputEnabled(false);
            RepositionBall();
            RepositionPlayers();
            _score?.ResetScore();
            _onMatchStart?.Raise();
            _hud?.UpdateScore(0, 0);

            yield return new WaitForSeconds(2.5f);

            SetInputEnabled(true);
            _matchActive = true;
            ChangeState(MatchState.Playing);
            _timer?.StartTimer(_matchDurationSeconds / 2f);
            _timer.OnTimerExpired += HandleTimerExpired;

            Debug.Log("[MatchController] Jogo em andamento!");
        }

        // ===== GOL =====
        public void RegisterGoal(int scoringTeam)
        {
            if (!_matchActive || _processingGoal) return;
            StartCoroutine(GoalSequence(scoringTeam));
        }

        private IEnumerator GoalSequence(int scoringTeam)
        {
            _processingGoal = true;
            _matchActive    = false;
            ChangeState(MatchState.Goal);

            SetInputEnabled(false);
            _timer?.PauseTimer();
            _rules?.IsActive.Equals(false); // pausar detecção

            string scorer = GetScorerName(scoringTeam);
            float  time   = _timer?.ElapsedTime ?? 0f;
            _score?.AddGoal(scoringTeam, scorer, time);

            // Dispara evento ScriptableObject
            if (_onGoalScored != null)
            {
                _onGoalScored.Raise(new GoalEventData
                {
                    ScoringTeamIndex = scoringTeam,
                    ScorerName       = scorer,
                    MatchTime        = time,
                    GoalType         = GoalType.Normal
                });
            }

            _hud?.ShowGoal(scoringTeam);

            // Vibra câmera
            FindObjectOfType<Camera.MatchCamera>()?.Shake(0.5f, 0.6f);

            yield return new WaitForSeconds(3f);

            _hud?.HideGoal();

            // Replay (se disponível)
            var replay = FindObjectOfType<ReplaySystem>();
            if (replay != null)
            {
                ChangeState(MatchState.GoalReplay);
                replay.PlayGoalReplay(transform.position);
                yield return new WaitForSeconds(4f);
            }

            RepositionBall();
            RepositionPlayers();
            yield return new WaitForSeconds(2f);

            SetInputEnabled(true);
            _matchActive    = true;
            _processingGoal = false;
            ChangeState(MatchState.Playing);
            _timer?.ResumeTimer();
            if (_rules != null) _rules.IsActive = true;
        }

        // ===== CRONÔMETRO =====
        private void HandleTimerExpired()
        {
            _timer.OnTimerExpired -= HandleTimerExpired;

            switch (Phase)
            {
                case MatchPhase.FirstHalf:
                    StartCoroutine(HalftimeSequence());
                    break;
                case MatchPhase.SecondHalf:
                    HandleFullTime();
                    break;
                case MatchPhase.ExtraTimeFirst:
                    Phase = MatchPhase.ExtraTimeSecond;
                    _timer.OnTimerExpired += HandleTimerExpired;
                    _timer.StartTimer(_extraTimeDuration / 2f);
                    break;
                case MatchPhase.ExtraTimeSecond:
                    if (_score != null && _score.ScoreA == _score.ScoreB)
                        SceneManager.LoadScene("Penalty");
                    else
                        EndMatch();
                    break;
            }
        }

        private IEnumerator HalftimeSequence()
        {
            ChangeState(MatchState.Halftime);
            SetInputEnabled(false);
            Phase = MatchPhase.SecondHalf;
            yield return new WaitForSeconds(3f);
            RepositionBall();
            RepositionPlayers();
            yield return new WaitForSeconds(2f);
            SetInputEnabled(true);
            _matchActive = true;
            ChangeState(MatchState.Playing);
            _timer.OnTimerExpired += HandleTimerExpired;
            _timer.StartTimer(_matchDurationSeconds / 2f);
            Debug.Log("[MatchController] Segundo tempo!");
        }

        private void HandleFullTime()
        {
            bool draw      = _score != null && _score.ScoreA == _score.ScoreB;
            bool isKnockout = GameManager.Instance?.CurrentGameMode == GameMode.Championship;

            if (draw && isKnockout && _enableExtraTime)
            {
                Phase = MatchPhase.ExtraTimeFirst;
                _timer.OnTimerExpired += HandleTimerExpired;
                _timer.StartTimer(_extraTimeDuration / 2f);
                Debug.Log("[MatchController] Prorrogação!");
            }
            else EndMatch();
        }

        private void EndMatch()
        {
            _matchActive = false;
            SetInputEnabled(false);
            ChangeState(MatchState.Finished);
            _onMatchEnd?.Raise();
            GameManager.Instance?.ChangeState(GameState.MatchEnd);
            _hud?.ShowMatchEnd(_score?.ScoreA ?? 0, _score?.ScoreB ?? 0);
            Debug.Log(string.Format("[MatchController] FIM DE JOGO! {0} x {1}",
                _score?.ScoreA, _score?.ScoreB));
        }

        // ===== UTILITÁRIOS =====
        private void ChangeState(MatchState s) { State = s; }

        private void SetInputEnabled(bool enabled)
        {
            Input.InputManager.Instance?.SetInputEnabled(enabled);
        }

        private void RepositionBall()
        {
            if (_ball == null) return;
            _ball.DetachFromPlayer();
            Vector3 pos = _ballSpawnPoint != null ? _ballSpawnPoint.position : new Vector3(0f, 0.11f, 0f);
            _ball.transform.position = pos;
            var rb = _ball.GetComponent<Rigidbody>();
            if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        }

        private void RepositionPlayers()
        {
            var players = FindObjectsOfType<Player.PlayerController>();
            int iA = 0, iB = 0;
            foreach (var p in players)
            {
                var rb = p.GetComponent<Rigidbody>();
                if (p.TeamIndex == 0 && iA < DefaultPosA.Length)
                {
                    p.transform.position = _spawnPointsA != null && iA < _spawnPointsA.Length
                        ? _spawnPointsA[iA].position : DefaultPosA[iA];
                    if (rb) rb.velocity = Vector3.zero;
                    iA++;
                }
                else if (p.TeamIndex == 1 && iB < DefaultPosB.Length)
                {
                    p.transform.position = _spawnPointsB != null && iB < _spawnPointsB.Length
                        ? _spawnPointsB[iB].position : DefaultPosB[iB];
                    if (rb) rb.velocity = Vector3.zero;
                    iB++;
                }
            }
        }

        private string GetScorerName(int teamIndex)
        {
            foreach (var p in FindObjectsOfType<Player.PlayerController>())
                if (p.TeamIndex == teamIndex && p.IsControlledByHuman)
                    return p.Data?.PlayerName ?? "Jogador";
            return "Jogador";
        }
    }

    // ===== ENUMS =====
    public enum MatchState  { NotStarted, Kickoff, Playing, Goal, GoalReplay, Halftime, Finished, PenaltyShootout }
    public enum MatchPhase  { FirstHalf, SecondHalf, ExtraTimeFirst, ExtraTimeSecond }
    public enum SetPieceType{ None, Kickoff, Corner, ThrowIn, FreeKick, Penalty, GoalKick }
}
