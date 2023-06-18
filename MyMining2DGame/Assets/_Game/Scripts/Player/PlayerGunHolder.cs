using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class PlayerGunHolder : MonoBehaviour
    {
        public ePlayerType PlayerType;
        public eGunType GunType;
        public Gun ThisGun;
        public virtual void Setup(ePlayerType playerType, eGunType gunType, GunConfig gunConfig)
        {
            PlayerType = playerType;
            GunType = gunType;
            ThisGun.Setup(gunConfig);
        }
        public virtual void Shoot() { ThisGun.Shoot(); }
        public virtual void StopShooting() { ThisGun.StopShooting(); }
        public virtual void Flip(bool isFacingLeft) {}
    }
}
