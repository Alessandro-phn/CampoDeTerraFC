using UnityEngine;

namespace CampoDeTerraFC.Config
{
    /// <summary>
    /// Configuração global do jogo como ScriptableObject.
    /// Centraliza todos os parâmetros ajustáveis sem exigir recompilação.
    /// Criado via Assets > Create > Campo de Terra FC > Config > Game Config.
    /// </summary>
    [CreateAssetMenu(menuName = "Campo de Terra FC/Config/Game Config", fileName = "GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        // ===== PARTIDA =====

        [Header("Configurações de Partida")]

        [Tooltip("Duração padrão de uma partida em segundos (600 = 10 minutos).")]
        [Range(60f, 1800f)]
        public float MatchDurationSeconds = 600f;

        [Tooltip("Número de jogadores por time em campo.")]
        [Range(2, 11)]
        public int PlayersPerTeam = 5;

        [Tooltip("Número de substituições permitidas por time.")]
        [Range(0, 5)]
        public int MaxSubstitutions = 3;

        [Tooltip("Habilita prorrogação ao empate em mata-mata.")]
        public bool EnableExtraTime = true;

        [Tooltip("Duração da prorrogação em segundos.")]
        [Range(60f, 300f)]
        public float ExtraTimeDurationSeconds = 180f;

        // ===== FÍSICA =====

        [Header("Configurações de Física")]

        [Tooltip("Peso da bola em gramas.")]
        [Range(300f, 600f)]
        public float BallWeightGrams = 430f;

        [Tooltip("Multiplicador de força de chute forte.")]
        [Range(1f, 5f)]
        public float StrongKickForceMultiplier = 3.5f;

        [Tooltip("Multiplicador de força de passe curto.")]
        [Range(0.5f, 3f)]
        public float ShortPassForceMultiplier = 1.5f;

        [Tooltip("Multiplicador de força de passe longo.")]
        [Range(1f, 5f)]
        public float LongPassForceMultiplier = 2.8f;

        [Tooltip("Coeficiente de atrito do campo de terra (0 = sem atrito, 1 = parado).")]
        [Range(0.1f, 1f)]
        public float DirtFieldFriction = 0.6f;

        [Tooltip("Coeficiente de atrito do cimento.")]
        [Range(0.1f, 1f)]
        public float CementFieldFriction = 0.4f;

        [Tooltip("Elasticidade do bounce da bola (0 = sem rebote, 1 = elástico perfeito).")]
        [Range(0f, 1f)]
        public float BallBounciness = 0.65f;

        [Tooltip("Spin máximo aplicável à bola (rad/s).")]
        [Range(0f, 20f)]
        public float MaxBallSpin = 12f;

        // ===== JOGADOR =====

        [Header("Configurações de Jogador")]

        [Tooltip("Velocidade base de caminhada (unidades/segundo).")]
        [Range(1f, 5f)]
        public float BaseWalkSpeed = 3f;

        [Tooltip("Velocidade base de corrida.")]
        [Range(3f, 10f)]
        public float BaseRunSpeed = 6f;

        [Tooltip("Multiplicador de sprint (aplicado sobre RunSpeed).")]
        [Range(1f, 2f)]
        public float SprintMultiplier = 1.35f;

        [Tooltip("Tempo de aceleração até velocidade máxima (segundos).")]
        [Range(0.1f, 1f)]
        public float AccelerationTime = 0.25f;

        [Tooltip("Tempo de desaceleração ao soltar o movimento.")]
        [Range(0.05f, 0.5f)]
        public float DecelerationTime = 0.15f;

        [Tooltip("Raio de detecção de bola para controle automático.")]
        [Range(0.5f, 3f)]
        public float BallControlRadius = 1.5f;

        [Tooltip("Raio de mudança automática de jogador.")]
        [Range(2f, 10f)]
        public float AutoSwitchRadius = 5f;

        // ===== IA =====

        [Header("Configurações de IA")]

        [Tooltip("Dificuldade padrão da IA.")]
        public DifficultyLevel DefaultDifficulty = DifficultyLevel.Normal;

        [Tooltip("Acurácia de passe da IA em dificuldade Fácil (0-1).")]
        [Range(0f, 1f)]
        public float AIPassAccuracyEasy = 0.6f;

        [Tooltip("Acurácia de passe da IA em dificuldade Normal.")]
        [Range(0f, 1f)]
        public float AIPassAccuracyNormal = 0.78f;

        [Tooltip("Acurácia de passe da IA em dificuldade Difícil.")]
        [Range(0f, 1f)]
        public float AIPassAccuracyHard = 0.92f;

        [Tooltip("Tempo de reação da IA em milissegundos (maior = mais lento).")]
        [Range(0f, 1f)]
        public float AIReactionDelay = 0.2f;

        // ===== CÂMERA =====

        [Header("Configurações de Câmera")]

        [Tooltip("Distância padrão da câmera do ponto de foco.")]
        [Range(5f, 30f)]
        public float DefaultCameraDistance = 18f;

        [Tooltip("Ângulo padrão da câmera em graus.")]
        [Range(20f, 80f)]
        public float DefaultCameraAngle = 55f;

        [Tooltip("Suavidade do seguimento da câmera (maior = mais suave/lento).")]
        [Range(0.01f, 1f)]
        public float CameraSmoothTime = 0.2f;

        // ===== PERFORMANCE =====

        [Header("Configurações de Performance")]

        [Tooltip("FPS alvo no Android.")]
        [Range(30, 60)]
        public int TargetFPSAndroid = 60;

        [Tooltip("FPS alvo no PC.")]
        [Range(60, 144)]
        public int TargetFPSPC = 120;

        [Tooltip("Qualidade de sombras no Android.")]
        public ShadowQuality AndroidShadowQuality = ShadowQuality.HardOnly;

        // ===== GOLEIRO =====

        [Header("Configurações de Goleiro")]

        [Tooltip("Raio de atuação do goleiro dentro da área.")]
        [Range(2f, 10f)]
        public float GoalkeeperActionRadius = 6f;

        [Tooltip("Velocidade máxima de deslocamento do goleiro.")]
        [Range(2f, 8f)]
        public float GoalkeeperMaxSpeed = 5f;

        [Tooltip("Tempo de reação do goleiro em pênaltis (segundos).")]
        [Range(0.1f, 0.8f)]
        public float GoalkeeperPenaltyReactionTime = 0.3f;

        // ===== REPLAY =====

        [Header("Configurações de Replay")]

        [Tooltip("Duração do buffer de replay em segundos.")]
        [Range(5f, 30f)]
        public float ReplayBufferDuration = 10f;

        [Tooltip("Velocidade de reprodução do replay.")]
        [Range(0.1f, 1f)]
        public float ReplaySpeed = 0.5f;

        // ===== MÉTODOS UTILITÁRIOS =====

        /// <summary>
        /// Retorna a acurácia de passe da IA baseada na dificuldade configurada.
        /// </summary>
        public float GetAIPassAccuracy()
        {
            return DefaultDifficulty switch
            {
                DifficultyLevel.Easy => AIPassAccuracyEasy,
                DifficultyLevel.Normal => AIPassAccuracyNormal,
                DifficultyLevel.Hard => AIPassAccuracyHard,
                _ => AIPassAccuracyNormal
            };
        }

        /// <summary>
        /// Retorna a duração total da partida incluindo prorrogação se ativa.
        /// </summary>
        public float GetTotalMatchDuration(bool isExtraTime)
        {
            return isExtraTime
                ? MatchDurationSeconds + ExtraTimeDurationSeconds
                : MatchDurationSeconds;
        }
    }

    // ===== ENUMS DE CONFIGURAÇÃO =====

    /// <summary>Níveis de dificuldade da IA.</summary>
    public enum DifficultyLevel
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Legendary = 3
    }
}
