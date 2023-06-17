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
    [System.Serializable]
    public class PlayerStatDict : SerializableDictionaryBase<ePlayerType, PlayerStat> {}
    [CreateAssetMenu(menuName = "Scriptable Objects/PlayerConfig", fileName = "Player Config")]
    public class PlayerConfig : ScriptableObject
    {
        public ePlayerType InitPlayerType;
        public PlayerStatDict Stats;
    }
    [System.Serializable]
    public class PlayerStat
    {
        public RuntimeAnimatorController AnimatorController;
        public float BaseSpeed = 10;
        public float SpeedScaleSprint = 1;
    }
}
