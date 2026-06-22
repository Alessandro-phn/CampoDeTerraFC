using System;
using System.Collections;
using UnityEngine;
using CampoDeTerraFC.Ball;

namespace CampoDeTerraFC.Goalkeeper
{
    /// <summary>
    /// Controlador completo do goleiro com IA própria.
    /// FUSÃO: Lógica completa do Sprint1 (dive preditivo, saída, tiro de meta, modo pênalti)
    ///        + compatibilidade Unity 2022.3 LTS (velocity, drag).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class GoalkeeperController : MonoBehaviour
    {
        private Rigidbody _rb;

        [Header("Referências")]
        [SerializeField] private BallController _ball;
        [SerializeField] private Transform _goalCenter;
        [SerializeField] private Transform _goalPostLeft;
        [SerializeField] private Transform _goalPostRight;

        [Header("Configuração")]
        [SerializeField] private Data.PlayerDataSO _goalkeeperData;
        [SerializeField] private float _maxSpeed     = 5f;
        [SerializeField] private float _diveForce    = 12f;
        [SerializeField] private float _lineOffset   = 0.5f;
        [SerializeField] private float _halfWidth    = 3.5f;

        // ===== ESTADO =====
        public GoalkeeperState State   { get; private set; } = GoalkeeperState.Idle;
        public int TeamIndex           { get; private set; }

        private bool    _isDiving;
        private bool    _hasBall;
        private float   _decisionTimer;
        private Vector3 _goalLineCenter;
        private Vector3 _currentVel;
        private float   _reactionTime  = 0.3f;

        // ===== CONSTANTES =====
        private const float DECISION_INTERVAL = 0.08f;
        private const float KICK_FORCE        = 25f;
        private const float DIVE_DURATION     = 0.7f;

        // ===== EVENTOS =====
        public event Action OnSaveAttempt;
        public event Action OnGoalKick;

        // ===== UNITY LIFECYCLE =====
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation         = true;
            _rb.interpolation          = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _rb.drag                   = 3f;
        }

        private void Start()
        {
            _ball = _ball ?? FindObjectOfType<BallController>();
            _goalLineCenter = transform.position;

            var config = Core.GameManager.Instance?.GetConfig();
            if (config != null)
            {
                _maxSpeed     = config.GoalkeeperMaxSpeed;
                _reactionTime = config.GoalkeeperPenaltyReactionTime;
            }

            ApplyStats();
        }

