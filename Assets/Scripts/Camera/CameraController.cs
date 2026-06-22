using System;
using System.Collections;
using UnityEngine;
using CampoDeTerraFC.Ball;
using CampoDeTerraFC.Player;
using CampoDeTerraFC.Config;
using CampoDeTerraFC.Core;
using CampoDeTerraFC.Core.Services;

namespace CampoDeTerraFC.Camera
{
    /// <summary>
    /// Controla o sistema de câmera do jogo.
    /// Implementa seguimento suave da bola/jogador, zoom dinâmico,
    /// câmera de replay cinemática e shake de impacto.
    /// Funciona independentemente do Cinemachine mas é preparado para integração.
    /// </summary>
    public sealed class CameraController : MonoBehaviour
    {
        // ===== COMPONENTES =====

        private UnityEngine.Camera _camera;

        // ===== REFERÊNCIAS =====

        [Header("Referências")]
        [SerializeField] private BallController _ball;
        [SerializeField] private Transform _fieldCenter;

        // ===== CONFIGURAÇÃO =====

        [Header("Configuração de Câmera")]
        [SerializeField] private float _defaultHeight = 15f;
        [SerializeField] private float _defaultAngle = 55f;
        [SerializeField] private float _defaultDistance = 18f;
        [SerializeField] private float _smoothTime = 0.2f;
        [SerializeField] private float _minZoom = 12f;
        [SerializeField] private float _maxZoom = 28f;

        [Header("Limites do Campo")]
        [SerializeField] private float _fieldHalfWidth = 25f;
        [SerializeField] private float _fieldHalfLength = 35f;

        // ===== ESTADO =====

        /// <summary>Modo atual da câmera.</summary>
        public CameraMode Mode { get; private set; } = CameraMode.FollowBall;

        private Vector3 _currentVelocity;
        private float _currentZoomVelocity;
        private float _currentZoom;

        private bool _isShaking;
        private float _shakeTimer;
        private float _shakeMagnitude;
        private Vector3 _shakeOffset;

        // ===== REPLAY =====

        private Vector3 _replayStartPos;
        private Quaternion _replayStartRot;
        private float _replayTimer;
        private bool _isInReplay;

        // ===== CONSTANTES =====

        private const float ZOOM_SMOOTHTIME = 0.3f;
        private const float NEAR_GOAL_ZOOM_THRESHOLD = 15f; // Quando começa a dar zoom-in
        private const float MIN_GOAL_ZOOM = 8f;

        // ===== UNITY LIFECYCLE =====

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            if (_camera == null)
                _camera = UnityEngine.Camera.main;

            _currentZoom = _defaultDistance;
        }

        private void Start()
        {
            GameConfigSO config = Core.GameManager.Instance?.GetConfig();
            if (config != null)
            {
                _smoothTime = config.CameraSmoothTime;
                _defaultDistance = config.DefaultCameraDistance;
                _defaultAngle = config.DefaultCameraAngle;
            }
        }

        private void LateUpdate()
        {
            if (_isInReplay)
            {
                UpdateReplayCamera();
                return;
            }

            switch (Mode)
            {
                case CameraMode.FollowBall:
                    UpdateFollowBall();
                    break;
                case CameraMode.FollowPlayer:
                    UpdateFollowPlayer();
                    break;
                case CameraMode.Static:
                    // Câmera estática — não atualiza posição
                    break;
                case CameraMode.Overview:
                    UpdateOverview();
                    break;
            }

            ApplyShake();
        }

        // ===== MODOS DE CÂMERA =====

        /// <summary>
        /// Câmera principal: segue a bola com zoom dinâmico.
        /// </summary>
        private void UpdateFollowBall()
        {
            if (_ball == null) return;

            Vector3 ballPos = _ball.transform.position;

            // Calcula zoom baseado na proximidade do gol
            float distToEdge = Mathf.Min(
                _fieldHalfLength - Mathf.Abs(ballPos.z),
                _fieldHalfWidth - Mathf.Abs(ballPos.x));

            float targetZoom = distToEdge < NEAR_GOAL_ZOOM_THRESHOLD
                ? Mathf.Lerp(MIN_GOAL_ZOOM, _defaultDistance,
                    distToEdge / NEAR_GOAL_ZOOM_THRESHOLD)
                : _defaultDistance;

            _currentZoom = Mathf.SmoothDamp(_currentZoom, targetZoom, ref _currentZoomVelocity, ZOOM_SMOOTHTIME);

            // Calcula posição da câmera (angular isométrica)
            Vector3 targetPosition = CalculateCameraPosition(ballPos, _currentZoom);

            // Limita a câmera dentro dos bounds do campo
            targetPosition.x = Mathf.Clamp(targetPosition.x, -_fieldHalfWidth, _fieldHalfWidth);
            targetPosition.z = Mathf.Clamp(targetPosition.z, -_fieldHalfLength - 5f, -5f);

            // Aplica suavização
            transform.position = Vector3.SmoothDamp(
                transform.position, targetPosition, ref _currentVelocity, _smoothTime);

            // Rotação fixa apontando para baixo
            transform.rotation = Quaternion.Euler(_defaultAngle, 0f, 0f);
        }

