using System;
using UnityEngine;
using CampoDeTerraFC.Ball;
using CampoDeTerraFC.Core.Events;

namespace CampoDeTerraFC.Gameplay
{
    /// <summary>
    /// Motor de regras da partida: gol, escanteio, lateral, tiro de meta, falta.
    /// FUSÃO: Lógica completa do Sprint1 + referências diretas do Unity2022.
    /// </summary>
    public sealed class RulesEngine : MonoBehaviour
    {
        [Header("Dimensões do Campo")]
        [SerializeField] private float _fieldHalfX = 25f;
        [SerializeField] private float _fieldHalfZ = 35f;

        [Header("Referências")]
        [SerializeField] private BallController _ball;

        [Header("Eventos")]
        [SerializeField] private GameEventSO _onCornerKick;
        [SerializeField] private GameEventSO _onThrowIn;
        [SerializeField] private GameEventSO _onGoalKick;
        [SerializeField] private FoulEventSO _onFoul;

        // ===== ESTADO =====
        public bool IsActive { get; set; } = true;
        private int _lastTouchTeam = -1;
        private bool _foulEnabled = true;
        private bool _offsideEnabled = false;

        // ===== EVENTOS LOCAIS =====
        public event Action<SetPieceType, Vector3, int> OnSetPiece;

        private void Start()
        {
            _ball = _ball ?? FindObjectOfType<BallController>();
        }

        private void Update()
        {
            if (!IsActive || _ball == null) return;
            CheckBallOutOfBounds();
        }

        public void RegisterTouch(int teamIndex) { _lastTouchTeam = teamIndex; }

        private void CheckBallOutOfBounds()
        {
            Vector3 p = _ball.transform.position;
            bool outL  = p.x < -_fieldHalfX;
            bool outR  = p.x >  _fieldHalfX;
            bool outFrontB = p.z >  _fieldHalfZ;
            bool outFrontA = p.z < -_fieldHalfZ;

            if (outL || outR)
            {
                Vector3 pos = new Vector3(Mathf.Clamp(p.x, -_fieldHalfX, _fieldHalfX), 0f, p.z);
                int cobrador = _lastTouchTeam == 0 ? 1 : 0;
                HandleSetPiece(SetPieceType.ThrowIn, pos, cobrador);
                _onThrowIn?.Raise();
            }
            else if (outFrontB)
            {
                if (_lastTouchTeam == 0) { HandleSetPiece(SetPieceType.GoalKick, new Vector3(0f,0f,_fieldHalfZ-5f), 1); _onGoalKick?.Raise(); }
                else                     { HandleSetPiece(SetPieceType.Corner, new Vector3(p.x<0?-_fieldHalfX:_fieldHalfX,0f,_fieldHalfZ), 0); _onCornerKick?.Raise(); }
            }
            else if (outFrontA)
            {
                if (_lastTouchTeam == 1) { HandleSetPiece(SetPieceType.GoalKick, new Vector3(0f,0f,-_fieldHalfZ+5f), 0); _onGoalKick?.Raise(); }
                else                     { HandleSetPiece(SetPieceType.Corner, new Vector3(p.x<0?-_fieldHalfX:_fieldHalfX,0f,-_fieldHalfZ), 1); _onCornerKick?.Raise(); }
            }
        }

        public void TryRegisterFoul(int offenderTeam, Vector3 position, Player.PlayerController target)
        {
            if (!_foulEnabled || !IsActive) return;
            if (UnityEngine.Random.value > 0.7f) return;

            bool isPenalty = IsInsidePenaltyArea(position, offenderTeam);
            _onFoul?.Raise(new FoulEventData { CommittingTeamIndex = offenderTeam, FoulPosition = position, IsPenalty = isPenalty });

            int cobrador = offenderTeam == 0 ? 1 : 0;
            HandleSetPiece(isPenalty ? SetPieceType.Penalty : SetPieceType.FreeKick, position, cobrador);
        }

        private void HandleSetPiece(SetPieceType type, Vector3 pos, int kickingTeam)
        {
            IsActive = false;
            if (_ball != null)
            {
                _ball.DetachFromPlayer();
                _ball.transform.position = pos + Vector3.up * 0.11f;
                var rb = _ball.GetComponent<Rigidbody>();
                if (rb) rb.velocity = Vector3.zero;
            }
            OnSetPiece?.Invoke(type, pos, kickingTeam);
            Debug.Log(string.Format("[RulesEngine] {0} para time {1} em {2}", type, kickingTeam, pos));
        }

        public void ResumeRules() { IsActive = true; }

        private bool IsInsidePenaltyArea(Vector3 pos, int defendingTeam)
        {
            float depth = 16.5f, halfW = 20.16f;
            if (defendingTeam == 0) return pos.z < -_fieldHalfZ + depth && Mathf.Abs(pos.x) < halfW;
            else                    return pos.z >  _fieldHalfZ - depth && Mathf.Abs(pos.x) < halfW;
        }

        public bool IsOffside(Player.PlayerController player, Vector3 passPos) { return false; }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f,1f,0f,0.1f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(_fieldHalfX*2f, 1f, _fieldHalfZ*2f));
        }
#endif
    }
}
