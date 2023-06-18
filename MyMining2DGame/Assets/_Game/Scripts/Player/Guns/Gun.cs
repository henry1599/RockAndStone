using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class Gun : MonoBehaviour
    {
        [SerializeField] protected Bullet BulletPrefab;
        protected GunConfig config;
        protected float startShootTime;
        protected bool isShooting;
        public virtual void Setup(GunConfig gunConfig) { this.config = gunConfig; }
        public virtual void Shoot() { this.isShooting = true; }
        public virtual void StopShooting() { this.isShooting = false; }
    }
}
