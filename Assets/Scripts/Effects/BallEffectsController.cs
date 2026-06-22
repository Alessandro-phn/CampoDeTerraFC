using UnityEngine;
using CampoDeTerraFC.Ball;
using CampoDeTerraFC.Core;

namespace CampoDeTerraFC.Effects
{
    // ==========================================================================
    //  BALL EFFECTS CONTROLLER
    // ==========================================================================

    /// <summary>
    /// Gerencia os efeitos visuais e sonoros da bola.
    /// Partículas de poeira, rastros, impactos e flashes.
    /// Separado do BallController para manter responsabilidade única.
    /// </summary>
    [RequireComponent(typeof(BallController))]
    public sealed class BallEffectsController : MonoBehaviour
    {
        // ===== PARTÍCULAS =====

        [Header("Sistemas de Partícula")]
        [Tooltip("Poeira ao quicar na terra.")]
        [SerializeField] private ParticleSystem _dirtBounceParticles;

        [Tooltip("Rastro de poeira ao rolar no chão.")]
        [SerializeField] private ParticleSystem _rollDustParticles;

        [Tooltip("Faísca ao bater no poste/trave.")]
        [SerializeField] private ParticleSystem _postSparkParticles;

        [Tooltip("Explosão de poeira ao chutar forte.")]
        [SerializeField] private ParticleSystem _kickDustParticles;

        // ===== TRAIL =====

        [Header("Trail")]
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private float _trailMinSpeed = 8f; // Velocidade mínima para ativar o trail

        // ===== SOMBRA =====

        [Header("Sombra Dinâmica")]
        [SerializeField] private Transform _shadowTransform;
        [SerializeField] private float _maxShadowDistance = 8f;
        [SerializeField] private float _minShadowScale = 0.2f;
        [SerializeField] private float _maxShadowScale = 1f;

        // ===== REFERÊNCIAS =====

        private BallController _ballController;
        private ObjectPoolManager _poolManager;

        // ===== CONSTANTES =====

        private const float BOUNCE_SPEED_THRESHOLD = 3f;
        private const float ROLL_DUST_SPEED_THRESHOLD = 2f;

        // ===== UNITY LIFECYCLE =====

        private void Awake()
        {
            _ballController = GetComponent<BallController>();
        }

        private void Start()
        {
            _poolManager = Core.GameManager.Instance != null
                ? FindObjectOfType<Core.ObjectPoolManager>()
                : null;
        }

        private void Update()
        {
            UpdateTrail();
            UpdateShadow();
            UpdateRollDust();
        }

        // ===== ATUALIZAÇÕES POR FRAME =====

        /// <summary>
        /// Ativa/desativa o trail baseado na velocidade da bola.
        /// </summary>
        private void UpdateTrail()
        {
            if (_trailRenderer == null) return;

            bool shouldTrail = _ballController.CurrentSpeed > _trailMinSpeed && _ballController.IsAirborne;
            _trailRenderer.emitting = shouldTrail;
        }

