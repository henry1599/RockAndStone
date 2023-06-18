using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    [CreateAssetMenu(menuName = "Scriptable Objects/GunConfig", fileName = "Gun Config")]
    public class GunConfig : ScriptableObject
    {
        // * How many bullets per second
        public int RateOfFire;
        // * How much damage per bullet
        public float Damage;
        // * How many bullet this gun has
        public int MaxAmmo;
        // * Bullets per mag
        public int Magazine;
        // * How many bullets shot per time
        public int BulletPerShot;
        public float MinShootingDistance;
        public float MaxShootingDistance;
        public float BaseSpread;
        public float StableSpread;
    }
}
