using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class PlayerGunHolderBlue : PlayerGunHolder
    {
        public SpriteRenderer GunRenderer;
        public Transform GunGraphic;
        public Transform ShootPoint;
        [SerializeField] protected int leftSortingOrder;
        [SerializeField] protected int rightSortingOrder;
        public override void Flip(bool isFacingLeft)
        {
            var scale = GunGraphic.localScale;
            int sortingOrder = isFacingLeft ? this.leftSortingOrder : this.rightSortingOrder;
            GunRenderer.sortingOrder = sortingOrder;
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
            GunGraphic.localScale = scale;
        }
    }
}