        private void Update()
        {
            if (_isDiving || _hasBall) return;
            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer <= 0f) { _decisionTimer = DECISION_INTERVAL; EvaluateSituation(); }
        }

        private void FixedUpdate()
        {
            if (_isDiving || _hasBall) return;
            TrackBallHorizontally();
            ApplyMovement();
        }

        // ===== INICIALIZAÇÃO =====
        public void Initialize(Data.PlayerDataSO data, int teamIndex)
        {
            _goalkeeperData = data;
            TeamIndex       = teamIndex;
            ApplyStats();
        }

        private void ApplyStats()
        {
            if (_goalkeeperData == null) return;
            float speedFactor = Mathf.Lerp(0.7f, 1.2f, _goalkeeperData.Stats.Speed / 99f);
            _maxSpeed     = _maxSpeed * speedFactor;
            float reflexFactor = Mathf.Lerp(1.4f, 0.7f, _goalkeeperData.Stats.Reflexes / 99f);
            _reactionTime = _reactionTime * reflexFactor;
        }

        // ===== IA =====
        private void EvaluateSituation()
        {
            if (_ball == null) return;

            Vector3 ballPos = _ball.transform.position;
            var     ballRb  = _ball.GetComponent<Rigidbody>();
            Vector3 ballVel = ballRb != null ? ballRb.velocity : Vector3.zero;

            bool comingToGoal = IsBallComingToGoal(ballPos, ballVel);

            if (comingToGoal && ballVel.magnitude > 5f)
            {
                float distToMe = Vector3.Distance(transform.position, ballPos);
                if (distToMe < 8f && ShouldDive())
                {
                    Vector3 predicted = PredictBallPos(0.35f, ballPos, ballVel);
                    StartCoroutine(DiveCoroutine(predicted));
                }
                else ChangeState(GoalkeeperState.PrepareDive);
            }
            else if (Vector3.Distance(transform.position, ballPos) < 6f)
            {
                ChangeState(GoalkeeperState.ComingOut);
            }
            else ChangeState(GoalkeeperState.Repositioning);
        }

        private bool ShouldDive()
        {
            return UnityEngine.Random.value < 0.04f; // triggera com baixa frequência para não spam
        }

        private bool IsBallComingToGoal(Vector3 ballPos, Vector3 ballVel)
        {
            if (_goalCenter == null) return false;
            Vector3 toGoal = _goalCenter.position - ballPos;
            return ballVel.magnitude > 2f && Vector3.Dot(ballVel.normalized, toGoal.normalized) > 0.5f;
        }

        private Vector3 PredictBallPos(float seconds, Vector3 pos, Vector3 vel)
        {
            return pos + vel * seconds + 0.5f * Physics.gravity * seconds * seconds;
        }

        // ===== MOVIMENTAÇÃO =====
        private void TrackBallHorizontally()
        {
            if (_ball == null) return;
            float leftLimit  = _goalPostLeft  != null ? _goalPostLeft.position.x  : -_halfWidth;
            float rightLimit = _goalPostRight != null ? _goalPostRight.position.x : _halfWidth;
            float targetX    = Mathf.Clamp(_ball.transform.position.x, leftLimit + 0.3f, rightLimit - 0.3f);
            float goalZ      = _goalLineCenter.z + _lineOffset * (TeamIndex == 1 ? -1f : 1f);

            _rb.velocity = new Vector3(
                (new Vector3(targetX, 0f, goalZ) - transform.position).normalized.x * _maxSpeed,
                _rb.velocity.y,
                0f);

            // Rotaciona para encarar a bola
            Vector3 look = (_ball.transform.position - transform.position);
            look.y = 0f;
            if (look.magnitude > 0.1f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.fixedDeltaTime * 8f);
        }

        private void ApplyMovement()
        {
            // Suaviza a velocidade
            _rb.velocity = new Vector3(
                Mathf.SmoothDamp(_rb.velocity.x, 0f, ref _currentVel.x, 0.05f),
                _rb.velocity.y,
                _rb.velocity.z);
        }

        // ===== DIVE =====
        private IEnumerator DiveCoroutine(Vector3 target)
        {
            _isDiving = true;
            ChangeState(GoalkeeperState.Diving);
            OnSaveAttempt?.Invoke();

            Vector3 dir = (target - transform.position).normalized;
            dir.z = 0f;
            if (dir.magnitude < 0.1f) dir = Vector3.right;

            _rb.AddForce(dir * _diveForce + Vector3.up * 3f, ForceMode.Impulse);

            yield return new WaitForSeconds(DIVE_DURATION);
            _isDiving = false;
            ChangeState(GoalkeeperState.Repositioning);

            // Retorna ao centro suavemente
            float t = 0f;
            Vector3 start = transform.position;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                transform.position = Vector3.Lerp(start, _goalLineCenter, t);
                yield return null;
            }
        }

        // ===== PÊNALTI =====
        public void EnterPenaltyMode()
        {
            ChangeState(GoalkeeperState.PenaltyMode);
            StartCoroutine(PenaltyRoutine());
        }

        private IEnumerator PenaltyRoutine()
        {
            yield return new WaitForSeconds(_reactionTime);

            float reflexFactor = _goalkeeperData != null ? _goalkeeperData.Stats.Reflexes / 99f : 0.5f;
            bool  reads        = UnityEngine.Random.value < (0.4f + reflexFactor * 0.35f);
            float side         = reads ? GetPredictedSide() : -GetPredictedSide();
            Vector3 target     = new Vector3(side * _halfWidth, 1f, _goalLineCenter.z);

            yield return DiveCoroutine(target);
        }

        private float GetPredictedSide() => UnityEngine.Random.value > 0.5f ? 1f : -1f;

        // ===== TIRO DE META =====
        private IEnumerator GoalKickSequence()
        {
            ChangeState(GoalkeeperState.GoalKick);
            yield return new WaitForSeconds(1.5f);
            if (_ball == null) { _hasBall = false; ChangeState(GoalkeeperState.Repositioning); yield break; }

            Vector3 dir = new Vector3(
                UnityEngine.Random.Range(-0.3f, 0.3f),
                0.4f,
                TeamIndex == 0 ? 1f : -1f);
            _ball.ApplyShoot(dir.normalized, KICK_FORCE, 0f);
            _hasBall = false;
            ChangeState(GoalkeeperState.Repositioning);
            OnGoalKick?.Invoke();
        }

        // ===== COLISÃO =====
        private void OnCollisionEnter(Collision c)
        {
            if (!c.gameObject.CompareTag("Ball")) return;
            var ball = c.gameObject.GetComponent<BallController>();
            if (ball == null) return;

            if (_isDiving || State == GoalkeeperState.ComingOut)
            {
                // Defesa — rebate
                Vector3 rebate = (c.contacts[0].point - _goalLineCenter).normalized;
                rebate.z  = TeamIndex == 0 ? -0.5f : 0.5f;
                rebate.y  = 0.4f;
                ball.ApplyShoot(rebate.normalized, 10f, 0f);
                Debug.Log("[GoalkeeperController] DEFESA!");
            }
            else
            {
                // Segurou
                _hasBall = true;
                ChangeState(GoalkeeperState.WithBall);
                StartCoroutine(GoalKickSequence());
            }
        }

        private void ChangeState(GoalkeeperState s) { State = s; }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 6f);
            Gizmos.color = Color.yellow;
            if (_goalPostLeft  != null) Gizmos.DrawLine(_goalPostLeft.position, _goalPostRight?.position ?? _goalPostLeft.position);
        }
#endif
    }

    public enum GoalkeeperState
    { Idle, Repositioning, ComingOut, PrepareDive, Diving, WithBall, GoalKick, PenaltyMode }
}
