using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class PlayerGunHolder : MonoBehaviour
    {
        public ePlayerType Type;
        public virtual void Setup(ePlayerType type)
        {
            Type = type;
        }
        public virtual void Flip(bool isFacingLeft) {}
    }
}
