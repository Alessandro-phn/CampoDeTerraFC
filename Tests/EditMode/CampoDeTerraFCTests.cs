using NUnit.Framework;
using UnityEngine;
using CampoDeTerraFC.Managers;
using CampoDeTerraFC.Gameplay;
using CampoDeTerraFC.SaveSystem;
using CampoDeTerraFC.Data;
using System.Collections.Generic;

namespace CampoDeTerraFC.Tests.EditMode
{
    // ==========================================================================
    //  SCORE MANAGER TESTS
    // ==========================================================================

    /// <summary>
    /// Testes unitários para o ScoreManager.
    /// Valida lógica de placar, histórico de gols e determinação de vencedor.
    /// </summary>
    [TestFixture]
    public class ScoreManagerTests
    {
        private GameObject _go;
        private ScoreManager _scoreManager;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject("TestScoreManager");
            _scoreManager = _go.AddComponent<ScoreManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void ScoreManager_InitialScore_IsZeroZero()
        {
            Assert.AreEqual(0, _scoreManager.ScoreA, "Placar inicial do Time A deve ser 0.");
            Assert.AreEqual(0, _scoreManager.ScoreB, "Placar inicial do Time B deve ser 0.");
        }

        [Test]
        public void ScoreManager_AddGoalTeamA_IncrementsScoreA()
        {
            _scoreManager.AddGoal(0, "Pedrão", 120f);
            Assert.AreEqual(1, _scoreManager.ScoreA, "ScoreA deve ser 1 após um gol do Time A.");
            Assert.AreEqual(0, _scoreManager.ScoreB, "ScoreB não deve mudar.");
        }

        [Test]
        public void ScoreManager_AddGoalTeamB_IncrementsScoreB()
        {
            _scoreManager.AddGoal(1, "Neguinho", 300f);
            Assert.AreEqual(0, _scoreManager.ScoreA);
            Assert.AreEqual(1, _scoreManager.ScoreB);
        }

        [Test]
        public void ScoreManager_MultipleGoals_AccumulatesCorrectly()
        {
            _scoreManager.AddGoal(0, "A", 10f);
            _scoreManager.AddGoal(0, "A", 20f);
            _scoreManager.AddGoal(1, "B", 30f);

            Assert.AreEqual(2, _scoreManager.ScoreA);
            Assert.AreEqual(1, _scoreManager.ScoreB);
        }

        [Test]
        public void ScoreManager_Reset_ClearsAllData()
        {
            _scoreManager.AddGoal(0, "A", 10f);
            _scoreManager.AddGoal(1, "B", 20f);
            _scoreManager.ResetScore();

            Assert.AreEqual(0, _scoreManager.ScoreA);
            Assert.AreEqual(0, _scoreManager.ScoreB);
            Assert.AreEqual(0, _scoreManager.GoalHistory.Count);
        }

        [Test]
        public void ScoreManager_GoalHistory_RecordsCorrectly()
        {
            _scoreManager.AddGoal(0, "Tigrão", 450f);

            Assert.AreEqual(1, _scoreManager.GoalHistory.Count);
            Assert.AreEqual("Tigrão", _scoreManager.GoalHistory[0].ScorerName);
            Assert.AreEqual(0, _scoreManager.GoalHistory[0].TeamIndex);
            Assert.AreEqual(450f, _scoreManager.GoalHistory[0].MatchTimeSeconds);
        }

        [Test]
        public void ScoreManager_GetWinner_TeamAWins()
        {
            _scoreManager.AddGoal(0, "A", 10f);
            Assert.AreEqual(0, _scoreManager.GetWinner());
        }

        [Test]
        public void ScoreManager_GetWinner_TeamBWins()
        {
            _scoreManager.AddGoal(1, "B", 10f);
            Assert.AreEqual(1, _scoreManager.GetWinner());
        }

        [Test]
        public void ScoreManager_GetWinner_Draw_ReturnsMinusOne()
        {
            _scoreManager.AddGoal(0, "A", 10f);
            _scoreManager.AddGoal(1, "B", 20f);
            Assert.AreEqual(-1, _scoreManager.GetWinner());
        }

        [Test]
        public void ScoreManager_GetScoreString_FormatsCorrectly()
        {
            _scoreManager.AddGoal(0, "A", 10f);
            _scoreManager.AddGoal(0, "A", 20f);
            _scoreManager.AddGoal(1, "B", 30f);

            Assert.AreEqual("2 x 1", _scoreManager.GetScoreString());
        }

        [Test]
        public void ScoreManager_InvalidTeamIndex_DoesNotChangeScore()
        {
            _scoreManager.AddGoal(5, "X", 10f); // Índice inválido
            Assert.AreEqual(0, _scoreManager.ScoreA);
            Assert.AreEqual(0, _scoreManager.ScoreB);
        }

