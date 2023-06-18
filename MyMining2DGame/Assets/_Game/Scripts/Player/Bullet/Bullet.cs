using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] protected Rigidbody2D rb2D;
        public virtual void Setup() {}
    }
}
