using UnityEngine;
using UnityEngine.AI;

namespace CampoDeTerraFC.AI
{
    /// <summary>
    /// IA simples para jogadores de campo. Funciona sem NavMesh (usa movimento direto).
    /// Persegue a bola quando não tem posse; avança e chuta quando tem.
    /// </summary>
    [RequireComponent(typeof(Player.PlayerController))]
    public class SimpleFieldAI : MonoBehaviour
    {
        private Player.PlayerController _pc;
        private Ball.BallController _ball;
        private int _teamIndex = 0;

        [SerializeField] private float _decisionInterval = 0.2f;
        private float _decisionTimer;

        private enum AIState { Chase, WithBall, Defend, Idle }
        private AIState _state = AIState.Chase;

        private Transform _opponentGoal;
        private Transform _ownGoal;

        private float _shootRangeSq = 324f; // 18² metros

        public void SetTeamIndex(int idx) => _teamIndex = idx;

        private void Awake()
        {
            _pc = GetComponent<Player.PlayerController>();
        }

        private void Start()
        {
            _ball = FindObjectOfType<Ball.BallController>();
            FindGoals();
            // Aleatoriza timer inicial para evitar que todas as IAs decidam no mesmo frame
            _decisionTimer = Random.Range(0f, _decisionInterval);
        }

        private void FindGoals()
        {
            var goals = FindObjectsOfType<Gameplay.GoalTrigger>();
            foreach (var g in goals)
            {
                if (g.GoalIndex == _teamIndex) _ownGoal = g.transform;
                else _opponentGoal = g.transform;
            }
        }

        private void Update()
        {
            if (_ball == null) return;

            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer > 0f) return;
            _decisionTimer = _decisionInterval + Random.Range(0f, 0.05f);

            Decide();
        }

        private void FixedUpdate()
        {
            Execute();
        }

        private void Decide()
        {
            bool hasBall = _pc.HasBall;
            bool teamHasBall = _ball.AttachedPlayer != null && _ball.AttachedPlayer.TeamIndex == _teamIndex;

            if (hasBall)
            {
                _state = AIState.WithBall;
            }
            else if (teamHasBall)
            {
                // Vai para posição de apoio
                _state = AIState.Idle;
            }
            else
            {
                float distToBall = Vector3.Distance(transform.position, _ball.transform.position);
                _state = distToBall < 12f ? AIState.Chase : AIState.Defend;
            }
        }

        private void Execute()
        {
            switch (_state)
            {
                case AIState.Chase:
                    MoveToward(_ball.transform.position);
                    break;

                case AIState.WithBall:
                    ExecuteWithBall();
                    break;

                case AIState.Defend:
                    if (_ownGoal != null)
                        MoveToward(Vector3.Lerp(transform.position, _ownGoal.position, 0.3f));
                    break;

                case AIState.Idle:
                    // Para levemente mas mantém posição
                    _pc.AI_SetMoveDirection(Vector2.zero);
                    break;
            }
        }

        private void ExecuteWithBall()
        {
            if (_opponentGoal == null) return;

            Vector3 toGoal = _opponentGoal.position - transform.position;
            float distSq = toGoal.sqrMagnitude;

            if (distSq < _shootRangeSq)
            {
                // Chuta!
                float charge = Mathf.Lerp(0.5f, 1f, 1f - (distSq / _shootRangeSq));
                // Pequeno delay para não chutar instantaneamente
                if (Random.value < 0.1f)
                    _pc.AI_Shoot(charge);
            }
            else
            {
                // Avança com a bola
                MoveToward(_opponentGoal.position);
            }
        }

        private void MoveToward(Vector3 target)
        {
            Vector3 dir = (target - transform.position);
            dir.y = 0f;
            if (dir.magnitude < 0.5f) { _pc.AI_SetMoveDirection(Vector2.zero); return; }
            dir.Normalize();
            _pc.AI_SetMoveDirection(new Vector2(dir.x, dir.z));
        }
    }
}
