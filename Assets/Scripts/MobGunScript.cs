using UnityEngine;

public class MobGunScript : MonoBehaviour
{
    private MobScript _parentScript;

    [SerializeField] private GameObject Bullet;
    private float _reloadTime = 0;

    private Transform _player;

    private LayerMask _layersToIgnore;

    void Start()
    {
        _parentScript = transform.parent.GetComponent<MobScript>();
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        _layersToIgnore = LayerMask.GetMask("Node", "Bullet", "PlayerBullet");
    }

    void FixedUpdate()
    {
        if (_parentScript.Kill)
        {
            // Rotate the gun in the direction of the player
            Vector2 playerPosition = _player.position;
            Vector2 position = transform.position;
            float distance = Mathf.Clamp(Vector2.Distance(position, playerPosition), 0.01f, 10000);
            float angle = Mathf.Acos((playerPosition.y - transform.position.y) / distance) * 180 / Mathf.PI;
            if (playerPosition.x > position.x) { angle *= -1; }
            transform.eulerAngles = new Vector3(0, 0, angle);
            // If the player is visible -> shoot them
            RaycastHit2D hit = Physics2D.Raycast(position, transform.up, Mathf.Infinity, ~_layersToIgnore);
            if (hit.collider.transform == _player)
            {
                _parentScript.PlayerVisible = true;
                if (_reloadTime > 0) { _reloadTime -= Time.deltaTime; }
                else
                {
                    Vector3 position3 = transform.position;
                    GameObject bulletInstance = Instantiate(Bullet, position3, Quaternion.identity);
                    bulletInstance.GetComponent<BulletScript>().Init(playerPosition - position, distance, false);
                    _reloadTime = 1.5f;
                }
            }
            else { _parentScript.PlayerVisible = false; }
        }
    }
}