        [Test]
        public void ScoreManager_OnScoreChanged_FiresOnGoal()
        {
            bool eventFired = false;
            _scoreManager.OnScoreChanged += (a, b) => eventFired = true;

            _scoreManager.AddGoal(0, "A", 10f);

            Assert.IsTrue(eventFired, "OnScoreChanged deve ser disparado ao marcar gol.");
        }
    }

    // ==========================================================================
    //  TIMER MANAGER TESTS
    // ==========================================================================

    /// <summary>
    /// Testes unitários para o TimerManager.
    /// </summary>
    [TestFixture]
    public class TimerManagerTests
    {
        private GameObject _go;
        private TimerManager _timerManager;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject("TestTimerManager");
            _timerManager = _go.AddComponent<TimerManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void TimerManager_Start_SetsCorrectDuration()
        {
            _timerManager.StartTimer(600f);
            Assert.AreEqual(600f, _timerManager.TotalDuration, 0.01f);
            Assert.AreEqual(600f, _timerManager.RemainingTime, 0.01f);
            Assert.IsTrue(_timerManager.IsRunning);
        }

        [Test]
        public void TimerManager_Pause_StopsRunning()
        {
            _timerManager.StartTimer(300f);
            _timerManager.PauseTimer();
            Assert.IsFalse(_timerManager.IsRunning);
        }

        [Test]
        public void TimerManager_Resume_StartsAgain()
        {
            _timerManager.StartTimer(300f);
            _timerManager.PauseTimer();
            _timerManager.ResumeTimer();
            Assert.IsTrue(_timerManager.IsRunning);
        }

        [Test]
        public void TimerManager_Stop_ResetsToZero()
        {
            _timerManager.StartTimer(300f);
            _timerManager.StopTimer();
            Assert.IsFalse(_timerManager.IsRunning);
            Assert.AreEqual(0f, _timerManager.RemainingTime, 0.01f);
        }

        [Test]
        public void TimerManager_GetFormattedTime_Format()
        {
            _timerManager.StartTimer(90f); // 1:30
            string formatted = _timerManager.GetFormattedTime();
            Assert.AreEqual("01:30", formatted);
        }

        [Test]
        public void TimerManager_GetFormattedTime_ZeroSeconds()
        {
            _timerManager.StartTimer(0f);
            string formatted = _timerManager.GetFormattedTime();
            Assert.AreEqual("00:00", formatted);
        }

        [Test]
        public void TimerManager_GetProgress_StartsAtZero()
        {
            _timerManager.StartTimer(600f);
            Assert.AreEqual(0f, _timerManager.GetProgress(), 0.01f);
        }
    }

    // ==========================================================================
    //  CHAMPIONSHIP MANAGER TESTS
    // ==========================================================================

    /// <summary>
    /// Testes para a lógica do campeonato.
    /// </summary>
    [TestFixture]
    public class ChampionshipTests
    {
        [Test]
        public void TeamData_RegisterMatchResult_Win_IncrementsWins()
        {
            TeamDataSO team = ScriptableObject.CreateInstance<TeamDataSO>();
            team.ResetSeasonStats();
            team.RegisterMatchResult(2, 0);

            Assert.AreEqual(1, team.Wins);
            Assert.AreEqual(0, team.Draws);
            Assert.AreEqual(0, team.Losses);
            Assert.AreEqual(3, team.Points);

            Object.DestroyImmediate(team);
        }

        [Test]
        public void TeamData_RegisterMatchResult_Draw_IncrementsDraw()
        {
            TeamDataSO team = ScriptableObject.CreateInstance<TeamDataSO>();
            team.ResetSeasonStats();
            team.RegisterMatchResult(1, 1);

            Assert.AreEqual(0, team.Wins);
            Assert.AreEqual(1, team.Draws);
            Assert.AreEqual(1, team.Points);

            Object.DestroyImmediate(team);
        }

        [Test]
        public void TeamData_RegisterMatchResult_Loss_IncrementsLoss()
        {
            TeamDataSO team = ScriptableObject.CreateInstance<TeamDataSO>();
            team.ResetSeasonStats();
            team.RegisterMatchResult(0, 3);

            Assert.AreEqual(0, team.Wins);
            Assert.AreEqual(1, team.Losses);
            Assert.AreEqual(0, team.Points);

            Object.DestroyImmediate(team);
        }

        [Test]
        public void TeamData_GoalDifference_CalculatesCorrectly()
        {
            TeamDataSO team = ScriptableObject.CreateInstance<TeamDataSO>();
            team.ResetSeasonStats();
            team.RegisterMatchResult(3, 1); // +2
            team.RegisterMatchResult(0, 2); // -2

            Assert.AreEqual(0, team.GoalDifference);

            Object.DestroyImmediate(team);
        }

        [Test]
        public void TeamData_ResetSeasonStats_ClearsAll()
        {
            TeamDataSO team = ScriptableObject.CreateInstance<TeamDataSO>();
            team.RegisterMatchResult(5, 0);
            team.ResetSeasonStats();

            Assert.AreEqual(0, team.GamesPlayed);
            Assert.AreEqual(0, team.GoalsScored);
            Assert.AreEqual(0, team.Points);

            Object.DestroyImmediate(team);
        }
    }

