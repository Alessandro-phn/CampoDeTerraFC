using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using CampoDeTerraFC.Core.Services;
using CampoDeTerraFC.Audio;
using CampoDeTerraFC.SaveSystem;

namespace CampoDeTerraFC.Core
{
    /// <summary>
    /// Gerenciador central do jogo. Singleton persistente entre cenas.
    /// FUSÃO: Arquitetura do Sprint1 (ServiceLocator, estados, modo de jogo)
    ///        + simplicidade do Unity2022 (sem deps quebradas no Awake).
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        // ===== SINGLETON =====
        private static GameManager _instance;
        public static GameManager Instance
        {
            get { if (_instance == null) Debug.LogError("[GameManager] Instância não encontrada."); return _instance; }
        }

        // ===== ESTADO =====
        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public GameMode CurrentGameMode { get; private set; } = GameMode.Pelada;

        // ===== REFERÊNCIAS (atribuídas pelo ProjectSetup ou Inspector) =====
        [Header("Serviços")]
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private SaveManager _saveManager;

        // ===== CONFIG =====
        [Header("Configuração")]
        [SerializeField] private Config.GameConfigSO _gameConfig;

        // ===== EVENTOS =====
        public event Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Registra serviços disponíveis (não falha se null)
            if (_audioManager != null) ServiceLocator.Register<AudioManager>(_audioManager);
            if (_saveManager  != null) ServiceLocator.Register<SaveManager>(_saveManager);
            ServiceLocator.Register<GameManager>(this);

            _saveManager?.Initialize();
            _audioManager?.Initialize();

            Debug.Log("[GameManager] Inicializado. v" + Application.version);
        }

        private void OnDestroy()
        {
            ServiceLocator.UnregisterAll();
        }

        // ===== CONTROLE DE ESTADO =====
        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;
            GameState prev = CurrentState;
            CurrentState = newState;
            Debug.Log("[GameManager] " + prev + " → " + newState);
            OnGameStateChanged?.Invoke(newState);
            HandleTransition(newState);
        }

        private void HandleTransition(GameState state)
        {
            switch (state)
            {
                case GameState.Paused:
                    Time.timeScale = 0f;
                    _audioManager?.PauseMusic();
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    _audioManager?.ResumeMusic();
                    break;
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    _audioManager?.PlayMusic(MusicType.MainMenu);
                    break;
                case GameState.MatchEnd:
                    _audioManager?.PlaySFX(SFXType.FinalWhistle);
                    break;
            }
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Playing) ChangeState(GameState.Paused);
            else if (CurrentState == GameState.Paused) ChangeState(GameState.Playing);
        }

        public void SetGameMode(GameMode mode) { CurrentGameMode = mode; }
        public Config.GameConfigSO GetConfig() => _gameConfig;

        public void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        public void QuitGame()
        {
            _saveManager?.SaveAll();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    // ===== ENUMS =====
    public enum GameState  { None, MainMenu, TeamSelection, Playing, Paused, GoalReplay, MatchEnd, Championship, PenaltyShootout, Loading }
    public enum GameMode   { Pelada, Championship, PenaltyShootout, Training }
}
