using System.Collections;
using UnityEngine;

namespace CampoDeTerraFC.Goalkeeper
{
    /// <summary>
    /// IA do goleiro. Rastreia a bola lateralmente na linha do gol e mergulha quando necessário.
    /// </summary>
    public class SimpleGoalkeeperAI : MonoBehaviour
    {
        private Rigidbody _rb;
        private Ball.BallController _ball;

        [Header("Configuração")]
        [SerializeField] private float _maxSpeed = 5f;
        [SerializeField] private float _diveForce = 10f;
        [SerializeField] private float _halfWidth = 3.5f;   // Metade da largura do gol
        [SerializeField] private float _lineOffset = 0.5f;  // Distância da linha do gol

        private int _teamIndex = 0;
        private bool _isDiving;
        private Vector3 _goalLineCenter;
        private Vector3 _currentVel;

        public void SetTeamIndex(int idx) => _teamIndex = idx;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        private void Start()
        {
            _ball = FindObjectOfType<Ball.BallController>();
            _goalLineCenter = transform.position;
            _rb.freezeRotation = true;
        }

        private void FixedUpdate()
        {
            if (_ball == null || _isDiving) return;

            // Rastreia horizontalmente
            float targetX = Mathf.Clamp(_ball.transform.position.x, -_halfWidth, _halfWidth);
            Vector3 target = new Vector3(targetX, transform.position.y, _goalLineCenter.z);

            Vector3 vel = (target - transform.position).normalized * _maxSpeed;
            vel.y = _rb.velocity.y;
            _rb.velocity = Vector3.SmoothDamp(_rb.velocity, vel, ref _currentVel, 0.1f);

            // Rotaciona para encarar a bola
            Vector3 lookDir = (_ball.transform.position - transform.position);
            lookDir.y = 0f;
            if (lookDir.magnitude > 0.1f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.fixedDeltaTime * 8f);

            // Decide mergulhar
            if (ShouldDive()) StartCoroutine(DiveRoutine());
        }

        private bool ShouldDive()
        {
            if (_ball.CurrentSpeed < 5f) return false;
            Vector3 vel = _ball.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero;
            float distToMe = Vector3.Distance(transform.position, _ball.transform.position);
            // Bola vindo em direção ao gol rápido e próxima
            bool comingThisWay = Vector3.Dot(vel.normalized, (transform.position - _ball.transform.position).normalized) > 0.5f;
            return comingThisWay && distToMe < 8f && Random.value < 0.02f;
        }

        private IEnumerator DiveRoutine()
        {
            _isDiving = true;

            Vector3 diveTo = new Vector3(_ball.transform.position.x, transform.position.y, transform.position.z);
            Vector3 diveDir = (diveTo - transform.position).normalized;
            diveDir.z = 0f;
            if (diveDir.magnitude < 0.1f) diveDir = Vector3.right;

            _rb.AddForce(diveDir * _diveForce + Vector3.up * 3f, ForceMode.Impulse);

            yield return new WaitForSeconds(0.7f);

            // Recupera
            _isDiving = false;
            yield return new WaitForSeconds(0.5f);

            // Volta para o centro
            float t = 0f;
            Vector3 start = transform.position;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                transform.position = Vector3.Lerp(start, _goalLineCenter, t);
                yield return null;
            }
        }

        private void OnCollisionEnter(Collision c)
        {
            if (!c.gameObject.CompareTag("Ball")) return;
            var ball = c.gameObject.GetComponent<Ball.BallController>();
            if (ball == null) return;

            // Defende — rebate a bola para o lado
            Vector3 rebate = (c.contacts[0].point - _goalLineCenter).normalized;
            rebate.z = _teamIndex == 1 ? -1f : 1f;
            rebate.y = 0.4f;
            rebate.Normalize();
            ball.ApplyShoot(rebate, 12f, 0f);
            Debug.Log("[Goalkeeper] DEFESA!");
        }
    }
}
