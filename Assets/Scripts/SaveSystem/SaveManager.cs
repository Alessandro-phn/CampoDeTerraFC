using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using CampoDeTerraFC.Config;

namespace CampoDeTerraFC.SaveSystem
{
    /// <summary>
    /// Gerencia o salvamento e carregamento de dados do jogo.
    /// Usa JSON serializado em arquivo para suportar dados complexos de campeonato.
    /// PlayerPrefs é usado apenas para configurações simples (volume, qualidade).
    /// </summary>
    public sealed class SaveManager : MonoBehaviour
    {
        // ===== CAMINHOS =====

        private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");
        private const string SETTINGS_FILE = "settings.json";
        private const string CHAMPIONSHIP_FILE = "championship.json";
        private const string STATS_FILE = "stats.json";
        private const string ACHIEVEMENTS_FILE = "achievements.json";

        // ===== DADOS =====

        private SettingsData _settings = new SettingsData();
        private ChampionshipSaveData _championship = new ChampionshipSaveData();
        private CareerStatsData _stats = new CareerStatsData();
        private AchievementsData _achievements = new AchievementsData();

        // ===== PROPRIEDADES =====

        public SettingsData Settings => _settings;
        public ChampionshipSaveData Championship => _championship;
        public CareerStatsData Stats => _stats;
        public AchievementsData Achievements => _achievements;

        // ===== UNITY LIFECYCLE =====

        private void Awake()
        {
            EnsureSaveDirectory();
        }

        // ===== INICIALIZAÇÃO =====

        /// <summary>
        /// Carrega todos os dados salvos ao iniciar o jogo.
        /// </summary>
        public void Initialize()
        {
            LoadSettings();
            LoadStats();
            LoadAchievements();

            Debug.Log($"[SaveManager] Dados carregados. Diretório: {SaveDirectory}");
        }

        // ===== SAVE =====

        /// <summary>
        /// Salva todos os dados do jogo.
        /// </summary>
        public void SaveAll()
        {
            SaveSettings();
            SaveStats();
            SaveAchievements();

            if (_championship.IsActive)
                SaveChampionship();

            Debug.Log("[SaveManager] Todos os dados salvos.");
        }

        public void SaveSettings()
        {
            WriteJson(SETTINGS_FILE, _settings);
        }

        public void SaveChampionship()
        {
            WriteJson(CHAMPIONSHIP_FILE, _championship);
        }

        public void SaveStats()
        {
            WriteJson(STATS_FILE, _stats);
        }

        public void SaveAchievements()
        {
            WriteJson(ACHIEVEMENTS_FILE, _achievements);
        }

        // ===== LOAD =====

        public void LoadSettings()
        {
            _settings = ReadJson<SettingsData>(SETTINGS_FILE) ?? new SettingsData();
            ApplySettings();
        }

        public void LoadChampionship()
        {
            _championship = ReadJson<ChampionshipSaveData>(CHAMPIONSHIP_FILE) ?? new ChampionshipSaveData();
        }

        public void LoadStats()
        {
            _stats = ReadJson<CareerStatsData>(STATS_FILE) ?? new CareerStatsData();
        }

        public void LoadAchievements()
        {
            _achievements = ReadJson<AchievementsData>(ACHIEVEMENTS_FILE) ?? new AchievementsData();
        }

        // ===== CONQUISTAS =====

        /// <summary>
        /// Desbloqueia uma conquista e salva imediatamente.
        /// </summary>
        public bool UnlockAchievement(string achievementId)
        {
            if (_achievements.Unlocked.Contains(achievementId))
                return false; // Já desbloqueada

            _achievements.Unlocked.Add(achievementId);
            _achievements.UnlockDates[achievementId] = DateTime.Now.ToString("yyyy-MM-dd");

            SaveAchievements();

            Debug.Log($"[SaveManager] Conquista desbloqueada: {achievementId}");
            return true;
        }

        public bool IsAchievementUnlocked(string id)
        {
            return _achievements.Unlocked.Contains(id);
        }

        // ===== ESTATÍSTICAS =====

