using UnityEngine;

public class BulletScript : MonoBehaviour
{
    private Rigidbody2D _rigidBody;

    private readonly int _speed = 20;
    private Vector2 _speedVector;

    void Start()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        _rigidBody.linearVelocity = _speedVector;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(gameObject);
    }

    public void Init(Vector2 direction, float distance, bool forPlayer)
    {
        _speedVector = direction / distance * _speed;
        if (forPlayer) { gameObject.layer = LayerMask.NameToLayer("PlayerBullet"); }
    }
}
