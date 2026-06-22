using UnityEngine;

namespace CampoDeTerraFC.Animation
{
    /// <summary>
    /// Controla o Animator do jogador.
    /// Traduz os estados de gameplay em parâmetros de animação.
    /// Todos os parâmetros são cacheados via hash para performance.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        // ===== COMPONENTES =====

        private Animator _animator;

        // ===== HASHES DOS PARÂMETROS (cacheados para evitar string lookup) =====

        private static readonly int SPEED_HASH = Animator.StringToHash("Speed");
        private static readonly int IS_SPRINTING_HASH = Animator.StringToHash("IsSprinting");
        private static readonly int IS_AIRBORNE_HASH = Animator.StringToHash("IsAirborne");
        private static readonly int HAS_BALL_HASH = Animator.StringToHash("HasBall");

        // Triggers
        private static readonly int SHOOT_TRIGGER_HASH = Animator.StringToHash("Shoot");
        private static readonly int SHOOT_STRONG_TRIGGER_HASH = Animator.StringToHash("ShootStrong");
        private static readonly int PASS_SHORT_TRIGGER_HASH = Animator.StringToHash("PassShort");
        private static readonly int PASS_LONG_TRIGGER_HASH = Animator.StringToHash("PassLong");
        private static readonly int DRIBBLE_TRIGGER_HASH = Animator.StringToHash("Dribble");
        private static readonly int SLIDE_TRIGGER_HASH = Animator.StringToHash("Slide");
        private static readonly int HEADER_TRIGGER_HASH = Animator.StringToHash("Header");
        private static readonly int BALL_CONTROL_TRIGGER_HASH = Animator.StringToHash("BallControl");
        private static readonly int CELEBRATE_HASH = Animator.StringToHash("Celebrate");
        private static readonly int FALL_TRIGGER_HASH = Animator.StringToHash("Fall");

        // Floats para blend trees
        private static readonly int MOVE_X_HASH = Animator.StringToHash("MoveX");
        private static readonly int MOVE_Y_HASH = Animator.StringToHash("MoveY");
        private static readonly int SHOOT_FORCE_HASH = Animator.StringToHash("ShootForce");

        // ===== CONFIGURAÇÃO =====

        [Header("Suavização")]
        [Tooltip("Velocidade de suavização do parâmetro Speed para o Animator.")]
        [SerializeField, Range(0.01f, 0.5f)] private float _speedSmoothTime = 0.1f;

        // ===== ESTADO INTERNO =====

        private float _currentAnimSpeed;
        private float _speedSmoothVelocity;

        // ===== UNITY LIFECYCLE =====

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_animator == null)
            {
                Debug.LogError($"[PlayerAnimationController] Animator não encontrado em {gameObject.name}");
            }
        }

        // ===== PARÂMETROS CONTÍNUOS =====

        /// <summary>
        /// Atualiza o parâmetro de velocidade com suavização.
        /// Controla a transição entre Idle, Walk e Run no Animator.
        /// </summary>
        /// <param name="speed">Velocidade atual (unidades/segundo).</param>
        public void SetSpeed(float speed)
        {
            if (_animator == null) return;

            _currentAnimSpeed = Mathf.SmoothDamp(
                _currentAnimSpeed, speed, ref _speedSmoothVelocity, _speedSmoothTime);

            _animator.SetFloat(SPEED_HASH, _currentAnimSpeed);
        }

        /// <summary>
        /// Atualiza o estado de sprint no Animator.
        /// </summary>
        public void SetSprinting(bool isSprinting)
        {
            _animator?.SetBool(IS_SPRINTING_HASH, isSprinting);
        }

        /// <summary>
        /// Atualiza o estado de estar no ar (para blend de animações aéreas).
        /// </summary>
        public void SetAirborne(bool isAirborne)
        {
            _animator?.SetBool(IS_AIRBORNE_HASH, isAirborne);
        }

        /// <summary>
        /// Define se o jogador está com a bola (muda postura e idle).
        /// </summary>
        public void SetHasBall(bool hasBall)
        {
            _animator?.SetBool(HAS_BALL_HASH, hasBall);
        }

        /// <summary>
        /// Atualiza a direção de movimento para o blend tree 2D.
        /// </summary>
        /// <param name="moveDirection">Direção normalizada de movimento (-1 a 1).</param>
        public void SetMoveDirection(Vector2 moveDirection)
        {
            if (_animator == null) return;

            _animator.SetFloat(MOVE_X_HASH, moveDirection.x);
            _animator.SetFloat(MOVE_Y_HASH, moveDirection.y);
        }

        // ===== TRIGGERS DE AÇÃO =====

        /// <summary>
        /// Dispara a animação de chute.
        /// </summary>
        /// <param name="charge">Força do chute (0-1). Acima de 0.6 usa animação de chute forte.</param>
        public void TriggerShoot(float charge)
        {
            if (_animator == null) return;

            _animator.SetFloat(SHOOT_FORCE_HASH, charge);

            if (charge > 0.6f)
                _animator.SetTrigger(SHOOT_STRONG_TRIGGER_HASH);
            else
                _animator.SetTrigger(SHOOT_TRIGGER_HASH);
        }

        /// <summary>
        /// Dispara a animação de passe.
        /// </summary>
        /// <param name="isLong">Se é passe longo (animação diferente do passe curto).</param>
        public void TriggerPass(bool isLong)
        {
            if (_animator == null) return;

            _animator.SetTrigger(isLong ? PASS_LONG_TRIGGER_HASH : PASS_SHORT_TRIGGER_HASH);
        }

        /// <summary>
        /// Dispara a animação de drible.
        /// </summary>
        public void TriggerDribble()
        {
            _animator?.SetTrigger(DRIBBLE_TRIGGER_HASH);
        }

        /// <summary>
        /// Dispara a animação de carrinho.
        /// </summary>
        public void TriggerSlide()
        {
            _animator?.SetTrigger(SLIDE_TRIGGER_HASH);
        }

        /// <summary>
        /// Dispara a animação de cabeceio.
        /// </summary>
        public void TriggerHeader()
        {
            _animator?.SetTrigger(HEADER_TRIGGER_HASH);
        }

        /// <summary>
        /// Dispara a animação de domínio de bola.
        /// </summary>
        public void TriggerBallControl()
        {
            _animator?.SetTrigger(BALL_CONTROL_TRIGGER_HASH);
        }

        /// <summary>
        /// Dispara uma animação de comemoração.
        /// </summary>
        /// <param name="celebrationIndex">Índice da comemoração (0-N no parâmetro do Animator).</param>
        public void TriggerCelebration(int celebrationIndex = 0)
        {
            if (_animator == null) return;

            _animator.SetInteger("CelebrationIndex", celebrationIndex);
            _animator.SetTrigger(CELEBRATE_HASH);
        }

        /// <summary>
        /// Dispara a animação de queda.
        /// </summary>
        public void TriggerFall()
        {
            _animator?.SetTrigger(FALL_TRIGGER_HASH);
        }

        // ===== UTILITÁRIOS =====

        /// <summary>
        /// Verifica se o Animator está no estado especificado.
        /// </summary>
        public bool IsInState(string stateName, int layerIndex = 0)
        {
            return _animator != null &&
                   _animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName);
        }

        /// <summary>
        /// Retorna a velocidade de transição do estado atual (0-1).
        /// </summary>
        public float GetNormalizedTime(int layerIndex = 0)
        {
            return _animator != null
                ? _animator.GetCurrentAnimatorStateInfo(layerIndex).normalizedTime
                : 0f;
        }
    }
}
