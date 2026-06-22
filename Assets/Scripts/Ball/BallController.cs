using System;
using UnityEngine;
using CampoDeTerraFC.Effects;

namespace CampoDeTerraFC.Ball
{
    public enum SurfaceType { Dirt, Grass, Concrete, Mud, Asphalt }

    /// <summary>
    /// Controlador completo da bola de futebol.
    /// FUSÃO: Física completa do Sprint1 (Magnus effect, spin decay, bounce, atrito por superfície,
    ///        shadow projection) + runtime direto do Unity2022 (RaiseGoal, DetachFromPlayer público).
    /// APIs corrigidas para Unity 2022.3 LTS: velocity, drag, angularDrag, PhysicMaterial.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class BallController : MonoBehaviour
    {
        // ===== COMPONENTES =====
        private Rigidbody     _rb;
        private SphereCollider _col;
        private BallEffectsController _effects;

        // ===== CONFIG =====
        [Header("Física da Bola")]
        [SerializeField, Range(0f, 2f)]  private float _magnusCoefficient = 0.4f;
        [SerializeField, Range(0f, 1f)]  private float _airDrag            = 0.15f;
        [SerializeField, Range(0f, 1f)]  private float _bounciness         = 0.65f;

        [Header("Referências")]
        [SerializeField] private TrailRenderer _trailRenderer;

        // ===== ESTADO =====
        public float       CurrentSpeed      => _rb != null ? _rb.velocity.magnitude : 0f;
        public bool        IsAirborne        { get; private set; }
        public bool        IsAttachedToPlayer{ get; private set; }
        public Player.PlayerController AttachedPlayer { get; private set; }
        public SurfaceType LastSurface       { get; private set; } = SurfaceType.Dirt;

        private Vector3 _spin;
        private const float GROUND_CHECK_RADIUS = 0.12f;
        private const float MIN_SPEED           = 0.1f;
        private const float BALL_MASS_KG        = 0.43f;
        private const float MAX_SPEED           = 35f;

        // ===== EVENTOS =====
        public event Action<Vector3, SurfaceType> OnBounce;
        public event Action<Vector3>              OnPostHit;
        public event Action<int>                  OnGoalScored;

        // ===== UNITY LIFECYCLE =====
        private void Awake()
        {
            _rb  = GetComponent<Rigidbody>();
            _col = GetComponent<SphereCollider>();
            _effects = GetComponent<BallEffectsController>();
            ConfigurePhysics();
        }

        private void Start()
        {
            // Aplica configurações do GameConfigSO se disponível
            var config = Core.GameManager.Instance?.GetConfig();
            if (config != null)
            {
                _bounciness = config.BallBounciness;
                _airDrag    = 0.12f;
            }
        }

        private void FixedUpdate()
        {
            if (IsAttachedToPlayer) return;
            CheckAirborne();
            ApplyMagnusEffect();
            ApplyAirDrag();
            ApplySpinDecay();
            ClampMaxSpeed();
        }

        // ===== INICIALIZAÇÃO =====
        private void ConfigurePhysics()
        {
            _rb.mass                    = BALL_MASS_KG;
            _rb.drag                    = 0f;      // drag controlado manualmente (mais preciso)
            _rb.angularDrag             = 0.5f;
            _rb.interpolation           = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode  = CollisionDetectionMode.Continuous;
            _rb.useGravity              = true;

            var mat          = new PhysicMaterial("BallPhysics");
            mat.bounciness          = _bounciness;
            mat.dynamicFriction     = 0.4f;
            mat.staticFriction      = 0.5f;
            mat.frictionCombine     = PhysicMaterialCombine.Multiply;
            mat.bounceCombine       = PhysicMaterialCombine.Maximum;
            _col.material           = mat;
        }

        // ===== FÍSICA =====
        private void ApplyMagnusEffect()
        {
            if (_spin.magnitude < 0.01f || CurrentSpeed < MIN_SPEED) return;
            _rb.AddForce(_magnusCoefficient * Vector3.Cross(_spin, _rb.velocity), ForceMode.Force);
        }

        private void ApplyAirDrag()
        {
            if (!IsAirborne || CurrentSpeed < MIN_SPEED) return;
            _rb.AddForce(-_airDrag * CurrentSpeed * _rb.velocity, ForceMode.Force);
        }

        private void ApplySpinDecay()
        {
            _spin *= IsAirborne ? 0.98f : 0.92f;
            if (_spin.magnitude < 0.01f) _spin = Vector3.zero;
        }

        private void CheckAirborne()
        {
            bool prev = IsAirborne;
            IsAirborne = !Physics.CheckSphere(
                transform.position + Vector3.down * (_col.radius - GROUND_CHECK_RADIUS),
                GROUND_CHECK_RADIUS, LayerMask.GetMask("Ground"));
            if (prev && !IsAirborne)
            {
                OnBounce?.Invoke(transform.position, LastSurface);
                _effects?.PlayBounce(transform.position, LastSurface);
            }
        }

        private void ClampMaxSpeed()
        {
            if (_rb.velocity.magnitude > MAX_SPEED)
                _rb.velocity = _rb.velocity.normalized * MAX_SPEED;
        }

        // ===== API PÚBLICA — CHUTES E PASSES =====

        /// <summary>Aplica força de chute com spin lateral para curva.</summary>
        public void ApplyShoot(Vector3 direction, float forceMagnitude, float spin = 0f)
        {
            DetachFromPlayer();
            Vector3 dir = Quaternion.AngleAxis(-12f, Vector3.Cross(direction, Vector3.up)) * direction;
            dir.Normalize();
            _rb.velocity = Vector3.zero;
            _rb.AddForce(dir * forceMagnitude, ForceMode.Impulse);
            _spin = new Vector3(0f, spin, 0f);
            _effects?.PlayKick(transform.position);
            _trailRenderer?.Clear();
            Debug.Log(string.Format("[BallController] Chute: força={0:F1} spin={1:F1}", forceMagnitude, spin));
        }

        /// <summary>Passe rasteiro ou aéreo para uma posição alvo.</summary>
        public void ApplyPass(Vector3 targetPosition, float forceMult, bool isLong)
        {
            DetachFromPlayer();
            Vector3 toTarget = targetPosition - transform.position;
            float dist = toTarget.magnitude;
            Vector3 dir = toTarget.normalized;
            if (isLong)
                dir = Quaternion.AngleAxis(-15f, Vector3.Cross(dir, Vector3.up)) * dir;
            _rb.velocity = Vector3.zero;
            _rb.AddForce(dir * Mathf.Clamp(dist * forceMult, 3f, 25f), ForceMode.Impulse);
            _effects?.PlayKick(transform.position);
        }

        /// <summary>Lançamento em profundidade para o espaço.</summary>
        public void ApplyThroughBall(Vector3 direction, float forceMult)
        {
            Vector3 dir = Quaternion.AngleAxis(-20f, Vector3.Cross(direction, Vector3.up)) * direction;
            ApplyShoot(dir, 20f * forceMult, 0f);
        }

        // ===== CONTROLE DE POSSE =====

        public void AttachToPlayer(Player.PlayerController player)
        {
            if (player == null) return;
            IsAttachedToPlayer = true;
            AttachedPlayer = player;
            _rb.isKinematic = true;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        public void DetachFromPlayer()
        {
            IsAttachedToPlayer = false;
            AttachedPlayer = null;
            _rb.isKinematic = false;
        }

        /// <summary>Notifica que a bola entrou no gol (chamado pelo GoalTrigger).</summary>
        public void RaiseGoal(int goalIndex) => OnGoalScored?.Invoke(goalIndex);

        // ===== COLISÕES =====
        private void OnCollisionEnter(Collision c)
        {
            if (c.gameObject.CompareTag("Post"))
            {
                OnPostHit?.Invoke(c.contacts[0].point);
                _effects?.PlayPostHit(c.contacts[0].point);
                Debug.Log("[BallController] Bola na trave!");
            }
            if (c.gameObject.CompareTag("Ground"))
            {
                var surface = c.gameObject.GetComponent<FieldSurface>();
                LastSurface = surface != null ? surface.SurfaceType : SurfaceType.Dirt;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _rb == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, _rb.velocity * 0.1f);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, _spin * 0.1f);
        }
#endif
    }
}