        /// <summary>
        /// Registra os resultados de uma partida nas estatísticas de carreira.
        /// </summary>
        public void RecordMatch(int goalsScored, int goalsConceded, bool isWin, bool isDraw)
        {
            _stats.TotalMatches++;
            _stats.TotalGoalsScored += goalsScored;
            _stats.TotalGoalsConceded += goalsConceded;

            if (isWin) _stats.TotalWins++;
            else if (isDraw) _stats.TotalDraws++;
            else _stats.TotalLosses++;

            if (goalsScored > _stats.BestGoalsInMatch)
                _stats.BestGoalsInMatch = goalsScored;

            SaveStats();
        }

        // ===== CONFIGURAÇÕES =====

        /// <summary>
        /// Aplica as configurações carregadas ao jogo.
        /// </summary>
        private void ApplySettings()
        {
            // Qualidade gráfica
            QualitySettings.SetQualityLevel(_settings.QualityLevel, applyExpensiveChanges: true);

            // Volume
            AudioListener.volume = _settings.MasterVolume;

            // FPS alvo
            Application.targetFrameRate = _settings.TargetFPS;

            // Tela cheia
            Screen.fullScreen = _settings.Fullscreen;

            Debug.Log($"[SaveManager] Configurações aplicadas. Qualidade: {_settings.QualityLevel}, Volume: {_settings.MasterVolume}");
        }

        // ===== I/O =====

        private void WriteJson<T>(string filename, T data)
        {
            try
            {
                string path = Path.Combine(SaveDirectory, filename);
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Erro ao salvar {filename}: {e.Message}");
            }
        }

        private T ReadJson<T>(string filename) where T : class
        {
            try
            {
                string path = Path.Combine(SaveDirectory, filename);
                if (!File.Exists(path)) return null;

                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Erro ao carregar {filename}: {e.Message}");
                return null;
            }
        }

        private void EnsureSaveDirectory()
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
                Debug.Log($"[SaveManager] Diretório de saves criado: {SaveDirectory}");
            }
        }

        // ===== DELETE =====

        /// <summary>
        /// Apaga todos os dados de save (para o menu de novo jogo).
        /// </summary>
        public void DeleteAllSaves()
        {
            string[] files = { SETTINGS_FILE, CHAMPIONSHIP_FILE, STATS_FILE, ACHIEVEMENTS_FILE };
            foreach (string file in files)
            {
                string path = Path.Combine(SaveDirectory, file);
                if (File.Exists(path))
                    File.Delete(path);
            }

            _championship = new ChampionshipSaveData();
            _stats = new CareerStatsData();
            _achievements = new AchievementsData();

            Debug.Log("[SaveManager] Todos os saves apagados.");
        }
    }

    // ==========================================================================
    //  DATA STRUCTURES
    // ==========================================================================

    /// <summary>Configurações do jogo.</summary>
    [Serializable]
    public class SettingsData
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 0.7f;
        public float SFXVolume = 1f;
        public int QualityLevel = 2;        // 0=Low, 1=Medium, 2=High
        public bool Fullscreen = true;
        public int TargetFPS = 60;
        public string Language = "pt-BR";
        public bool ShowMinimap = true;
        public bool ScreenShake = true;
        public float ControllerVibration = 0.8f;
    }

    /// <summary>Estado de um campeonato salvo.</summary>
    [Serializable]
    public class ChampionshipSaveData
    {
        public bool IsActive = false;
        public string ChampionshipName = "";
        public int CurrentRound = 0;
        public string PlayerTeamName = "";
        public List<MatchRecord> CompletedMatches = new List<MatchRecord>();
        public string LastSaveDate = "";
    }

    /// <summary>Registro de uma partida do campeonato.</summary>
    [Serializable]
    public struct MatchRecord
    {
        public string HomeTeam;
        public string AwayTeam;
        public int HomeScore;
        public int AwayScore;
        public int Round;
    }

    /// <summary>Estatísticas de carreira do jogador.</summary>
    [Serializable]
    public class CareerStatsData
    {
        public int TotalMatches = 0;
        public int TotalWins = 0;
        public int TotalDraws = 0;
        public int TotalLosses = 0;
        public int TotalGoalsScored = 0;
        public int TotalGoalsConceded = 0;
        public int BestGoalsInMatch = 0;
        public int TotalChampionshipsWon = 0;
        public float TotalPlaytimeHours = 0f;
    }

    /// <summary>Conquistas desbloqueadas.</summary>
    [Serializable]
    public class AchievementsData
    {
        public List<string> Unlocked = new List<string>();
        public Dictionary<string, string> UnlockDates = new Dictionary<string, string>();
    }
}