        private void UpdateFollowPlayer()
        {
            // Implementado quando o jogador selecionado for exposto
            UpdateFollowBall();
        }

        private void UpdateOverview()
        {
            if (_fieldCenter == null) return;

            Vector3 overviewPos = _fieldCenter.position + new Vector3(0f, 35f, -15f);
            transform.position = Vector3.SmoothDamp(
                transform.position, overviewPos, ref _currentVelocity, _smoothTime * 2f);
            transform.LookAt(_fieldCenter);
        }

        // ===== REPLAY =====

        /// <summary>
        /// Inicia a câmera de replay cinemática ao marcar um gol.
        /// </summary>
        /// <param name="goalPosition">Posição onde o gol foi marcado.</param>
        /// <param name="duration">Duração do replay em segundos.</param>
        public void StartGoalReplay(Vector3 goalPosition, float duration)
        {
            _isInReplay = true;
            _replayTimer = duration;
            _replayStartPos = transform.position;
            _replayStartRot = transform.rotation;

            // Posiciona câmera de replay atrás do gol
            Vector3 replayPos = goalPosition + new Vector3(0f, 5f, -8f);
            StartCoroutine(ReplaySequence(goalPosition, replayPos, duration));
        }

        private IEnumerator ReplaySequence(Vector3 goalPos, Vector3 replayPos, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Órbita suave ao redor do gol durante o replay
                float angle = Mathf.Lerp(0f, 90f, t);
                float radius = 8f;

                Vector3 orbitPos = goalPos + new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                    4f,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius);

                transform.position = orbitPos;
                transform.LookAt(goalPos);

                yield return null;
            }

            _isInReplay = false;
        }

        private void UpdateReplayCamera()
        {
            // Gerenciado pela coroutine ReplaySequence
        }

        // ===== SHAKE =====

        /// <summary>
        /// Aplica um shake à câmera (impacto, gol, trave).
        /// </summary>
        /// <param name="magnitude">Intensidade do shake.</param>
        /// <param name="duration">Duração em segundos.</param>
        public void Shake(float magnitude, float duration)
        {
            _isShaking = true;
            _shakeMagnitude = magnitude;
            _shakeTimer = duration;
        }

        private void ApplyShake()
        {
            if (!_isShaking)
            {
                _shakeOffset = Vector3.zero;
                return;
            }

            _shakeTimer -= Time.deltaTime;

            if (_shakeTimer <= 0f)
            {
                _isShaking = false;
                _shakeOffset = Vector3.zero;
                return;
            }

            float decay = _shakeTimer / _shakeMagnitude;
            _shakeOffset = UnityEngine.Random.insideUnitSphere * _shakeMagnitude * decay;
            _shakeOffset.y = Mathf.Abs(_shakeOffset.y); // Shake apenas para cima

            transform.position += _shakeOffset;
        }

        // ===== UTILITÁRIOS =====

        /// <summary>
        /// Calcula a posição da câmera baseada no foco e na distância.
        /// </summary>
        private Vector3 CalculateCameraPosition(Vector3 focusPoint, float distance)
        {
            float angleRad = _defaultAngle * Mathf.Deg2Rad;
            float verticalDist = distance * Mathf.Sin(angleRad);
            float horizontalDist = distance * Mathf.Cos(angleRad);

            return new Vector3(
                focusPoint.x,
                focusPoint.y + verticalDist,
                focusPoint.z - horizontalDist);
        }

        // ===== API PÚBLICA =====

        /// <summary>
        /// Define o modo da câmera.
        /// </summary>
        public void SetMode(CameraMode mode)
        {
            Mode = mode;
            Debug.Log($"[CameraController] Modo alterado para: {mode}");
        }

        /// <summary>
        /// Define a bola a ser seguida.
        /// </summary>
        public void SetBall(BallController ball)
        {
            _ball = ball;
        }
    }

    /// <summary>Modos de câmera disponíveis.</summary>
    public enum CameraMode
    {
        FollowBall,    // Segue a bola (padrão)
        FollowPlayer,  // Segue o jogador controlado
        Static,        // Câmera fixa (cutscenes)
        Overview,      // Visão geral do campo
        Replay         // Câmera cinemática de replay
    }
}
