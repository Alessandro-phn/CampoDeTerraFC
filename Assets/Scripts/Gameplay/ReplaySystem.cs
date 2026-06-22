using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CampoDeTerraFC.Ball;
using CampoDeTerraFC.Player;
using CampoDeTerraFC.Camera;

namespace CampoDeTerraFC.Gameplay
{
    /// <summary>
    /// Sistema de replay de gol.
    /// Grava continuamente as últimas N segundos de posições de todos os objetos relevantes.
    /// Ao marcar um gol, reproduz o buffer em câmera lenta com ângulo cinemático.
    /// </summary>
    public sealed class ReplaySystem : MonoBehaviour
    {
        // ===== REFERÊNCIAS =====

        [Header("Referências")]
        [SerializeField] private BallController _ball;
        [SerializeField] private Camera.MatchCamera _cameraController;

        [Header("Configuração")]
        [SerializeField, Range(5f, 30f)] private float _bufferDuration = 10f;
        [SerializeField, Range(0.1f, 1f)] private float _replaySpeed = 0.4f;
        [SerializeField] private float _replayDuration = 6f;

        // ===== BUFFER DE GRAVAÇÃO =====

        /// <summary>Snapshot de posição/rotação de todos os objetos rastreados.</summary>
        private struct ObjectSnapshot
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public float Timestamp;
        }

        private Dictionary<Transform, Queue<ObjectSnapshot>> _snapshots
            = new Dictionary<Transform, Queue<ObjectSnapshot>>();

        private List<Transform> _trackedObjects = new List<Transform>();

        // ===== ESTADO =====

        private bool _isRecording;
        private bool _isReplaying;
        private float _recordInterval = 0.033f; // ~30 fps de gravação
        private float _recordTimer;

        // ===== EVENTOS =====

        public event Action OnReplayStarted;
        public event Action OnReplayFinished;

        // ===== UNITY LIFECYCLE =====

        private void Awake()
        {
            // Registra a bola imediatamente
            if (_ball != null)
                RegisterObject(_ball.transform);
        }

        private void Update()
        {
            if (!_isRecording || _isReplaying) return;

            _recordTimer -= Time.deltaTime;
            if (_recordTimer <= 0f)
            {
                _recordTimer = _recordInterval;
                RecordSnapshot();
            }
        }

        // ===== INICIALIZAÇÃO =====

        /// <summary>
        /// Inicia a gravação do buffer de replay.
        /// </summary>
        public void StartRecording()
        {
            _isRecording = true;
            _recordTimer = 0f;
            Debug.Log("[ReplaySystem] Gravação iniciada.");
        }

        /// <summary>
        /// Para a gravação do buffer.
        /// </summary>
        public void StopRecording()
        {
            _isRecording = false;
        }

        /// <summary>
        /// Registra um objeto para ser rastreado no replay.
        /// </summary>
        public void RegisterObject(Transform obj)
        {
            if (obj == null || _trackedObjects.Contains(obj)) return;

            _trackedObjects.Add(obj);
            _snapshots[obj] = new Queue<ObjectSnapshot>();

            Debug.Log($"[ReplaySystem] Objeto registrado: {obj.name}");
        }

        /// <summary>
        /// Registra um grupo de jogadores para rastreamento.
        /// </summary>
        public void RegisterPlayers(List<PlayerController> players)
        {
            foreach (PlayerController player in players)
            {
                if (player != null)
                    RegisterObject(player.transform);
            }
        }

        // ===== GRAVAÇÃO =====

        /// <summary>
        /// Captura as posições de todos os objetos rastreados neste frame.
        /// Remove snapshots antigos além do buffer configurado.
        /// </summary>
        private void RecordSnapshot()
        {
            float now = Time.time;
            float cutoffTime = now - _bufferDuration;

            foreach (Transform obj in _trackedObjects)
            {
                if (obj == null) continue;

                if (!_snapshots.TryGetValue(obj, out Queue<ObjectSnapshot> queue))
                    continue;

                queue.Enqueue(new ObjectSnapshot
                {
                    Position = obj.position,
                    Rotation = obj.rotation,
                    Timestamp = now
                });

                // Remove snapshots mais antigos que o buffer
                while (queue.Count > 0 && queue.Peek().Timestamp < cutoffTime)
                    queue.Dequeue();
            }
        }

        // ===== REPLAY =====

        /// <summary>
        /// Inicia o replay a partir do ponto de gol.
        /// </summary>
        /// <param name="goalPosition">Posição do gol para a câmera.</param>
        public void PlayGoalReplay(Vector3 goalPosition)
        {
            if (_isReplaying) return;

            StopRecording();

            StartCoroutine(ReplayCoroutine(goalPosition));
        }

        private IEnumerator ReplayCoroutine(Vector3 goalPosition)
        {
            _isReplaying = true;
            OnReplayStarted?.Invoke();

            // Congela a física enquanto prepara o replay
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.3f);

            // Ativa câmera de replay
            _cameraController?.StartGoalReplay(goalPosition, _replayDuration);

            // Coleta snapshots de cada objeto para reprodução
            Dictionary<Transform, List<ObjectSnapshot>> replayData
                = new Dictionary<Transform, List<ObjectSnapshot>>();

            foreach (Transform obj in _trackedObjects)
            {
                if (_snapshots.TryGetValue(obj, out Queue<ObjectSnapshot> queue))
                {
                    replayData[obj] = new List<ObjectSnapshot>(queue);
                }
            }

            // Reproduz em câmera lenta
            Time.timeScale = _replaySpeed;

            float elapsed = 0f;

            while (elapsed < _replayDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalizedTime = elapsed / _replayDuration;

                // Interpola as posições de cada objeto
                foreach (Transform obj in _trackedObjects)
                {
                    if (obj == null) continue;
                    if (!replayData.TryGetValue(obj, out List<ObjectSnapshot> snapshots)) continue;
                    if (snapshots.Count < 2) continue;

                    // Mapeia o tempo do replay para os snapshots gravados
                    float targetIndex = normalizedTime * (snapshots.Count - 1);
                    int indexA = Mathf.FloorToInt(targetIndex);
                    int indexB = Mathf.Min(indexA + 1, snapshots.Count - 1);
                    float t = targetIndex - indexA;

                    obj.position = Vector3.Lerp(snapshots[indexA].Position, snapshots[indexB].Position, t);
                    obj.rotation = Quaternion.Slerp(snapshots[indexA].Rotation, snapshots[indexB].Rotation, t);
                }

                yield return null;
            }

            // Finaliza o replay
            Time.timeScale = 1f;
            _isReplaying = false;

            // Reinicia a gravação
            StartRecording();

            OnReplayFinished?.Invoke();

            Debug.Log("[ReplaySystem] Replay concluído.");
        }

        // ===== LIMPEZA =====

        /// <summary>
        /// Limpa o buffer de replay (ao iniciar nova partida).
        /// </summary>
        public void ClearBuffer()
        {
            foreach (var key in _snapshots.Keys)
                _snapshots[key].Clear();
        }
    }
}
