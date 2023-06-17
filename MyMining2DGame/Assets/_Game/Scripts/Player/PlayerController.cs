using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System;

namespace DinoMining
{
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        public PlayerConfig Config;
        
        #region Internal
        private PlayerInput input;
        private FrameInput frameInput;
        private float baseSpeed;
        private float speedScaleSprint;
        [SerializeField, Foldout("Status")] private bool canInteract;
        [SerializeField, Foldout("Status")] private bool canMine;
        [SerializeField, Foldout("Status")] private bool canShoot;
        [SerializeField, Foldout("Status")] private bool canSprint;
        #endregion


        #region External
        public ePlayerType CurrentType 
        {
            get => this.currentType;
            set
            {
                this.currentType = value;
                OnTypeChanged(value);
            }
        } [SerializeField] ePlayerType currentType;
        [ShowNativeProperty] public Vector2 Input => this.frameInput.Move;
        [ShowNativeProperty] public float BaseSpeed => this.baseSpeed;
        [ShowNativeProperty] public float SpeedScaleSprint => this.speedScaleSprint;
        public event Action<bool> OnMove;
        public event Action<bool> OnSprint;
        public event Action OnMine;
        public event Action OnInteract;
        public event Action OnShoot;
        public event Action<PlayerStat> OnPlayerTypeChanged;
        #endregion
        protected virtual void Awake()
        {
            input = GetComponent<PlayerInput>();
        }
        protected virtual void Start()
        {
            GatherStats();
        }
        protected virtual void Update()
        {
            GatherInput();

            HandleMove();
            HandleShoot();
            HandleInteract();
            HandleMine();
        }
        protected virtual void GatherStats()
        {
            CurrentType = Config.InitPlayerType;
            GatherPlayerStatByType(CurrentType);
        }
        protected virtual void GatherPlayerStatByType(ePlayerType type)
        {
            this.baseSpeed = Config.Stats[type].BaseSpeed;
            this.speedScaleSprint = Config.Stats[type].SpeedScaleSprint;
        }
        protected virtual void GatherInput()
        {
            this.frameInput = this.input.FrameInput;

            this.canInteract = this.frameInput.Interact;
            this.canMine = this.frameInput.Mine;
            this.canShoot = this.frameInput.Shoot;
            this.canSprint = this.frameInput.Sprint;
        }
        private void OnTypeChanged(ePlayerType newType)
        {
            OnPlayerTypeChanged?.Invoke(Config.Stats[newType]);
            GatherPlayerStatByType(newType);
        }
        protected virtual void HandleMove()
        {
            Vector2 moveDirection = Input.normalized;

            bool isMoving = moveDirection.x != 0 || moveDirection.y != 0;
            bool isSprint = this.canSprint && isMoving;

            Vector2 moveDistance = moveDirection * Time.deltaTime * this.baseSpeed * (isSprint ? this.speedScaleSprint : 1);
            transform.position += (Vector3)moveDistance;


            OnMove?.Invoke(isMoving);
            OnSprint?.Invoke(isSprint);
        }
        protected virtual void HandleInteract()
        {
            if (!this.canInteract)
                return;
            OnInteract?.Invoke();
        }
        protected virtual void HandleMine()
        {
            if (!this.canMine)
                return;
            OnMine?.Invoke();
        }
        protected virtual void HandleShoot()
        {
            if (!this.canShoot)
                return;
            OnShoot?.Invoke();
        }
    }
    public interface IPlayerController
    {
        #region Use for animator
        public event System.Action<PlayerStat> OnPlayerTypeChanged;
        public event System.Action<bool> OnMove;
        public event System.Action<bool> OnSprint;
        public event System.Action OnMine;
        public event System.Action OnInteract;
        public event System.Action OnShoot;
        #endregion
    }
}
