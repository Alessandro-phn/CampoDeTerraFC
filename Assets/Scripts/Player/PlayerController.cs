using System;
using UnityEngine;
using CampoDeTerraFC.Ball;
using CampoDeTerraFC.Animation;

namespace CampoDeTerraFC.Player
{
    /// <summary>
    /// Controla um personagem jogador — humano ou IA.
    /// FUSÃO: Movimentação inercial, passe inteligente com busca de alvo, chute carregado,
    ///        drible, carrinho e eventos do Sprint1 + acoplamento direto ao BallController
    ///        e InputManager do Unity2022 (sem ServiceLocator no Awake).
    /// APIs corrigidas: velocity, drag, angularDrag.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PlayerController : MonoBehaviour
    {
        private Rigidbody _rb;
        private PlayerAnimationController _anim;

        [Header("Movimento")]
        [SerializeField] private float _walkSpeed    = 5f;
        [SerializeField] private float _sprintSpeed  = 7.5f;
        [SerializeField] private float _accelTime    = 0.12f;
        [SerializeField] private float _decelTime    = 0.08f;
        [SerializeField] private float _rotationSpeed = 12f;

        [Header("Bola")]
        [SerializeField] private float _ballControlRadius = 1.5f;

        // ===== ESTADO PÚBLICO =====
        public bool IsControlledByHuman { get; private set; }
        public bool HasBall             { get; private set; }
        public int  TeamIndex           { get; private set; } = 0;
        public Data.PlayerDataSO Data   { get; private set; }
        public float CurrentSpeed       => _rb != null ? _rb.velocity.magnitude : 0f;

        // ===== ESTADO INTERNO =====
        private Vector2 _moveInput;
        private bool    _isSprinting;
        private Vector3 _smoothVel;
        private bool    _isSliding;
        private float   _slideTimer;
        private BallController _ball;
        private Input.InputManager _input;
        private GameObject _arrow; // indicador visual de jogador selecionado

        // ===== CONSTANTES =====
        private const float SLIDE_DURATION    = 0.6f;
        private const float BALL_HOLD_OFFSET  = 0.55f; // metros à frente

        // ===== EVENTOS =====
        public event Action OnBallReceived;
        public event Action OnBallLost;
        public event Action OnControlStarted;
        public event Action OnControlLost;

        // ===== UNITY LIFECYCLE =====
        private void Awake()
        {
            _rb   = GetComponent<Rigidbody>();
            _anim = GetComponent<PlayerAnimationController>();
            _arrow = transform.Find("PlayerArrow")?.gameObject;
            ConfigureRigidbody();
        }

        private void Start()
        {
            _ball  = FindObjectOfType<BallController>();
            _input = Input.InputManager.Instance;
        }

        private void Update()
        {
            UpdateSlide();
            if (HasBall && _ball != null)
                HoldBall();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
        }

        private void OnDestroy()
        {
            UnbindInput();
        }

        // ===== INICIALIZAÇÃO =====
        private void ConfigureRigidbody()
        {
            _rb.freezeRotation         = true;
            _rb.interpolation          = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _rb.drag                   = 5f;
            _rb.angularDrag            = 10f;
        }

        public void Initialize(Data.PlayerDataSO data, int teamIndex)
        {
            Data      = data;
            TeamIndex = teamIndex;

            if (data != null)
            {
                float speedFactor = Mathf.Lerp(0.75f, 1.25f, data.Stats.Speed / 99f);
                _sprintSpeed = 7.5f * speedFactor;
                _walkSpeed   = 5f   * speedFactor;
            }
        }

        // ===== CONTROLE HUMANO =====
        public void TakeControl()
        {
            // Remove controle de qualquer colega que estava selecionado
            foreach (var pc in FindObjectsOfType<PlayerController>())
                if (pc != this && pc.TeamIndex == TeamIndex && pc.IsControlledByHuman)
                    pc.ReleaseControl();

            IsControlledByHuman = true;
            if (_arrow != null) _arrow.SetActive(true);
            BindInput();
            OnControlStarted?.Invoke();
            Debug.Log("[PlayerController] Controle: " + (Data?.PlayerName ?? gameObject.name));
        }

        public void ReleaseControl()
        {
            IsControlledByHuman = false;
            if (_arrow != null) _arrow.SetActive(false);
            UnbindInput();
            _moveInput   = Vector2.zero;
            _isSprinting = false;
            OnControlLost?.Invoke();
        }

        /// <summary>Chamado pelo ProjectSetup para marcar o jogador inicial como humano.</summary>
        public void SetAsHumanControlled()
        {
            IsControlledByHuman = true;
            if (_arrow != null) _arrow.SetActive(true);
        }

        // ===== BINDING DE INPUT =====
        private void BindInput()
        {
            if (_input == null) _input = Input.InputManager.Instance;
            if (_input == null) return;
            _input.OnMoveInput          += HandleMove;
            _input.OnSprintInput        += HandleSprint;
            _input.OnShootReleased      += HandleShoot;
            _input.OnShortPassPressed   += HandleShortPass;
            _input.OnLongPassPressed    += HandleLongPass;
            _input.OnThroughBallPressed += HandleThroughBall;
            _input.OnDribblePressed     += HandleDribble;
            _input.OnSlidePressed       += HandleSlide;
            _input.OnSwitchPlayerPressed += HandleSwitchPlayer;
        }

        private void UnbindInput()
        {
            if (_input == null) return;
            _input.OnMoveInput          -= HandleMove;
            _input.OnSprintInput        -= HandleSprint;
            _input.OnShootReleased      -= HandleShoot;
            _input.OnShortPassPressed   -= HandleShortPass;
            _input.OnLongPassPressed    -= HandleLongPass;
            _input.OnThroughBallPressed -= HandleThroughBall;
            _input.OnDribblePressed     -= HandleDribble;
            _input.OnSlidePressed       -= HandleSlide;
            _input.OnSwitchPlayerPressed -= HandleSwitchPlayer;
        }

        // ===== HANDLERS DE INPUT =====
        private void HandleMove(Vector2 dir)   { _moveInput   = dir; }
        private void HandleSprint(bool s)       { _isSprinting = s; }

        private void HandleShoot(float charge)
        {
            if (!HasBall || _ball == null) return;
            float stat   = Data != null ? Mathf.Lerp(0.7f, 1.3f, Data.Stats.Shoot / 99f) : 1f;
            float force  = Mathf.Lerp(8f, 22f, charge) * stat;
            float spin   = _moveInput.x * 5f;
            _ball.ApplyShoot(transform.forward, force, spin);
            LoseBall();
            _anim?.TriggerShoot(charge);
        }

        private void HandleShortPass()
        {
            if (!HasBall || _ball == null) return;
            var target = FindBestPassTarget(false);
            if (target == null) return;
            _ball.ApplyPass(target.transform.position, 1.8f, false);
            LoseBall();
            _anim?.TriggerPass(false);
        }

        private void HandleLongPass()
        {
            if (!HasBall || _ball == null) return;
            var target = FindBestPassTarget(true);
            if (target == null) return;
            _ball.ApplyPass(target.transform.position, 2.8f, true);
            LoseBall();
            _anim?.TriggerPass(true);
        }

        private void HandleThroughBall()
        {
            if (!HasBall || _ball == null) return;
            _ball.ApplyThroughBall(transform.forward, 2.8f);
            LoseBall();
            _anim?.TriggerPass(true);
        }

        private void HandleDribble()
        {
            if (!HasBall) return;
            _anim?.TriggerDribble();
        }

        private void HandleSlide()
        {
            if (_isSliding) return;
            _isSliding  = true;
            _slideTimer = SLIDE_DURATION;
            _rb.AddForce(transform.forward * 8f, ForceMode.Impulse);
            _anim?.TriggerSlide();
        }

        private void HandleSwitchPlayer()
        {
            if (_ball == null) return;
            PlayerController nearest = null;
            float minD = float.MaxValue;
            foreach (var pc in FindObjectsOfType<PlayerController>())
            {
                if (pc == this || pc.TeamIndex != TeamIndex) continue;
                float d = Vector3.Distance(pc.transform.position, _ball.transform.position);
                if (d < minD) { minD = d; nearest = pc; }
            }
            nearest?.TakeControl();
        }

        // ===== MOVIMENTO =====
        private void ApplyMovement()
        {
            if (_isSliding) return;

            Vector3 dir = new Vector3(_moveInput.x, 0f, _moveInput.y);
            if (dir.magnitude < 0.05f)
            {
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
                _anim?.SetSpeed(0f);
                _anim?.SetSprinting(false);
                return;
            }

            dir.Normalize();
            float speed   = _isSprinting ? _sprintSpeed : _walkSpeed;
            Vector3 target = new Vector3(dir.x * speed, _rb.velocity.y, dir.z * speed);
            float smooth   = _moveInput.magnitude > 0.1f ? _accelTime : _decelTime;
            _rb.velocity   = Vector3.SmoothDamp(_rb.velocity, target, ref _smoothVel, smooth);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.fixedDeltaTime * _rotationSpeed);

            _anim?.SetSpeed(CurrentSpeed);
            _anim?.SetSprinting(_isSprinting && CurrentSpeed > 0.5f);
        }

