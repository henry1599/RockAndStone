using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class Gun_LMG : Gun
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
        public override void Setup(GunConfig gunConfig, params Transform[] shootingPoints)
        {
            base.Setup(gunConfig);
            this.baseSpreadRadius = this.config.BaseSpread;
            this.stableTime = this.config.StableTime;
            this.shootingPoint = shootingPoints[0];
            this.minDistance = this.config.MinShootingDistance;
            this.maxDistance = this.config.MaxShootingDistance;
            this.stableSpreadRadius = this.config.StableSpread;
            this.speed = this.config.BulletSpeed;
            this.rateOfFire = this.config.RateOfFire;
            this.timeBetweenShotValue = 1f / (float)this.rateOfFire;
            this.timeBetweenShot = this.timeBetweenShotValue;
        }
        private void Update() 
        {
            if (!this.isShooting)
            {
                this.startShootTime = 0;
                this.timeBetweenShot = this.timeBetweenShotValue;
                this.timer = 0;
                this.horizontalDistance = 0;
                this.spread = this.baseSpreadRadius;
                return;
            }    
            if (this.timeBetweenShot > 0)
            {
                this.timeBetweenShot -= Time.deltaTime;
                return;
            }
            this.timeBetweenShot = this.timeBetweenShotValue;
            // * Opt1: Get bullet from pool

            // * Opt2: Instantiate bullet
            var bulletInstance = Instantiate(BulletPrefab, this.shootingPoint.position, Quaternion.identity);
            bulletInstance.gameObject.SetActive(false);

            // * Calculating shooting
            if (timer < this.stableTime)
            {
                timer += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(timer / this.stableTime);

                this.spread = Mathf.Lerp(this.baseSpreadRadius, this.stableSpreadRadius, normalizedTime);
                this.horizontalDistance = Mathf.Lerp(this.minDistance, this.maxDistance, normalizedTime);
            }
            else
            {
                this.spread = this.stableSpreadRadius;
                this.horizontalDistance = this.maxDistance;
            }

            // * Get distance and speed and launch bullet
            var shootDirection = this.mousePosition - (Vector2)this.shootingPoint.position;
            var centerShoot = GetPointB(this.shootingPoint.position, this.horizontalDistance, shootDirection);
            var endPoint = GetRandomPointOnCircle(centerShoot, this.spread);

            var finalDirection = endPoint - (Vector2)this.shootingPoint.position;
            bulletInstance.gameObject.SetActive(true);
            bulletInstance.Launch(finalDirection, endPoint, this.speed);
        }
        public Vector2 GetRandomPointOnCircle(Vector2 center, float radius)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float x = center.x + radius * Mathf.Cos(angle);
            float y = center.y + radius * Mathf.Sin(angle);

            return new Vector2(x, y);
        }
        public Vector2 GetPointB(Vector2 pointA, float lengthAB, Vector2 direction)
        {
            Vector2 normalizedDirection = direction.normalized;
            Vector2 pointB = pointA + normalizedDirection * lengthAB;

            return pointB;
        }
    }
}
