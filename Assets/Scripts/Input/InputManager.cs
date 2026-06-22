using System;
using UnityEngine;

namespace CampoDeTerraFC.Input
{
    /// <summary>
    /// Gerencia o input do jogador.
    /// FUSÃO: Base do Unity2022 (Legacy Input, sem conflito de package)
    ///        + Interface de eventos completa do Sprint1 (OnShootReleased com charge, OnSwitchPlayer, etc.)
    ///        + Sistema de carregamento de força de chute do Sprint1.
    /// </summary>
    public sealed class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        // ===== EVENTOS (Sprint1 interface) =====
        public event Action<Vector2> OnMoveInput;
        public event Action<bool>    OnSprintInput;
        public event Action          OnShortPassPressed;
        public event Action          OnLongPassPressed;
        public event Action          OnThroughBallPressed;
        public event Action          OnShootStarted;
        public event Action<float>   OnShootReleased;  // float = charge 0-1
        public event Action          OnDribblePressed;
        public event Action          OnSlidePressed;
        public event Action          OnSwitchPlayerPressed;
        public event Action          OnPausePressed;

        // ===== ESTADO PÚBLICO =====
        public Vector2 MoveDirection  { get; private set; }
        public bool    IsSprinting    { get; private set; }
        public float   ShootCharge    { get; private set; }
        public bool    IsInputEnabled { get; private set; } = true;

        // ===== ESTADO INTERNO =====
        private bool  _chargingShoot;
        private float _chargeStart;
        private bool  _prevSprint;
        private const float MAX_CHARGE = 1.5f; // segundos para carga máxima

        // ===== MAPEAMENTO DE TECLAS =====
        // Facilita reconfiguração futura sem alteração de lógica
        private KeyCode _keyShortPass   = KeyCode.Q;
        private KeyCode _keyLongPass    = KeyCode.E;
        private KeyCode _keyThroughBall = KeyCode.R;
        private KeyCode _keyShoot       = KeyCode.F;
        private KeyCode _keyDribble     = KeyCode.C;
        private KeyCode _keySlide       = KeyCode.X;
        private KeyCode _keySwitchPlayer= KeyCode.Tab;
        private KeyCode _keyPause       = KeyCode.Escape;
        private KeyCode _keySprint      = KeyCode.LeftShift;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Chamado pelo GameManager após a inicialização.</summary>
        public void Initialize() { Debug.Log("[InputManager] Inicializado (Legacy Input)."); }

        public void SetInputEnabled(bool enabled) { IsInputEnabled = enabled; }

        private void Update()
        {
            if (!IsInputEnabled) { ResetState(); return; }

            ReadMovement();
            ReadSprint();
            ReadActions();
            ReadShoot();
        }

        private void ReadMovement()
        {
            float h = UnityEngine.Input.GetAxisRaw("Horizontal");
            float v = UnityEngine.Input.GetAxisRaw("Vertical");
            Vector2 dir = new Vector2(h, v);
            if (dir.magnitude > 1f) dir.Normalize();
            MoveDirection = dir;
            OnMoveInput?.Invoke(dir);
        }

        private void ReadSprint()
        {
            bool sprint = UnityEngine.Input.GetKey(_keySprint);
            if (sprint != _prevSprint)
            {
                _prevSprint = sprint;
                IsSprinting = sprint;
                OnSprintInput?.Invoke(sprint);
            }
        }

        private void ReadActions()
        {
            if (UnityEngine.Input.GetKeyDown(_keyShortPass))    OnShortPassPressed?.Invoke();
            if (UnityEngine.Input.GetKeyDown(_keyLongPass))     OnLongPassPressed?.Invoke();
            if (UnityEngine.Input.GetKeyDown(_keyThroughBall))  OnThroughBallPressed?.Invoke();
            if (UnityEngine.Input.GetKeyDown(_keyDribble))      OnDribblePressed?.Invoke();
            if (UnityEngine.Input.GetKeyDown(_keySlide))        OnSlidePressed?.Invoke();
            if (UnityEngine.Input.GetKeyDown(_keySwitchPlayer)) OnSwitchPlayerPressed?.Invoke();
            if (UnityEngine.Input.GetKeyDown(_keyPause))        OnPausePressed?.Invoke();
        }

        private void ReadShoot()
        {
            if (UnityEngine.Input.GetKeyDown(_keyShoot))
            {
                _chargingShoot = true;
                _chargeStart   = Time.time;
                ShootCharge    = 0f;
                OnShootStarted?.Invoke();
            }

            if (_chargingShoot)
                ShootCharge = Mathf.Clamp01((Time.time - _chargeStart) / MAX_CHARGE);

            if (UnityEngine.Input.GetKeyUp(_keyShoot) && _chargingShoot)
            {
                _chargingShoot = false;
                float charge = ShootCharge;
                ShootCharge = 0f;
                OnShootReleased?.Invoke(charge);
            }
        }

        private void ResetState()
        {
            if (MoveDirection != Vector2.zero) { MoveDirection = Vector2.zero; OnMoveInput?.Invoke(Vector2.zero); }
        }
    }
}