        private void HoldBall()
        {
            Vector3 pos = transform.position + transform.forward * BALL_HOLD_OFFSET + Vector3.up * 0.11f;
            _ball.transform.position = Vector3.Lerp(_ball.transform.position, pos, Time.deltaTime * 15f);
        }

        private void UpdateSlide()
        {
            if (!_isSliding) return;
            _slideTimer -= Time.deltaTime;
            if (_slideTimer <= 0f) _isSliding = false;
        }

        // ===== POSSE DE BOLA =====
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Ball") || HasBall) return;
            var b = other.GetComponent<BallController>();
            if (b == null || (b.AttachedPlayer != null && b.AttachedPlayer != this)) return;
            GainBall(b);
        }

        public void GainBall(BallController ball)
        {
            if (ball.AttachedPlayer != null && ball.AttachedPlayer != this)
                ball.AttachedPlayer.LoseBall();
            HasBall = true;
            _ball   = ball;
            ball.AttachToPlayer(this);
            _anim?.SetHasBall(true);
            _anim?.TriggerBallControl();
            OnBallReceived?.Invoke();
        }

        public void LoseBall()
        {
            HasBall = false;
            _anim?.SetHasBall(false);
            OnBallLost?.Invoke();
        }

        // ===== IA — MÉTODOS PÚBLICOS =====
        public void AI_SetMoveDirection(Vector2 dir)
        {
            if (!IsControlledByHuman) _moveInput = dir;
        }

        public void AI_Shoot(float charge)
        {
            if (!HasBall || _ball == null) return;
            float force = Mathf.Lerp(8f, 20f, charge);
            _ball.ApplyShoot(transform.forward, force, 0f);
            LoseBall();
        }

        public void AI_Pass(Vector3 targetPos)
        {
            if (!HasBall || _ball == null) return;
            _ball.ApplyPass(targetPos, 1.8f, false);
            LoseBall();
        }

        // ===== BUSCA DE ALVO DE PASSE =====
        private PlayerController FindBestPassTarget(bool longPass)
        {
            float radius = longPass ? 30f : 15f;
            PlayerController best = null;
            float bestScore = float.MinValue;

            foreach (var pc in FindObjectsOfType<PlayerController>())
            {
                if (pc == this || pc.TeamIndex != TeamIndex) continue;
                Vector3 toTarget = pc.transform.position - transform.position;
                if (toTarget.magnitude > radius) continue;
                float dot   = Vector3.Dot(transform.forward, toTarget.normalized);
                if (dot < 0f) continue;
                float score = dot - toTarget.magnitude * 0.05f;
                if (score > bestScore) { bestScore = score; best = pc; }
            }
            return best;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _ballControlRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
#endif
    }
}
