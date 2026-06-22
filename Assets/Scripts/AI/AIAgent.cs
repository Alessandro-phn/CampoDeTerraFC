using System;
using UnityEngine;
using UnityEngine.AI;
using CampoDeTerraFC.Ball;
using CampoDeTerraFC.Data;
using CampoDeTerraFC.Config;
using CampoDeTerraFC.Core;
using CampoDeTerraFC.Core.Services;
using CampoDeTerraFC.Player;

namespace CampoDeTerraFC.AI
{
    /// <summary>
    /// Agente base de Inteligência Artificial para jogadores de futebol.
    /// Implementa uma State Machine com estados: Idle, Chase, Mark, Attack, Pass, Shoot, Return.
    /// Subclasses especializam o comportamento por posição (Atacante, Meia, Defensor, Goleiro).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(PlayerController))]
    public abstract class AIAgent : MonoBehaviour
    {
        // ===== COMPONENTES =====

        protected NavMeshAgent _navAgent;
        protected PlayerController _playerController;

        // ===== REFERÊNCIAS DE CAMPO =====

        [Header("Referências de IA")]
        [SerializeField] protected BallController _ball;
        [SerializeField] protected Transform _ownGoal;
        [SerializeField] protected Transform _opponentGoal;

        // ===== CONFIGURAÇÃO =====

        protected GameConfigSO _config;
        protected PlayerDataSO _data;

        // ===== ESTADO =====

        /// <summary>Estado atual do agente.</summary>
        public AIState CurrentState { get; protected set; } = AIState.Idle;

        /// <summary>Se o agente está ativo (pode tomar decisões).</summary>
        public bool IsActive { get; set; } = true;

        // ===== PARÂMETROS CALCULADOS =====

        /// <summary>Posição de formação atribuída pelo TeamManager.</summary>
        public Vector3 FormationPosition { get; set; }

        /// <summary>Índice do time deste agente.</summary>
        public int TeamIndex { get; private set; }

        // ===== TIMERS =====

        private float _decisionTimer;
        private float _stateTimer;

        // ===== CONSTANTES =====

        private const float DECISION_INTERVAL = 0.15f;  // Frequência de tomada de decisão (s)
        private const float BALL_REACH_RADIUS = 1.8f;   // Raio para considerar "perto da bola"
        private const float SHOOT_RANGE = 18f;           // Raio de chute
        private const float PASS_RANGE = 25f;            // Raio de passe

        // ===== UNITY LIFECYCLE =====

        protected virtual void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
            _playerController = GetComponent<PlayerController>();

            ConfigureNavAgent();
        }

        protected virtual void Start()
        {
            _config = Core.GameManager.Instance?.GetConfig();
            _data = _playerController.Data;
            TeamIndex = _playerController.TeamIndex;

            ApplyStatsToNavAgent();
        }

        protected virtual void Update()
        {
            if (!IsActive) return;
            if (_playerController.IsControlledByHuman) return;

            _decisionTimer -= Time.deltaTime;
            _stateTimer += Time.deltaTime;

            if (_decisionTimer <= 0f)
            {
                _decisionTimer = DECISION_INTERVAL + UnityEngine.Random.Range(0f, 0.05f);
                EvaluateState();
            }

            ExecuteCurrentState();
        }

        // ===== CONFIGURAÇÃO =====

        /// <summary>
        /// Configura o NavMeshAgent com parâmetros base.
        /// </summary>
        private void ConfigureNavAgent()
        {
            _navAgent.stoppingDistance = 0.5f;
            _navAgent.angularSpeed = 360f;
            _navAgent.acceleration = 20f;
            _navAgent.autoBraking = true;
            _navAgent.updateRotation = true;
        }

