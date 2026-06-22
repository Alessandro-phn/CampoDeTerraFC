using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampoDeTerraFC.Data
{
    /// <summary>
    /// Define a formação tática de um time como ScriptableObject.
    /// Cada posição é definida em coordenadas normalizadas do campo (0-1).
    /// O eixo X representa a largura (0 = esquerda, 1 = direita).
    /// O eixo Y representa o comprimento (0 = próprio gol, 1 = gol adversário).
    /// </summary>
    [CreateAssetMenu(menuName = "Campo de Terra FC/Data/Formation", fileName = "Formation_")]
    public class FormationSO : ScriptableObject
    {
        // ===== IDENTIFICAÇÃO =====

        [Header("Identificação")]
        [Tooltip("Nome da formação (ex: 4-3-3, 4-4-2).")]
        public string FormationName = "4-3-3";

        [Tooltip("Descrição da formação e estilo de jogo.")]
        [TextArea]
        public string Description;

        // ===== POSIÇÕES =====

        [Header("Posições dos Jogadores (normalizadas 0-1)")]
        [Tooltip("Posições de cada jogador de campo. O índice 0 é o goleiro.")]
        public List<FormationPosition> Positions = new List<FormationPosition>();

        // ===== FORMAÇÕES PRÉ-DEFINIDAS =====

        /// <summary>
        /// Cria a formação 4-3-3 padrão.
        /// Para campo de 5 jogadores, retorna formação 1-2-1-1.
        /// </summary>
        public static List<Vector2> Get433()
        {
            return new List<Vector2>
            {
                new Vector2(0.5f, 0.05f),  // 0 - Goleiro

                new Vector2(0.2f, 0.25f),  // 1 - ZE esquerdo
                new Vector2(0.4f, 0.28f),  // 2 - ZE central esq
                new Vector2(0.6f, 0.28f),  // 3 - ZE central dir
                new Vector2(0.8f, 0.25f),  // 4 - ZE direito

                new Vector2(0.25f, 0.50f), // 5 - ME esquerdo
                new Vector2(0.5f, 0.48f),  // 6 - ME central
                new Vector2(0.75f, 0.50f), // 7 - ME direito

                new Vector2(0.2f, 0.75f),  // 8 - ATA esquerdo
                new Vector2(0.5f, 0.78f),  // 9 - ATA central
                new Vector2(0.8f, 0.75f),  // 10 - ATA direito
            };
        }

        /// <summary>Formação 4-4-2 clássica.</summary>
        public static List<Vector2> Get442()
        {
            return new List<Vector2>
            {
                new Vector2(0.5f, 0.05f),

                new Vector2(0.15f, 0.25f),
                new Vector2(0.38f, 0.27f),
                new Vector2(0.62f, 0.27f),
                new Vector2(0.85f, 0.25f),

                new Vector2(0.15f, 0.52f),
                new Vector2(0.38f, 0.50f),
                new Vector2(0.62f, 0.50f),
                new Vector2(0.85f, 0.52f),

                new Vector2(0.38f, 0.75f),
                new Vector2(0.62f, 0.75f),
            };
        }

        /// <summary>
        /// Formação para pelada de 5 jogadores (1-2-1-1).
        /// </summary>
        public static List<Vector2> GetFiveAside()
        {
            return new List<Vector2>
            {
                new Vector2(0.5f, 0.05f),  // Goleiro

                new Vector2(0.3f, 0.30f),  // Defensor esq
                new Vector2(0.7f, 0.30f),  // Defensor dir

                new Vector2(0.5f, 0.55f),  // Meia

                new Vector2(0.5f, 0.78f),  // Atacante
            };
        }

        // ===== UTILITÁRIOS =====

        /// <summary>
        /// Retorna a posição normalizada de um jogador pelo índice.
        /// </summary>
        /// <param name="index">Índice do jogador (0 = goleiro).</param>
        /// <returns>Posição normalizada (0-1, 0-1).</returns>
        public Vector2 GetPositionByIndex(int index)
        {
            if (index < 0 || index >= Positions.Count)
            {
                Debug.LogWarning($"[FormationSO] Índice {index} fora do range. Retornando centro.");
                return new Vector2(0.5f, 0.5f);
            }

            return Positions[index].NormalizedPosition;
        }

        /// <summary>
        /// Converte uma posição normalizada para coordenada mundial do campo.
        /// </summary>
        /// <param name="normalizedPos">Posição normalizada (0-1, 0-1).</param>
        /// <param name="fieldBounds">Bounds do campo no mundo.</param>
        /// <param name="isTeamB">Se true, espelha as posições (time B começa do outro lado).</param>
        public Vector3 ToWorldPosition(Vector2 normalizedPos, Bounds fieldBounds, bool isTeamB)
        {
            float x = Mathf.Lerp(fieldBounds.min.x, fieldBounds.max.x, normalizedPos.x);
            float z = Mathf.Lerp(fieldBounds.min.z, fieldBounds.max.z,
                isTeamB ? 1f - normalizedPos.y : normalizedPos.y);

            return new Vector3(x, 0f, z);
        }
    }

    /// <summary>
    /// Posição de um jogador na formação com metadados.
    /// </summary>
    [Serializable]
    public class FormationPosition
    {
        [Tooltip("Posição normalizada no campo (X: 0-1, Y: 0-1).")]
        public Vector2 NormalizedPosition;

        [Tooltip("Posição de campo deste slot.")]
        public PlayerPosition Role;

        [Tooltip("Label para exibição (ex: LB, CB, CM).")]
        public string Label;
    }
}
