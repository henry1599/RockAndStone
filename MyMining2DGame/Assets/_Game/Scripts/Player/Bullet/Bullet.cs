using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] protected Rigidbody2D rb2D;
        Vector2 endPoint;
        void Update()
        {
            var distanceToEndPoint = (transform.position.x - endPoint.x) * (transform.position.x - endPoint.x) + (transform.position.y - endPoint.y) * (transform.position.y - endPoint.y);
            if (distanceToEndPoint <= 0.1f)
                Destroy(gameObject);
        }
        public virtual void Setup() {}
        void RotateTowardsDirection(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        public virtual void Launch(Vector2 direction, Vector2 endPoint, float speed)
        {
            RotateTowardsDirection(direction);
            this.endPoint = endPoint;
            this.rb2D.velocity = direction.normalized * speed;
        }
    }
}
