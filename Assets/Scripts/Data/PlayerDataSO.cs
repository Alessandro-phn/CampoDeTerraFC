using System;
using UnityEngine;

namespace CampoDeTerraFC.Data
{
    /// <summary>
    /// Dados de um jogador como ScriptableObject.
    /// Contém atributos, aparência e configuração de posição.
    /// Criado via Assets > Create > Campo de Terra FC > Data > Player Data.
    /// </summary>
    [CreateAssetMenu(menuName = "Campo de Terra FC/Data/Player Data", fileName = "Player_")]
    public class PlayerDataSO : ScriptableObject
    {
        // ===== IDENTIFICAÇÃO =====

        [Header("Identificação")]
        [Tooltip("Nome popular do jogador (apelido de pelada).")]
        public string PlayerName = "Pedrão";

        [Tooltip("Número da camisa.")]
        [Range(1, 99)]
        public int ShirtNumber = 10;

        [Tooltip("Posição preferencial do jogador.")]
        public PlayerPosition Position = PlayerPosition.Attacker;

        [Tooltip("Foto ou ícone do jogador para UI.")]
        public Sprite Portrait;

        // ===== ATRIBUTOS =====

        [Header("Atributos (1-99)")]
        public PlayerStats Stats;

        // ===== APARÊNCIA =====

        [Header("Aparência")]
        [Tooltip("Prefab do modelo 3D do jogador.")]
        public GameObject PlayerPrefab;

        [Tooltip("Tom de pele (para customização de shader).")]
        public SkinTone SkinTone = SkinTone.Medium;

        [Tooltip("Tipo de cabelo.")]
        public HairType HairType = HairType.Short;

        [Tooltip("Cor primária da camisa.")]
        public Color PrimaryShirtColor = Color.red;

        [Tooltip("Cor secundária (shorts, detalhes).")]
        public Color SecondaryShirtColor = Color.white;

        // ===== HABILIDADE ESPECIAL =====

        [Header("Habilidade Especial")]
        [Tooltip("Habilidade única deste jogador.")]
        public SpecialAbility SpecialAbility = SpecialAbility.None;

        [Tooltip("Descrição da habilidade especial.")]
        [TextArea]
        public string SpecialAbilityDescription;

        // ===== MÉTODOS =====

        /// <summary>
        /// Retorna o atributo geral do jogador (média ponderada das stats).
        /// </summary>
        public int GetOverallRating()
        {
            return Position switch
            {
                PlayerPosition.Goalkeeper =>
                    (int)((Stats.Reflexes * 0.35f) + (Stats.Physical * 0.20f) +
                          (Stats.Defense * 0.25f) + (Stats.Pass * 0.20f)),

                PlayerPosition.Defender =>
                    (int)((Stats.Defense * 0.40f) + (Stats.Physical * 0.25f) +
                          (Stats.Speed * 0.15f) + (Stats.Pass * 0.20f)),

                PlayerPosition.Midfielder =>
                    (int)((Stats.Pass * 0.30f) + (Stats.Dribble * 0.25f) +
                          (Stats.Defense * 0.20f) + (Stats.Speed * 0.25f)),

                PlayerPosition.Winger =>
                    (int)((Stats.Speed * 0.35f) + (Stats.Dribble * 0.30f) +
                          (Stats.Pass * 0.20f) + (Stats.Shoot * 0.15f)),

                PlayerPosition.Attacker =>
                    (int)((Stats.Shoot * 0.40f) + (Stats.Speed * 0.25f) +
                          (Stats.Dribble * 0.25f) + (Stats.Physical * 0.10f)),

                _ => (int)((Stats.Speed + Stats.Shoot + Stats.Pass + Stats.Dribble +
                            Stats.Defense + Stats.Physical) / 6f)
            };
        }
    }

    // ===== STATS =====

    /// <summary>
    /// Estrutura de atributos do jogador.
    /// Todos os atributos variam de 1 a 99.
    /// </summary>
    [Serializable]
    public struct PlayerStats
    {
        [Range(1, 99)] public int Speed;
        [Range(1, 99)] public int Shoot;
        [Range(1, 99)] public int Pass;
        [Range(1, 99)] public int Dribble;
        [Range(1, 99)] public int Defense;
        [Range(1, 99)] public int Physical;

        /// <summary>Atributo exclusivo de goleiros (1-99).</summary>
        [Range(1, 99)] public int Reflexes;

        /// <summary>
        /// Cria stats de jogador base equilibrado (média 60).
        /// </summary>
        public static PlayerStats Default => new PlayerStats
        {
            Speed = 60,
            Shoot = 60,
            Pass = 60,
            Dribble = 60,
            Defense = 60,
            Physical = 60,
            Reflexes = 60
        };

        /// <summary>
        /// Cria stats para um goleiro básico.
        /// </summary>
        public static PlayerStats DefaultGoalkeeper => new PlayerStats
        {
            Speed = 50,
            Shoot = 30,
            Pass = 55,
            Dribble = 25,
            Defense = 70,
            Physical = 65,
            Reflexes = 75
        };
    }

    // ===== ENUMS =====

    /// <summary>Posições do campo de futebol.</summary>
    public enum PlayerPosition
    {
        Goalkeeper = 0,
        Defender = 1,
        WingBack = 2,
        DefensiveMidfielder = 3,
        Midfielder = 4,
        Winger = 5,
        AttackingMidfielder = 6,
        Attacker = 7
    }

    /// <summary>Tons de pele disponíveis para customização.</summary>
    public enum SkinTone
    {
        VeryLight,
        Light,
        Medium,
        MediumDark,
        Dark,
        VeryDark
    }

    /// <summary>Tipos de cabelo disponíveis para customização.</summary>
    public enum HairType
    {
        Bald,
        Short,
        Curly,
        Braids,
        Afro,
        Long,
        Mohawk
    }

    /// <summary>Habilidades especiais dos jogadores.</summary>
    public enum SpecialAbility
    {
        None,
        SprintBurst,      // Aceleração explosiva
        PowerShot,        // Chute com força extra
        CurveMaster,      // Curva exagerada na bola
        DribleKing,       // Dribles mais efetivos
        IronWall,         // Defesa inabalável
        EagleEye,         // Passe com maior precisão
        Chameleon,        // Adapta-se a qualquer posição
        Thunderfoot,      // Chute de longe preciso
        SpiderGoal        // Reflexo de goleiro sobre-humano
    }
}