        /// <summary>
        /// Aplica os atributos do jogador às propriedades do NavMeshAgent.
        /// </summary>
        private void ApplyStatsToNavAgent()
        {
            if (_data == null || _config == null) return;

            float speedFactor = Mathf.Lerp(0.7f, 1.3f, _data.Stats.Speed / 99f);
            _navAgent.speed = _config.BaseRunSpeed * speedFactor;

            // Dificuldade afeta o delay de reação
            float reactionFactor = _config.AIReactionDelay;
            _decisionTimer = reactionFactor;
        }

        // ===== STATE MACHINE =====

        /// <summary>
        /// Avalia o contexto e determina o melhor estado para o agente.
        /// Subclasses podem sobrescrever para comportamento de posição específico.
        /// </summary>
        protected virtual void EvaluateState()
        {
            if (_ball == null) return;

            bool hasBall = _playerController.HasBall;
            bool teamHasBall = TeamHasBall();
            float distToBall = DistanceToBall();

            if (hasBall)
            {
                EvaluateWithBall();
            }
            else if (teamHasBall)
            {
                EvaluateTeamHasBall();
            }
            else
            {
                EvaluateOpponentHasBall(distToBall);
            }
        }

        /// <summary>
        /// Toma decisão quando está com a bola.
        /// </summary>
        protected virtual void EvaluateWithBall()
        {
            float distToGoal = DistanceToOpponentGoal();

            // Está em posição de chute?
            if (distToGoal < SHOOT_RANGE && HasClearShotOnGoal())
            {
                ChangeState(AIState.Shoot);
                return;
            }

            // Tem colega livre para passar?
            if (HasOpenTeammate())
            {
                ChangeState(AIState.Pass);
                return;
            }

            // Dribla para frente
            ChangeState(AIState.Attack);
        }

        /// <summary>
        /// Toma decisão quando o time tem a bola mas este agente não.
        /// </summary>
        protected virtual void EvaluateTeamHasBall()
        {
            // Por padrão: move para posição de apoio
            ChangeState(AIState.Support);
        }

        /// <summary>
        /// Toma decisão quando o adversário tem a bola.
        /// </summary>
        protected virtual void EvaluateOpponentHasBall(float distToBall)
        {
            if (distToBall < 8f)
            {
                ChangeState(AIState.Mark);
            }
            else if (IsNearestToBall())
            {
                ChangeState(AIState.Chase);
            }
            else
            {
                ChangeState(AIState.Return);
            }
        }

        // ===== EXECUÇÃO DE ESTADO =====

        /// <summary>
        /// Executa a lógica do estado atual a cada frame.
        /// </summary>
        protected virtual void ExecuteCurrentState()
        {
            switch (CurrentState)
            {
                case AIState.Idle:
                    ExecuteIdle();
                    break;
                case AIState.Chase:
                    ExecuteChase();
                    break;
                case AIState.Mark:
                    ExecuteMark();
                    break;
                case AIState.Attack:
                    ExecuteAttack();
                    break;
                case AIState.Support:
                    ExecuteSupport();
                    break;
                case AIState.Pass:
                    ExecutePass();
                    break;
                case AIState.Shoot:
                    ExecuteShoot();
                    break;
                case AIState.Return:
                    ExecuteReturn();
                    break;
                case AIState.Pressure:
                    ExecutePressure();
                    break;
            }
        }

        // ===== IMPLEMENTAÇÕES DE ESTADO =====

        protected virtual void ExecuteIdle()
        {
            _navAgent.isStopped = true;
        }

        protected virtual void ExecuteChase()
        {
            if (_ball == null) return;
            MoveTo(_ball.transform.position);

            if (DistanceToBall() < BALL_REACH_RADIUS)
            {
                // Chegou na bola — próxima decisão vai mudar o estado
            }
        }

        protected virtual void ExecuteMark()
        {
            // Marca o adversário mais perigoso próximo
            PlayerController target = FindNearestOpponent();
            if (target == null)
            {
                ChangeState(AIState.Return);
                return;
            }

            // Posiciona entre o adversário e o próprio gol
            Vector3 toGoal = (_ownGoal != null ? _ownGoal.position : transform.position) - target.transform.position;
            Vector3 markPosition = target.transform.position + toGoal.normalized * 1.5f;
            MoveTo(markPosition);
        }

