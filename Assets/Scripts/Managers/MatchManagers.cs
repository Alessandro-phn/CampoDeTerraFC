using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampoDeTerraFC.Managers
{
    // ==========================================================================
    //  SCORE MANAGER
    // ==========================================================================

    /// <summary>
    /// Gerencia o placar da partida.
    /// FUSÃO: Histórico e eventos do Sprint1 + atualização automática do HUD do Unity2022.
    /// </summary>
    public sealed class ScoreManager : MonoBehaviour
    {
        public int ScoreA { get; private set; }
        public int ScoreB { get; private set; }
        public List<GoalRecord> GoalHistory { get; private set; } = new List<GoalRecord>();

        public event Action<int, int> OnScoreChanged;

        public void AddGoal(int teamIndex, string scorer = "Jogador", float time = 0f)
        {
            if      (teamIndex == 0) ScoreA++;
            else if (teamIndex == 1) ScoreB++;
            else { Debug.LogError("[ScoreManager] Índice inválido: " + teamIndex); return; }

            GoalHistory.Add(new GoalRecord
            {
                TeamIndex         = teamIndex,
                ScorerName        = scorer,
                MatchTimeSeconds  = time
            });

            OnScoreChanged?.Invoke(ScoreA, ScoreB);

            // Atualiza o HUD diretamente (compatibilidade Unity2022)
            var hud = FindObjectOfType<UI.HUDController>();
            hud?.UpdateScore(ScoreA, ScoreB);

            Debug.Log(string.Format("[ScoreManager] GOL! {0} x {1} | {2} ({3:F0}s)",
                ScoreA, ScoreB, scorer, time));
        }

        public void ResetScore()
        {
            ScoreA = 0; ScoreB = 0;
            GoalHistory.Clear();
            OnScoreChanged?.Invoke(0, 0);
        }

        public int    GetWinner()      => ScoreA > ScoreB ? 0 : ScoreB > ScoreA ? 1 : -1;
        public string GetScoreString() => string.Format("{0} x {1}", ScoreA, ScoreB);
    }

    [Serializable]
    public struct GoalRecord
    {
        public int    TeamIndex;
        public string ScorerName;
        public float  MatchTimeSeconds;
    }

    // ==========================================================================
    //  TIMER MANAGER
    // ==========================================================================

    /// <summary>
    /// Gerencia o cronômetro da partida com pause/resume.
    /// FUSÃO: Sistema completo do Sprint1 + atualização automática do HUD do Unity2022.
    /// </summary>
    public sealed class TimerManager : MonoBehaviour
    {
        public float RemainingTime  { get; private set; }
        public float ElapsedTime    { get; private set; }
        public float TotalDuration  { get; private set; }
        public bool  IsRunning      { get; private set; }

        private float _lastTickTime;
        private const float TICK_INTERVAL = 1f;

        public event Action<float> OnTimerTick;
        public event Action        OnTimerExpired;

        private void Update()
        {
            if (!IsRunning) return;
            RemainingTime  = Mathf.Max(0f, RemainingTime - Time.deltaTime);
            ElapsedTime   += Time.deltaTime;

            if (Time.time - _lastTickTime >= TICK_INTERVAL)
            {
                _lastTickTime = Time.time;
                OnTimerTick?.Invoke(RemainingTime);

                // Atualiza HUD
                FindObjectOfType<UI.HUDController>()?.UpdateTimer(RemainingTime);
            }

            if (RemainingTime <= 0f)
            {
                IsRunning = false;
                OnTimerExpired?.Invoke();
                Debug.Log("[TimerManager] Tempo esgotado.");
            }
        }

        public void StartTimer(float duration)
        {
            TotalDuration = duration;
            RemainingTime = duration;
            ElapsedTime   = 0f;
            IsRunning     = true;
            _lastTickTime = Time.time;
            Debug.Log("[TimerManager] Cronômetro: " + duration + "s");
        }

        public void PauseTimer()  { IsRunning = false; }
        public void ResumeTimer() { if (RemainingTime > 0f) IsRunning = true; }
        public void StopTimer()   { IsRunning = false; RemainingTime = 0f; }

        public string GetFormattedTime()
        {
            int m = Mathf.FloorToInt(RemainingTime / 60f);
            int s = Mathf.FloorToInt(RemainingTime % 60f);
            return string.Format("{0:00}:{1:00}", m, s);
        }

        public float GetProgress() => TotalDuration > 0f ? 1f - (RemainingTime / TotalDuration) : 0f;
    }
}
