using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;
using TMPro;

public class PlayerScript : MonoBehaviour
{
    private CapsuleCollider2D _collider;
    private Rigidbody2D _rigidBody;
    private Transform _gun;
    private ParticleSystem _particleSystem;
    private Transform _reloadBar;
    private Transform _breakBar;

    private GameObject _nodes;
    public Transform ClosestNode;
    private MediatorScript _mediatorScript;
    [SerializeField] GameObject Bullet;
    private SceneSwitchScript _sceneSwitch;

    private int _health = 100;

    private readonly float _moveSpeed = 6;
    private readonly float _jumpSpeed = 15;
    private Vector2 _moveInput;

    private bool _nearComputer = false;
    private float _breakTime = 0;

    private float _loadTime = 0;

    private LayerMask _floorLayerMask;
    private int _bulletLayerMaskIndex;
    private int _pointLayerMaskIndex;

    private TextMeshProUGUI _healthText;
    private TextMeshProUGUI _helpText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _collider = GetComponent<CapsuleCollider2D>();
        _rigidBody = GetComponent<Rigidbody2D>();
        _gun = transform.GetChild(1);
        _particleSystem = transform.GetChild(2).GetComponent<ParticleSystem>();
        _reloadBar = transform.GetChild(3);
        _breakBar = transform.GetChild(4);
        _nodes = GameObject.FindGameObjectWithTag("Nodes");
        _mediatorScript = GameObject.FindGameObjectWithTag("Mediator").GetComponent<MediatorScript>();
        _sceneSwitch = GameObject.FindGameObjectWithTag("SceneSwitch").GetComponent<SceneSwitchScript>();
        _floorLayerMask = LayerMask.GetMask("Floor");
        _bulletLayerMaskIndex = LayerMask.NameToLayer("Bullet");
        _pointLayerMaskIndex = LayerMask.NameToLayer("AccessPoint");
        _healthText = GameObject.FindGameObjectWithTag("HealthText").GetComponent<TextMeshProUGUI>();
        _healthText.text = "100/100";
        _helpText = GameObject.FindGameObjectWithTag("HelpText").GetComponent<TextMeshProUGUI>();
        _helpText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        ClosestNode = FindClosestNode();
        Run();
        if (_nearComputer) { _helpText.text = "Press E to break"; }
        else { _helpText.text = ""; }

        if (_loadTime > 0)
        {
            _loadTime -= Time.deltaTime;
            _reloadBar.localPosition = new Vector3(_reloadBar.localPosition.x + Time.deltaTime / 2, _reloadBar.localPosition.y, 0);
            _reloadBar.localScale = new Vector3((_reloadBar.localPosition.x + 0.5f) * 2, _reloadBar.localScale.y, 0);
        }
        else
        {
            _reloadBar.localPosition = new Vector3(-0.5f, _reloadBar.localPosition.y, 0);
            _reloadBar.localScale = new Vector3(0, _reloadBar.localScale.y, 0);
        }
        if (_breakTime > 0)
        {
            _breakTime -= Time.deltaTime;
            _breakBar.localPosition = new Vector3(_breakBar.localPosition.x + Time.deltaTime / 4, _breakBar.localPosition.y, 0);
            _breakBar.localScale = new Vector3((_breakBar.localPosition.x + 0.5f) * 2, _breakBar.localScale.y, 0);
        }
        else
        {
            _breakBar.localPosition = new Vector3(-0.5f, _breakBar.localPosition.y, 0);
            _breakBar.localScale = new Vector3(0, _breakBar.localScale.y, 0);
        }
        RotateGun();
    }

    public void OnMove(CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(CallbackContext _)
    {
        if (_collider.IsTouchingLayers(_floorLayerMask)) { _rigidBody.linearVelocity = new Vector2(0, _jumpSpeed); }
    }

    public void OnAttack(CallbackContext context)
    {
        if (context.performed && _loadTime <= 0)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            float distance = Vector2.Distance(_gun.position, mousePosition);
            GameObject bulletInstance = Instantiate(Bullet, _gun.position, Quaternion.identity);
            bulletInstance.GetComponent<BulletScript>().Init(mousePosition - _gun.position, distance, true);
            _loadTime = 1;
        }
    }

    public void OnInteract(CallbackContext context)
    {
        if (_nearComputer)
        {
            if (context.started) { _breakTime = 2; }
            if (context.performed) { _mediatorScript.AffectComputer(ClosestNode, false); }
            if (context.canceled)
            {
                _breakTime = 0;
                _breakBar.localPosition = new Vector3(-0.5f, _breakBar.localPosition.y, 0);
                _breakBar.localScale = new Vector3(0, _breakBar.localScale.y, 0);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == _bulletLayerMaskIndex)
        {
            _health -= 1;
            _healthText.text = $"{_health}/100";
            _particleSystem.Emit(20);
            if (_health <= 0) { _sceneSwitch.ShowLoseMenu(); }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == _pointLayerMaskIndex) { _nearComputer = true; }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == _pointLayerMaskIndex) { _nearComputer = false; }
    }

    private void Run()
    {
        Vector2 velocity = new(_moveInput.x * _moveSpeed, _rigidBody.linearVelocity.y);
        _rigidBody.linearVelocity = velocity;
    }

    private Transform FindClosestNode()
    {
        float smallestDistance = float.MaxValue;
        int index = 0;
        for (int i = 0; i < _nodes.transform.childCount; i++)
        {
            float distance = Vector2.Distance(_nodes.transform.GetChild(i).position, transform.position);
            if (distance < smallestDistance)
            {
                smallestDistance = distance;
                index = i;
            }
        }
        return _nodes.transform.GetChild(index);
    }

    private void RotateGun()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        float distance = Mathf.Clamp(Vector2.Distance(_gun.position, mousePosition), 0.01f, 10000);
        float angle = Mathf.Acos((mousePosition.y - _gun.position.y) / distance) * 180 / Mathf.PI;
        if (mousePosition.x > _gun.position.x) { angle *= -1; }
        _gun.eulerAngles = new Vector3(0, 0, angle);
    }
}
