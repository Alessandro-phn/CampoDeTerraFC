using UnityEngine;
using CampoDeTerraFC.Player;

namespace CampoDeTerraFC.AI
{
    // ==========================================================================
    //  ATACANTE AI
    // ==========================================================================

    /// <summary>
    /// IA especializada para atacantes.
    /// Prioriza chutes ao gol, infiltrações e dribles.
    /// Fica posicionado na linha adversária aguardando oportunidades.
    /// </summary>
    public sealed class AttackerAI : AIAgent
    {
        private const float INFILTRATION_DEPTH = 0.75f; // Quão fundo infiltra (0-1)

        protected override void EvaluateTeamHasBall()
        {
            float distToGoal = DistanceToOpponentGoal();

            // Atacante infiltra quando o time tem a bola
            if (distToGoal > 10f)
            {
                ChangeState(AIState.Attack); // "Corre para a área"
            }
            else
            {
                ChangeState(AIState.Support); // Já está na área, espera o passe
            }
        }

        protected override void EvaluateOpponentHasBall(float distToBall)
        {
            // Atacante não precisa voltar muito para defender
            if (distToBall < 6f)
                ChangeState(AIState.Pressure); // Pressiona o zagueiro com a bola
            else
                ChangeState(AIState.Return);   // Volta levemente para meio-campo
        }

        protected override Vector3 GetSupportPosition()
        {
            // Posiciona na área adversária aguardando cruzamento
            if (_opponentGoal != null)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-4f, 4f),
                    0f,
                    -8f); // 8 metros à frente do gol

