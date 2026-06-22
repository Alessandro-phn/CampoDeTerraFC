using System;
using UnityEngine;

namespace CampoDeTerraFC.Core.Events
{
    // ===== EVENTOS BASE =====

    /// <summary>
    /// Evento de jogo baseado em ScriptableObject.
    /// Implementa o padrão Observer sem acoplamento entre emissores e ouvintes.
    /// Permite drag-and-drop de eventos no Inspector sem referências diretas.
    /// </summary>
    [CreateAssetMenu(menuName = "Campo de Terra FC/Events/Game Event", fileName = "GE_")]
    public class GameEventSO : ScriptableObject
    {
        /// <summary>Lista de ouvintes registrados para este evento.</summary>
        private event Action _onEvent;

        /// <summary>
        /// Dispara o evento, notificando todos os ouvintes registrados.
        /// </summary>
        public void Raise()
        {
            Debug.Log($"[GameEvent] Evento disparado: {name}");
            _onEvent?.Invoke();
        }

        /// <summary>Registra um ouvinte para este evento.</summary>
        public void AddListener(Action listener) => _onEvent += listener;

        /// <summary>Remove um ouvinte deste evento.</summary>
        public void RemoveListener(Action listener) => _onEvent -= listener;
    }

    // ===== EVENTO COM DADO: INT =====

    /// <summary>Evento de jogo que transporta um valor inteiro (ex: placar).</summary>
    [CreateAssetMenu(menuName = "Campo de Terra FC/Events/Game Event Int", fileName = "GE_Int_")]
    public class GameEventIntSO : ScriptableObject
    {
        private event Action<int> _onEvent;

        /// <summary>Dispara o evento com um valor inteiro.</summary>
        public void Raise(int value)
        {
            Debug.Log($"[GameEvent] Evento int disparado: {name} = {value}");
            _onEvent?.Invoke(value);
        }

        public void AddListener(Action<int> listener) => _onEvent += listener;
        public void RemoveListener(Action<int> listener) => _onEvent -= listener;
    }

    // ===== EVENTO COM DADO: FLOAT =====

    /// <summary>Evento de jogo que transporta um valor float (ex: tempo restante).</summary>
    [CreateAssetMenu(menuName = "Campo de Terra FC/Events/Game Event Float", fileName = "GE_Float_")]
    public class GameEventFloatSO : ScriptableObject
    {
        private event Action<float> _onEvent;

        public void Raise(float value) => _onEvent?.Invoke(value);
        public void AddListener(Action<float> listener) => _onEvent += listener;
        public void RemoveListener(Action<float> listener) => _onEvent -= listener;
    }

    // ===== EVENTO COM DADO: STRING =====

    /// <summary>Evento de jogo que transporta uma string (ex: nome do time).</summary>
    [CreateAssetMenu(menuName = "Campo de Terra FC/Events/Game Event String", fileName = "GE_String_")]
    public class GameEventStringSO : ScriptableObject
    {
        private event Action<string> _onEvent;

        public void Raise(string value) => _onEvent?.Invoke(value);
        public void AddListener(Action<string> listener) => _onEvent += listener;
        public void RemoveListener(Action<string> listener) => _onEvent -= listener;
    }

    // ===== EVENTO DE GOL =====

    /// <summary>
    /// Dados transportados pelo evento de gol.
    /// </summary>
    [Serializable]
    public struct GoalEventData
    {
        /// <summary>Índice do time que marcou (0 = Time A, 1 = Time B).</summary>
        public int ScoringTeamIndex;

        /// <summary>Nome do jogador que marcou.</summary>
        public string ScorerName;

        /// <summary>Tempo da partida em que o gol foi marcado (segundos).</summary>
        public float MatchTime;

        /// <summary>Tipo do gol (normal, cabeceio, bicicleta, pênalti).</summary>
        public GoalType GoalType;
    }

    /// <summary>Tipo do gol marcado.</summary>
    public enum GoalType
    {
        Normal,
        Header,
        Bicycle,
        Penalty,
        FreeKick,
        OwnGoal
    }

    /// <summary>Evento disparado ao marcar um gol.</summary>
    [CreateAssetMenu(menuName = "Campo de Terra FC/Events/Goal Event", fileName = "GE_Goal")]
    public class GoalEventSO : ScriptableObject
    {
        private event Action<GoalEventData> _onEvent;

        /// <summary>Dispara o evento de gol com dados completos.</summary>
        public void Raise(GoalEventData data)
        {
            Debug.Log($"[GoalEvent] GOL de {data.ScorerName} pelo time {data.ScoringTeamIndex} aos {data.MatchTime:F0}s!");
            _onEvent?.Invoke(data);
        }

        public void AddListener(Action<GoalEventData> listener) => _onEvent += listener;
        public void RemoveListener(Action<GoalEventData> listener) => _onEvent -= listener;
    }

    // ===== EVENTO DE FALTA =====

    /// <summary>Dados do evento de falta.</summary>
    [Serializable]
    public struct FoulEventData
    {
        /// <summary>Time que cometeu a falta.</summary>
        public int CommittingTeamIndex;

        /// <summary>Posição no campo onde a falta ocorreu.</summary>
        public Vector3 FoulPosition;

        /// <summary>Se a falta é dentro da área (pênalti).</summary>
        public bool IsPenalty;
    }

    /// <summary>Evento disparado ao ocorrer uma falta.</summary>
    [CreateAssetMenu(menuName = "Campo de Terra FC/Events/Foul Event", fileName = "GE_Foul")]
    public class FoulEventSO : ScriptableObject
    {
        private event Action<FoulEventData> _onEvent;

        public void Raise(FoulEventData data) => _onEvent?.Invoke(data);
        public void AddListener(Action<FoulEventData> listener) => _onEvent += listener;
        public void RemoveListener(Action<FoulEventData> listener) => _onEvent -= listener;
    }

    // ===== LISTENER COMPONENT =====

    /// <summary>
    /// Componente que escuta um GameEventSO e invoca uma UnityEvent.
    /// Permite ligar eventos ScriptableObject a comportamentos no Inspector.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEventSO _event;
        [SerializeField] private UnityEngine.Events.UnityEvent _response;

        private void OnEnable() => _event?.AddListener(OnEventRaised);
        private void OnDisable() => _event?.RemoveListener(OnEventRaised);

        private void OnEventRaised() => _response?.Invoke();
    }
}