    // ==========================================================================
    //  SAVE SYSTEM TESTS
    // ==========================================================================

    /// <summary>
    /// Testes para a serialização e estruturas de dados do SaveSystem.
    /// </summary>
    [TestFixture]
    public class SaveSystemTests
    {
        [Test]
        public void SettingsData_DefaultValues_AreValid()
        {
            SettingsData settings = new SettingsData();

            Assert.AreEqual(1f, settings.MasterVolume, 0.01f);
            Assert.AreEqual(0.7f, settings.MusicVolume, 0.01f);
            Assert.AreEqual(60, settings.TargetFPS);
            Assert.AreEqual("pt-BR", settings.Language);
        }

        [Test]
        public void CareerStatsData_DefaultValues_AreZero()
        {
            CareerStatsData stats = new CareerStatsData();

            Assert.AreEqual(0, stats.TotalMatches);
            Assert.AreEqual(0, stats.TotalGoalsScored);
            Assert.AreEqual(0, stats.TotalWins);
        }

        [Test]
        public void AchievementsData_InitiallyEmpty()
        {
            AchievementsData achievements = new AchievementsData();

            Assert.IsNotNull(achievements.Unlocked);
            Assert.AreEqual(0, achievements.Unlocked.Count);
        }

        [Test]
        public void SettingsData_Serialization_PreservesValues()
        {
            SettingsData original = new SettingsData
            {
                MasterVolume = 0.5f,
                MusicVolume = 0.3f,
                QualityLevel = 1,
                Language = "en-US"
            };

            string json = JsonUtility.ToJson(original);
            SettingsData restored = JsonUtility.FromJson<SettingsData>(json);

            Assert.AreEqual(original.MasterVolume, restored.MasterVolume, 0.001f);
            Assert.AreEqual(original.MusicVolume, restored.MusicVolume, 0.001f);
            Assert.AreEqual(original.QualityLevel, restored.QualityLevel);
            Assert.AreEqual(original.Language, restored.Language);
        }
    }

    // ==========================================================================
    //  PLAYER DATA TESTS
    // ==========================================================================

    /// <summary>
    /// Testes para lógica de dados dos jogadores.
    /// </summary>
    [TestFixture]
    public class PlayerDataTests
    {
        [Test]
        public void PlayerData_DefaultStats_AreValid()
        {
            PlayerStats stats = PlayerStats.Default;

            Assert.AreEqual(60, stats.Speed);
            Assert.AreEqual(60, stats.Shoot);
            Assert.AreEqual(60, stats.Pass);
        }

        [Test]
        public void PlayerData_GoalkeeperStats_HasHighReflexes()
        {
            PlayerStats gkStats = PlayerStats.DefaultGoalkeeper;

            Assert.Greater(gkStats.Reflexes, gkStats.Shoot,
                "Goleiro deve ter Reflexes maior que Shoot.");
            Assert.Greater(gkStats.Defense, gkStats.Dribble,
                "Goleiro deve ser melhor em defesa que drible.");
        }

        [Test]
        public void PlayerData_OverallRating_IsWithinRange()
        {
            PlayerDataSO player = ScriptableObject.CreateInstance<PlayerDataSO>();
            player.Position = PlayerPosition.Attacker;
            player.Stats = new PlayerStats
            {
                Speed = 80, Shoot = 85, Pass = 70,
                Dribble = 75, Defense = 40, Physical = 65, Reflexes = 30
            };

            int overall = player.GetOverallRating();

            Assert.GreaterOrEqual(overall, 1, "Overall deve ser >= 1");
            Assert.LessOrEqual(overall, 99, "Overall deve ser <= 99");

            Object.DestroyImmediate(player);
        }

        [Test]
        public void PlayerData_AttackerOverall_WeightsShootHighly()
        {
            PlayerDataSO highShooter = ScriptableObject.CreateInstance<PlayerDataSO>();
            highShooter.Position = PlayerPosition.Attacker;
            highShooter.Stats = new PlayerStats
            {
                Speed = 60, Shoot = 99, Pass = 60,
                Dribble = 60, Defense = 30, Physical = 60, Reflexes = 30
            };

            PlayerDataSO lowShooter = ScriptableObject.CreateInstance<PlayerDataSO>();
            lowShooter.Position = PlayerPosition.Attacker;
            lowShooter.Stats = new PlayerStats
            {
                Speed = 60, Shoot = 30, Pass = 60,
                Dribble = 60, Defense = 60, Physical = 60, Reflexes = 60
            };

            Assert.Greater(highShooter.GetOverallRating(), lowShooter.GetOverallRating(),
                "Atacante com Shoot 99 deve ter overall maior que um com Shoot 30.");

            Object.DestroyImmediate(highShooter);
            Object.DestroyImmediate(lowShooter);
        }
    }
}
