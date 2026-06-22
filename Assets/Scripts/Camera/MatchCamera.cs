using System.Collections;
using UnityEngine;
using CampoDeTerraFC.Ball;

namespace CampoDeTerraFC.Camera
{
    /// <summary>
    /// Câmera de partida com seguimento suave, zoom dinâmico e screen shake.
    /// FUSÃO: Lógica de replay e zoom do Sprint1 + acoplamento direto do Unity2022 (SetBallTransform).
    /// APIs corrigidas para Unity 2022.3 LTS.
    /// </summary>
    public sealed class MatchCamera : MonoBehaviour
    {
        private Transform _ballTransform;

        [Header("Configuração")]
        [SerializeField] private float _smoothTime     = 0.2f;
        [SerializeField] private float _angle          = 55f;
        [SerializeField] private float _defaultDist    = 22f;
        [SerializeField] private float _minZoom        = 10f;
        [SerializeField] private float _fieldHalfX     = 25f;
        [SerializeField] private float _fieldHalfZ     = 38f;

        // ===== SHAKE =====
        private float   _shakeTimer;
        private float   _shakeMag;

        // ===== ZOOM =====
        private float   _currentDist;
        private float   _distVel;
        private const float ZOOM_SMOOTH = 0.3f;
        private const float NEAR_GOAL_THRESHOLD = 15f;
        private const float MIN_GOAL_ZOOM       = 10f;

        // ===== SEGUIMENTO =====
        private Vector3 _posVel;

        // ===== REPLAY =====
        private bool _isReplaying;
        private Coroutine _replayCo;

        private void Awake()
        {
            _currentDist = _defaultDist;
        }

        private void Start()
        {
            if (_ballTransform == null)
            {
                var ball = FindObjectOfType<BallController>();
                if (ball != null) _ballTransform = ball.transform;
            }
        }

        public void SetBallTransform(Transform ball) => _ballTransform = ball;

        private void LateUpdate()
        {
            if (_isReplaying) return;
            if (_ballTransform == null) { _ballTransform = FindObjectOfType<BallController>()?.transform; return; }

            UpdateZoom();
            UpdatePosition();
            ApplyShake();
        }

        private void UpdateZoom()
        {
            Vector3 bp = _ballTransform.position;
            float edgeDist = Mathf.Min(_fieldHalfZ - Mathf.Abs(bp.z), _fieldHalfX - Mathf.Abs(bp.x));
            float targetDist = edgeDist < NEAR_GOAL_THRESHOLD
                ? Mathf.Lerp(MIN_GOAL_ZOOM, _defaultDist, edgeDist / NEAR_GOAL_THRESHOLD)
                : _defaultDist;
            _currentDist = Mathf.SmoothDamp(_currentDist, targetDist, ref _distVel, ZOOM_SMOOTH);
        }

        private void UpdatePosition()
        {
            Vector3 bp = _ballTransform.position;
            float rad  = _angle * Mathf.Deg2Rad;
            float vd   = _currentDist * Mathf.Sin(rad);
            float hd   = _currentDist * Mathf.Cos(rad);

            Vector3 target = new Vector3(
                Mathf.Clamp(bp.x, -_fieldHalfX + 5f, _fieldHalfX - 5f),
                bp.y + vd,
                Mathf.Clamp(bp.z - hd, -_fieldHalfZ, -2f));

            transform.position = Vector3.SmoothDamp(transform.position, target, ref _posVel, _smoothTime);
            transform.rotation = Quaternion.Euler(_angle, 0f, 0f);
        }

        private void ApplyShake()
        {
            if (_shakeTimer <= 0f) return;
            _shakeTimer -= Time.deltaTime;
            float decay = _shakeTimer > 0f ? (_shakeTimer / _shakeMag) : 0f;
            transform.position += (Vector3)UnityEngine.Random.insideUnitCircle * _shakeMag * decay;
        }

        public void Shake(float magnitude = 0.3f, float duration = 0.4f)
        {
            _shakeMag   = magnitude;
            _shakeTimer = duration;
        }

        public void StartGoalReplay(Vector3 goalPosition, float duration)
        {
            if (_replayCo != null) StopCoroutine(_replayCo);
            _replayCo = StartCoroutine(ReplayOrbit(goalPosition, duration));
        }

        private IEnumerator ReplayOrbit(Vector3 pivot, float duration)
        {
            _isReplaying = true;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t   = elapsed / duration;
                float ang = Mathf.Lerp(0f, 90f, t) * Mathf.Deg2Rad;
                float r   = 8f;
                transform.position = pivot + new Vector3(Mathf.Sin(ang) * r, 4f, Mathf.Cos(ang) * r);
                transform.LookAt(pivot);
                yield return null;
            }
            _isReplaying = false;
        }
    }
}
