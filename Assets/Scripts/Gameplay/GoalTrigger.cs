using UnityEngine;

namespace CampoDeTerraFC.Gameplay
{
    /// <summary>
    /// Trigger que detecta quando a bola entra no gol e notifica o MatchController.
    /// </summary>
    public class GoalTrigger : MonoBehaviour
    {
        [Tooltip("0 = Gol do time B (bola entrou no gol A), 1 = Gol do time A (bola entrou no gol B)")]
        public int GoalIndex;

        private MatchController _matchController;
        private bool _cooldown;

        private void Start()
        {
            _matchController = FindObjectOfType<MatchController>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_cooldown) return;
            if (!other.CompareTag("Ball")) return;

            _cooldown = true;
            Invoke(nameof(ResetCooldown), 3f);

            // Determina quem marcou (bola entrou no gol GoalIndex → time adversário marcou)
            int scoringTeam = GoalIndex == 0 ? 1 : 0;

            Debug.Log($"[GoalTrigger] GOL! Bola no gol {GoalIndex} → Time {scoringTeam} marcou!");

            // Notifica via BallController
            var ball = other.GetComponent<Ball.BallController>();
            ball?.RaiseGoal(GoalIndex);

            // Notifica o MatchController
            _matchController?.RegisterGoal(scoringTeam);
        }

        private void ResetCooldown() => _cooldown = false;
    }
}
