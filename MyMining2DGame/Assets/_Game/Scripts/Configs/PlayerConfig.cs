using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using RotaryHeart.Lib.SerializableDictionary;

namespace DinoMining
{
    public enum ePlayerType
    {
        Green,
        Blue,
        Red,
        Yellow
    }
    public enum eGunType
    {
        Sniper,
        SMG,
        LMG,
        AR
    }
    [System.Serializable]
    public class PlayerStatDict : SerializableDictionaryBase<ePlayerType, PlayerStat> {}
    [CreateAssetMenu(menuName = "Scriptable Objects/PlayerConfig", fileName = "Player Config")]
    public class PlayerConfig : ScriptableObject
    {
        public ePlayerType InitPlayerType;
        public PlayerStatDict Stats;
        public PlayerGunHolder LoadGunHolder(ePlayerType type)
        {
            string path = Stats[type].GunHolderPath;
            GameObject result = Resources.Load(path) as GameObject;
            return result.GetComponent<PlayerGunHolder>();
        }
    }
    [System.Serializable]
    public class PlayerStat
    {
        public eGunType PrimaryWeaponType;
        public GunConfig GunStat;
        public RuntimeAnimatorController AnimatorController;
        public float BaseSpeed = 10;
        public float SpeedScaleSprint = 1;
        public string GunHolderPath;
        public Vector3 GunHolderPosition;
    }
}
