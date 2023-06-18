using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class Gun : MonoBehaviour
    {
        [SerializeField] protected Bullet BulletPrefab;
        protected Transform[] shootingPoints;
        protected GunConfig config;
        protected float startShootTime;
        protected bool isShooting;
        protected Vector2 mousePosition;
        public virtual void Setup(GunConfig gunConfig, params Transform[] shootingPoints) 
        { 
            this.config = gunConfig; 
            this.shootingPoints = shootingPoints;
        }
        public virtual void Shoot(Vector2 mousePos) 
        { 
            this.isShooting = true; 
            this.mousePosition = mousePos;
        }
        public virtual void StopShooting() { this.isShooting = false; }
    }
}
