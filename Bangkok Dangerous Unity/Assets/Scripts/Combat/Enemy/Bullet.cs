using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float speed;
    private Vector3 direction;
    private int damage;

    public void InitializeBullet(Vector3 lastPos, float pSpeed, int pDamage)
    {
        speed = pSpeed;
        direction = lastPos - transform.position;
        direction.Normalize();
        direction.y = 0;
        damage = pDamage;

        if (speed <= 0)
        {
            speed = 10f;
        }
        transform.LookAt(lastPos);
    }

    private void Update()
    {
        MoveBullet();
    }

    private void MoveBullet()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            EventBus<PlayerTakeDamageEvent>.Publish(new PlayerTakeDamageEvent(damage));
            Destroy(gameObject);
        }
        if (collider.gameObject.CompareTag("Procedural"))
        {
            Destroy(gameObject);
        }
    }
}