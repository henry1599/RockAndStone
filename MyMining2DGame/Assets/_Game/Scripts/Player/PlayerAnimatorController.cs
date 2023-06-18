using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class PlayerAnimatorController : MonoBehaviour
    {
        private static readonly int MoveKeyAnim = Animator.StringToHash("isMoving");
        private static readonly int SprintKeyAnim = Animator.StringToHash("isSprint");
        private static readonly int MineKeyAnim = Animator.StringToHash("mine");
        private static readonly int HurtKeyAnim = Animator.StringToHash("hurt");
        public Animator GraphicAnimator;
        private RuntimeAnimatorController currentAnimator;
        private IPlayerController player;
        private bool isAnimGathered;
        protected virtual void Awake()
        {
            player = GetComponent<IPlayerController>();
            this.isAnimGathered = false;

            if (player == null)
                return;
            this.player.OnPlayerTypeChanged += HandlePlayerTypeChanged;
            this.player.OnMove += HandleMove;
            this.player.OnMine += HandleMine;
            this.player.OnInteract += HandleInteract;
            this.player.OnShoot += HandleShoot;
            this.player.OnSprint += HandleSprint;
        }
        protected virtual void OnDestroy()
        {
            this.player.OnPlayerTypeChanged -= HandlePlayerTypeChanged;
            this.player.OnMove -= HandleMove;
            this.player.OnMine -= HandleMine;
            this.player.OnInteract -= HandleInteract;
            this.player.OnShoot -= HandleShoot;
            this.player.OnSprint -= HandleSprint;
        }

        private void HandleMove(bool isMove)
        {
            GraphicAnimator.SetBool(MoveKeyAnim, isMove);
        }

        private void HandleMine()
        {
            GraphicAnimator.SetTrigger(MineKeyAnim);
        }

        private void HandleInteract()
        {
        }

        private void HandleShoot()
        {
        }

        private void HandleSprint(bool isSprint)
        {
            GraphicAnimator.SetBool(SprintKeyAnim, isSprint);
        }

        private void HandlePlayerTypeChanged(PlayerStat stat)
        {
            this.currentAnimator = stat.AnimatorController;
            GraphicAnimator.runtimeAnimatorController = this.currentAnimator;
            this.isAnimGathered = true;
        }
    }
}
