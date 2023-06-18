using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] protected Rigidbody rb;
        public virtual void Setup() {}
    }
}