        protected virtual void ExecuteAttack()
        {
            if (_opponentGoal == null) return;
            // Avança em direção ao gol com a bola
            MoveTo(_opponentGoal.position);
        }

        protected virtual void ExecuteSupport()
        {
            // Move para posição de apoio baseada na formação
            MoveTo(GetSupportPosition());
        }

        protected virtual void ExecutePass()
        {
            if (!_playerController.HasBall)
            {
                ChangeState(AIState.Support);
                return;
            }

            // Pequeno delay antes de passar (simula tomada de decisão humana)
            if (_stateTimer < GetReactionDelay()) return;

            // O passe é executado via PlayerController com lógica de busca de alvo
            // Aqui apenas acionamos o input simulado
            SimulatePassInput();
            ChangeState(AIState.Support);
        }

        protected virtual void ExecuteShoot()
        {
            if (!_playerController.HasBall)
            {
                ChangeState(AIState.Support);
                return;
            }

            if (_stateTimer < GetReactionDelay()) return;

            // Vira para o gol antes de chutar
            if (_opponentGoal != null)
            {
                Vector3 toGoal = (_opponentGoal.position - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(toGoal);
            }

            SimulateShootInput();
            ChangeState(AIState.Return);
        }

        protected virtual void ExecuteReturn()
        {
            MoveTo(FormationPosition);
        }

        protected virtual void ExecutePressure()
        {
            if (_ball == null) return;
            // Pressão alta: vai direto ao portador da bola adversária
            PlayerController ballOwner = _ball.AttachedPlayer;
            if (ballOwner != null && ballOwner.TeamIndex != TeamIndex)
            {
                MoveTo(ballOwner.transform.position);
            }
            else
            {
                ChangeState(AIState.Chase);
            }
        }

        // ===== NAVEGAÇÃO =====

        /// <summary>
        /// Move o agente para uma posição usando NavMesh.
        /// </summary>
        protected void MoveTo(Vector3 position)
        {
            if (!_navAgent.isActiveAndEnabled) return;

            _navAgent.isStopped = false;
            _navAgent.SetDestination(position);
        }

        /// <summary>
        /// Para o agente imediatamente.
        /// </summary>
        protected void StopMovement()
        {
            _navAgent.isStopped = true;
            _navAgent.velocity = Vector3.zero;
        }

        // ===== MUDANÇA DE ESTADO =====

        /// <summary>
        /// Transiciona para um novo estado e reseta o timer de estado.
        /// </summary>
        protected void ChangeState(AIState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            _stateTimer = 0f;
        }

        // ===== INPUT SIMULADO =====

        /// <summary>
        /// Simula o input de passe da IA via PlayerController.
        /// </summary>
        private void SimulatePassInput()
        {
            // A precisão do passe depende da dificuldade e do atributo de passe
            float accuracy = _config?.GetAIPassAccuracy() ?? 0.78f;
            float passStat = _data != null ? Mathf.Lerp(0.5f, 1f, _data.Stats.Pass / 99f) : 1f;

            if (UnityEngine.Random.value < accuracy * passStat)
            {
                // Passe bem-sucedido — chama método interno do PlayerController
                // (via interface ou método público para não expor input diretamente)
                Debug.Log($"[AIAgent] {_data?.PlayerName} tenta passe.");
            }
        }

        /// <summary>
        /// Simula o input de chute da IA.
        /// </summary>
        private void SimulateShootInput()
        {
            float shootStat = _data != null ? Mathf.Lerp(0.5f, 1f, _data.Stats.Shoot / 99f) : 1f;
            float charge = Mathf.Lerp(0.4f, 1f, shootStat);

            Debug.Log($"[AIAgent] {_data?.PlayerName} chuta com força {charge:F2}.");
        }

        // ===== AVALIAÇÕES =====

        protected float DistanceToBall()
        {
            return _ball != null ? Vector3.Distance(transform.position, _ball.transform.position) : float.MaxValue;
        }

        protected float DistanceToOpponentGoal()
        {
            return _opponentGoal != null
                ? Vector3.Distance(transform.position, _opponentGoal.position)
                : float.MaxValue;
        }

        protected bool TeamHasBall()
        {
            return _ball != null && _ball.IsAttachedToPlayer && _ball.AttachedPlayer?.TeamIndex == TeamIndex;
        }

        protected bool IsNearestToBall()
        {
            // Verifica se este é o jogador do time mais próximo da bola
            // Implementação simplificada — TeamManager fará isso de forma mais eficiente
            return DistanceToBall() < 6f;
        }

        protected bool HasClearShotOnGoal()
        {
            if (_opponentGoal == null) return false;

            Vector3 toGoal = _opponentGoal.position - transform.position;

            // Raycast para verificar se há adversários bloqueando
            return !Physics.Raycast(
                transform.position + Vector3.up,
                toGoal.normalized,
                toGoal.magnitude,
                LayerMask.GetMask("Player"));
        }

        protected bool HasOpenTeammate()
        {
            // Lógica simplificada: verifica se há colega de equipe à frente
            Collider[] nearby = Physics.OverlapSphere(transform.position, PASS_RANGE);
            foreach (Collider c in nearby)
            {
                PlayerController p = c.GetComponent<PlayerController>();
                if (p != null && p != _playerController && p.TeamIndex == TeamIndex)
                {
                    float forwardDot = Vector3.Dot(
                        transform.forward,
                        (p.transform.position - transform.position).normalized);

                    if (forwardDot > 0.3f) return true;
                }
            }
            return false;
        }

        protected PlayerController FindNearestOpponent()
        {
            Collider[] nearby = Physics.OverlapSphere(transform.position, 12f);
            PlayerController nearest = null;
            float minDist = float.MaxValue;

            foreach (Collider c in nearby)
            {
                PlayerController p = c.GetComponent<PlayerController>();
                if (p != null && p.TeamIndex != TeamIndex)
                {
                    float d = Vector3.Distance(transform.position, p.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        nearest = p;
                    }
                }
            }

            return nearest;
        }

        /// <summary>
        /// Calcula a posição de apoio baseada na formação e na posição da bola.
        /// </summary>
        protected virtual Vector3 GetSupportPosition()
        {
            // Por padrão volta à posição de formação
            return FormationPosition;
        }

        protected float GetReactionDelay()
        {
            float base_delay = _config?.AIReactionDelay ?? 0.2f;
            float statMod = _data != null ? Mathf.Lerp(0.5f, 1.5f, 1f - (_data.Stats.Speed / 99f)) : 1f;
            return base_delay * statMod;
        }

        // ===== GIZMOS =====

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            // Estado atual
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f,
                $"{_data?.PlayerName ?? name}\n{CurrentState}");

            // Raio de chute
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, SHOOT_RANGE);

            // Posição de formação
            if (FormationPosition != Vector3.zero)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(FormationPosition, 0.5f);
                Gizmos.DrawLine(transform.position, FormationPosition);
            }
        }
#endif
    }

    // ===== ENUM =====

    /// <summary>Estados da máquina de estados da IA.</summary>
    public enum AIState
    {
        Idle,
        Chase,      // Persegue a bola
        Mark,       // Marca um adversário
        Attack,     // Avança com a bola
        Support,    // Posiciona para receber
        Pass,       // Tenta passar
        Shoot,      // Tenta chutar
        Return,     // Volta à posição de formação
        Pressure,   // Pressão alta sobre o portador
        Cover       // Cobre o espaço atrás do marcador
    }
}
