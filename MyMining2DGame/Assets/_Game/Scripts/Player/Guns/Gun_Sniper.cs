using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class Gun_Sniper : Gun
    {
        float baseSpreadRadius;
        float stableSpreadRadius;
        float stableTime;
        Transform shootingPoint;
        float timer;
        float horizontalDistance;
        float spread;
        float minDistance;
        float maxDistance;
        float speed;
        int rateOfFire;
        float timeBetweenShotValue;
        float timeBetweenShot;
        bool alreadyShot = false;
        public override void Setup(GunConfig gunConfig, params Transform[] shootingPoints)
        {
            base.Setup(gunConfig);
            this.baseSpreadRadius = this.config.BaseSpread;
            this.stableTime = this.config.StableTime;
            this.shootingPoint = shootingPoints[0];
            this.minDistance = this.config.MinShootingDistance;
            this.maxDistance = this.config.MaxShootingDistance;
            this.stableSpreadRadius = this.config.StableSpread;
            this.rateOfFire = this.config.RateOfFire;
            this.timeBetweenShotValue = 1f / (float)this.rateOfFire;
            this.timer = this.timeBetweenShotValue;
        }
        private void Update() 
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
            }
            else
            {
                timer = this.timeBetweenShotValue;
                this.alreadyShot = false;
            }
            if (this.alreadyShot)
                return;
            if (!this.isShooting)
            {
                this.horizontalDistance = 0;
                this.spread = this.baseSpreadRadius;
                return;
            }    
            // * Opt1: Get bullet from pool

            // * Opt2: Instantiate bullet
            var bulletInstance = Instantiate(BulletPrefab, this.shootingPoint.position, Quaternion.identity);
            bulletInstance.gameObject.SetActive(false);
            this.horizontalDistance = this.maxDistance;

            // * Get distance and speed and launch bullet
            var shootDirection = this.mousePosition - (Vector2)this.shootingPoint.position;
            var centerShoot = GetPointB(this.shootingPoint.position, this.horizontalDistance, shootDirection);
            var endPoint = centerShoot;

            var finalDirection = endPoint - (Vector2)this.shootingPoint.position;
            bulletInstance.gameObject.SetActive(true);

            this.speed = Random.Range(this.config.MinBulletSpeed, this.config.MaxBulletSpeed);
            bulletInstance.Launch(finalDirection, endPoint, this.speed);

            this.alreadyShot = true;
        }
        public Vector2 GetPointB(Vector2 pointA, float lengthAB, Vector2 direction)
        {
            Vector2 normalizedDirection = direction.normalized;
            Vector2 pointB = pointA + normalizedDirection * lengthAB;

            return pointB;
        }
    }
}