        /// <summary>
        /// Atualiza a sombra projetada abaixo da bola.
        /// A sombra fica menor e mais translúcida quanto mais alto a bola está.
        /// </summary>
        private void UpdateShadow()
        {
            if (_shadowTransform == null) return;

            // Raycast para encontrar o chão
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, _maxShadowDistance))
            {
                _shadowTransform.gameObject.SetActive(true);
                _shadowTransform.position = hit.point + Vector3.up * 0.01f;
                _shadowTransform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

                // Escala e transparência baseadas na altura
                float heightFactor = 1f - (hit.distance / _maxShadowDistance);
                float scale = Mathf.Lerp(_minShadowScale, _maxShadowScale, heightFactor);
                _shadowTransform.localScale = new Vector3(scale, 1f, scale);

                // Transparência via material (se tiver SpriteRenderer ou MeshRenderer)
                Renderer shadowRenderer = _shadowTransform.GetComponent<Renderer>();
                if (shadowRenderer != null)
                {
                    Color c = shadowRenderer.material.color;
                    c.a = heightFactor * 0.5f;
                    shadowRenderer.material.color = c;
                }
            }
            else
            {
                _shadowTransform.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Emite poeira ao rolar no chão.
        /// </summary>
        private void UpdateRollDust()
        {
            if (_rollDustParticles == null) return;

            bool shouldEmit = !_ballController.IsAirborne &&
                              _ballController.CurrentSpeed > ROLL_DUST_SPEED_THRESHOLD &&
                              _ballController.LastSurface == SurfaceType.Dirt;

            if (shouldEmit && !_rollDustParticles.isPlaying)
                _rollDustParticles.Play();
            else if (!shouldEmit && _rollDustParticles.isPlaying)
                _rollDustParticles.Stop();
        }

        // ===== EFEITOS PONTUAIS =====

        /// <summary>
        /// Efeito ao quicar no chão.
        /// </summary>
        public void PlayBounce(Vector3 position, SurfaceType surface)
        {
            if (_ballController.CurrentSpeed < BOUNCE_SPEED_THRESHOLD) return;

            ParticleSystem bounceEffect = surface == SurfaceType.Dirt ? _dirtBounceParticles : null;

            if (bounceEffect != null)
            {
                bounceEffect.transform.position = position;
                bounceEffect.Play();
            }
        }

        /// <summary>
        /// Efeito ao bater na trave ou poste.
        /// </summary>
        public void PlayPostHit(Vector3 position)
        {
            if (_postSparkParticles == null) return;

            _postSparkParticles.transform.position = position;
            _postSparkParticles.Play();
        }

        /// <summary>
        /// Efeito ao ser chutada.
        /// </summary>
        public void PlayKick(Vector3 position)
        {
            if (_kickDustParticles == null) return;

            _kickDustParticles.transform.position = position;
            _kickDustParticles.Play();
        }
    }

    // ==========================================================================
    //  FIELD SURFACE
    // ==========================================================================

    /// <summary>
    /// Componente que marca a superfície de um objeto do campo.
    /// Usado pelo BallController para determinar atrito e efeitos de bounce.
    /// </summary>
    public sealed class FieldSurface : MonoBehaviour
    {
        [Tooltip("Tipo de superfície desta área do campo.")]
        public SurfaceType SurfaceType = SurfaceType.Dirt;

        [Tooltip("Coeficiente de atrito desta superfície (sobrescreve o valor padrão da config).")]
        [Range(0.1f, 1f)]
        public float CustomFriction = 0.6f;

        [Tooltip("Partícula específica de impacto para esta superfície.")]
        public ParticleSystem ImpactParticle;
    }

    // ==========================================================================
    //  DUST TRAIL (PEGADAS)
    // ==========================================================================

    /// <summary>
    /// Gera pegadas e rastros de poeira nos pés do jogador ao correr no campo de terra.
    /// </summary>
    public sealed class PlayerDustEffect : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private ParticleSystem _footstepDustLeft;
        [SerializeField] private ParticleSystem _footstepDustRight;

        [Tooltip("Velocidade mínima para emitir poeira.")]
        [SerializeField] private float _minSpeedForDust = 2f;

        [Tooltip("Superfícies que geram poeira.")]
        [SerializeField] private SurfaceType[] _dustSurfaces = { SurfaceType.Dirt, SurfaceType.Mud };

        // ===== ESTADO =====

        private float _currentSpeed;
        private bool _isOnDustSurface;
        private bool _isLeftFoot;
        private float _footstepTimer;

        private const float FOOTSTEP_INTERVAL = 0.35f;

        // ===== REFERÊNCIA =====

        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            _currentSpeed = _rigidbody != null ? _rigidbody.velocity.magnitude : 0f;

            if (_currentSpeed < _minSpeedForDust || !_isOnDustSurface) return;

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                _footstepTimer = FOOTSTEP_INTERVAL / (_currentSpeed / 5f); // Mais rápido = passos mais frequentes
                EmitFootstep();
            }
        }

        private void EmitFootstep()
        {
            ParticleSystem effect = _isLeftFoot ? _footstepDustLeft : _footstepDustRight;
            _isLeftFoot = !_isLeftFoot;

            if (effect != null && !effect.isPlaying)
                effect.Play();
        }

        /// <summary>
        /// Chamado quando o jogador toca em uma superfície nova.
        /// </summary>
        public void SetSurface(SurfaceType surface)
        {
            _isOnDustSurface = false;
            foreach (SurfaceType dustSurface in _dustSurfaces)
            {
                if (surface == dustSurface)
                {
                    _isOnDustSurface = true;
                    break;
                }
            }
        }
    }
}
