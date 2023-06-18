using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class PlayerGunHolderYellow : PlayerGunHolder
    {
        public SpriteRenderer GunLRenderer;
        public SpriteRenderer GunRRenderer;
        public Transform GunLGraphic; 
        public Transform GunRGraphic; 
        public Transform ShootPointL;
        public Transform ShootPointR;
        [SerializeField] protected int leftSortingOrderGunL;
        [SerializeField] protected int rightSortingOrderGunL;
        [SerializeField] protected int leftSortingOrderGunR;
        [SerializeField] protected int rightSortingOrderGunR;
        public override void Setup(ePlayerType playerType, eGunType gunType, GunConfig gunConfig)
        {
            base.Setup(playerType, gunType, gunConfig);
            ThisGun.Setup(gunConfig, ShootPointL, ShootPointR);
        }
        public override void Flip(bool isFacingLeft)
        {
            var scale = GunLGraphic.localScale;
            int sortingOrderGunL = isFacingLeft ? this.leftSortingOrderGunL : this.rightSortingOrderGunL;
            int sortingOrderGunR = isFacingLeft ? this.leftSortingOrderGunR : this.rightSortingOrderGunR;
            GunLRenderer.sortingOrder = sortingOrderGunL;
            GunRRenderer.sortingOrder = sortingOrderGunR;
            if (isFacingLeft)
            {
                if (scale.y < 0)
                    return;
                scale.y *= -1;
            }
            else
            {
                if (scale.y > 0)
                    return;
                scale.y *= -1;
            }
            GunLGraphic.localScale = scale;
            GunRGraphic.localScale = scale;
        }
    }
}