                return _opponentGoal.position + offset;
            }
            return FormationPosition;
        }

        protected override void ExecuteAttack()
        {
            if (_opponentGoal == null) return;

            // Infiltra em diagonal para a área
            Vector3 infiltrationTarget = new Vector3(
                _opponentGoal.position.x + UnityEngine.Random.Range(-5f, 5f),
                0f,
                _opponentGoal.position.z - 5f);

            MoveTo(infiltrationTarget);
        }
    }

    // ==========================================================================
    //  MEIA AI
    // ==========================================================================

    /// <summary>
    /// IA especializada para meias.
    /// Equilibra ataque e defesa, prioriza passes e distribuição de jogo.
    /// Ocupa o meio-campo e cria oportunidades para os atacantes.
    /// </summary>
    public sealed class MidfielderAI : AIAgent
    {
        protected override void EvaluateWithBall()
        {
            float distToGoal = DistanceToOpponentGoal();

            // Meia tenta passar antes de chutar (exceto se muito próximo)
            if (distToGoal > 12f && HasOpenTeammate())
            {
                ChangeState(AIState.Pass);
                return;
            }

            if (distToGoal < 20f && HasClearShotOnGoal())
            {
                ChangeState(AIState.Shoot);
                return;
            }

            ChangeState(AIState.Attack);
        }

        protected override void EvaluateTeamHasBall()
        {
            // Meia apoia próximo ao portador como opção de passe
            ChangeState(AIState.Support);
        }

        protected override void EvaluateOpponentHasBall(float distToBall)
        {
            if (distToBall < 8f)
                ChangeState(AIState.Mark);
            else if (IsNearestToBall())
                ChangeState(AIState.Chase);
            else
                ChangeState(AIState.Return);
        }

        protected override Vector3 GetSupportPosition()
        {
            // Mantém posição no meio-campo ligeiramente avançado
            if (_opponentGoal != null && _ownGoal != null)
            {
                Vector3 midpoint = (_opponentGoal.position + _ownGoal.position) * 0.5f;
                return new Vector3(
                    FormationPosition.x,
                    0f,
                    midpoint.z + 3f); // Levemente avançado do centro
            }
            return FormationPosition;
        }
    }

    // ==========================================================================
    //  DEFENSOR AI
    // ==========================================================================

    /// <summary>
    /// IA especializada para zagueiros e laterais.
    /// Prioriza marcação, bloqueios e interceptações.
    /// Raramente avança; mantém a linha defensiva.
    /// </summary>
    public sealed class DefenderAI : AIAgent
    {
        [Tooltip("Distância máxima que o defensor sai da posição (em unidades).")]
        [SerializeField] private float _maxAdvanceDistance = 15f;

        protected override void EvaluateWithBall()
        {
            // Defensor prefere passar rápido — raramente avança com a bola
            if (HasOpenTeammate())
            {
                ChangeState(AIState.Pass);
                return;
            }

            // Afasta a bola da própria área
            ChangeState(AIState.Attack);
        }

        protected override void EvaluateTeamHasBall()
        {
            // Defensor mantém a posição mesmo quando o time tem a bola
            ChangeState(AIState.Return);
        }

        protected override void EvaluateOpponentHasBall(float distToBall)
        {
            float distToOwnGoal = _ownGoal != null
                ? Vector3.Distance(transform.position, _ownGoal.position)
                : float.MaxValue;

            // Só persegue se estiver perto da bola E perto do próprio gol
            if (distToBall < 6f && distToOwnGoal < 20f)
            {
                ChangeState(AIState.Mark);
            }
            else
            {
                ChangeState(AIState.Return);
            }
        }

        protected override void ExecuteReturn()
        {
            // Defensor sempre volta para a posição de formação
            MoveTo(FormationPosition);
        }

        protected override Vector3 GetSupportPosition()
        {
            return FormationPosition; // Defensor não sai da posição
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Raio máximo de avanço
            Gizmos.color = new Color(0f, 0f, 1f, 0.15f);
            Gizmos.DrawWireSphere(FormationPosition, _maxAdvanceDistance);
        }
#endif
    }

    // ==========================================================================
    //  VOLANTE AI
    // ==========================================================================

    /// <summary>
    /// IA para volantes (meio-campo defensivo).
    /// Intercepta passes, dá cobertura aos zagueiros e distribui rapidamente.
    /// </summary>
    public sealed class DefensiveMidfielderAI : AIAgent
    {
        protected override void EvaluateWithBall()
        {
            // Volante distribui imediatamente — nunca dribla
            ChangeState(HasOpenTeammate() ? AIState.Pass : AIState.Attack);
        }

        protected override void EvaluateTeamHasBall()
        {
            // Ocupa espaço entre a defesa e o meia
            ChangeState(AIState.Support);
        }

        protected override void EvaluateOpponentHasBall(float distToBall)
        {
            if (distToBall < 10f)
                ChangeState(AIState.Mark);
            else if (IsNearestToBall())
                ChangeState(AIState.Chase);
            else
                ChangeState(AIState.Cover);
        }

        protected override void ExecuteCurrentState()
        {
            if (CurrentState == AIState.Cover)
            {
                ExecuteCover();
            }
            else
            {
                base.ExecuteCurrentState();
            }
        }

        /// <summary>
        /// Cobertura: posiciona entre a bola e o próprio gol para interceptar.
        /// </summary>
        private void ExecuteCover()
        {
            if (_ball == null || _ownGoal == null) return;

            Vector3 ballToGoal = _ownGoal.position - _ball.transform.position;
            Vector3 coverPosition = _ball.transform.position + ballToGoal.normalized * 5f;
            MoveTo(coverPosition);
        }
    }

    // ==========================================================================
    //  LATERAL AI
    // ==========================================================================

    /// <summary>
    /// IA para laterais (wing-backs).
    /// Sobe quando o time ataca e volta rápido para cobrir.
    /// Especialista em cruzamentos.
    /// </summary>
    public sealed class WingbackAI : AIAgent
    {
        [Tooltip("Lado do campo: -1 = esquerda, 1 = direita.")]
        [SerializeField] private int _side = -1;

        protected override void EvaluateTeamHasBall()
        {
            // Lateral sobe pela linha ao ataque
            ChangeState(AIState.Attack);
        }

        protected override void EvaluateOpponentHasBall(float distToBall)
        {
            if (distToBall < 8f)
                ChangeState(AIState.Mark);
            else
                ChangeState(AIState.Return);
        }

        protected override void ExecuteAttack()
        {
            if (_opponentGoal == null) return;

            // Sobe pela linha lateral
            Vector3 target = new Vector3(
                _opponentGoal.position.x + (_side * 8f), // Extremidade lateral
                0f,
                _opponentGoal.position.z - 10f);

            MoveTo(target);
        }
    }
}
