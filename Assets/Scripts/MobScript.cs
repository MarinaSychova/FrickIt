using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MobScript : MonoBehaviour
{
    private CapsuleCollider2D _collider;
    private Rigidbody2D _rigidBody;
    private ParticleSystem _particleSystem;

    private GameObject _nodes;
    private PlayerScript _playerScript;
    private MediatorScript _mediatorScript;

    private int _health = 100;
    private float _healTime = 1; // Time is takes to regain a set number of health points
    private bool _needRepair = false; // True when the _health is equall or bellow a set value
    private bool _insideRepair = false; // True if the mob is inside the repair point
    private GameObject[] _repairPoints; // Nodes that are inside the repair points
    private readonly Dictionary<string, string> _repairForSectors = new()
    {
        {"A", "I" },
        {"B", "I" },
        {"C", "K" },
        {"D", "K" },
        {"E", "I" },
        {"F", "I" },
        {"G", "G" },
        {"H1", "G" },
        {"H2", "I" },
        {"I", "I" },
        {"J", "K" },
        {"K", "K" },
        {"L", "K" }
    }; // Repair points assigned for each sector

    private float _gravity;
    private readonly float _moveSpeed = 4;
    private readonly Vector2 _jumpUpBoost = new(0.6f, 1.5f);

    private bool _midMove = false; // True if the mob is still moving from one node to the other

    public bool OnFixDuty = false; // True if the mob is assigned 
    private bool _nearPoint = false; // True if the mob is near the computer access point
    private float _fixTime = 2; // Time to restore the computer's set number of health points

    public bool PlayerVisible = false; // True if the player is withing a shooting range
    public bool Kill = false; // True if the mob was given a task to kill the player

    public Transform ClosestNode; // Closest node to the mob
    private Transform _destination; // The next node the mob needs to move to to reach the final destination
    private Transform _finalDestination; // Either the player's position or the repair point in most cases

    private LayerMask _floorLayerMask;
    private LayerMask _playerBulletLayerMask;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _collider = GetComponent<CapsuleCollider2D>();
        _rigidBody = GetComponent<Rigidbody2D>();
        _particleSystem = transform.GetChild(2).GetComponent<ParticleSystem>();
        _nodes = GameObject.FindGameObjectWithTag("Nodes");
        _playerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>();
        _mediatorScript = GameObject.FindGameObjectWithTag("Mediator").GetComponent<MediatorScript>();
        _repairPoints = GameObject.FindGameObjectsWithTag("RepairNode");
        _gravity = Mathf.Abs(Physics2D.gravity.y);
        _floorLayerMask = LayerMask.GetMask("Floor");
        _playerBulletLayerMask = LayerMask.NameToLayer("PlayerBullet");
        ClosestNode = FindClosestNode();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (_needRepair)
        {
            if (_insideRepair)
            {
                if (_healTime > 0) { _healTime -= Time.deltaTime; }
                else
                {
                    _health += 10;
                    _healTime = 1;
                }
                if (_health == 100)
                {
                    _insideRepair = false;
                    _finalDestination = ClosestNode.GetComponent<NodeScript>().Neighbors[0].GetComponent<NodeScript>().Neighbors[0];
                }
            }
            else { Move(); }
        }
        else if (Kill)
        {
            if (!PlayerVisible)
            {
                _finalDestination = _playerScript.GetComponent<PlayerScript>().ClosestNode;
                Move();
            }
        }
        else if (OnFixDuty)
        {
            if (_nearPoint)
            {
                if (_fixTime > 0) { _fixTime -= Time.deltaTime; }
                else
                {
                    _mediatorScript.AffectComputer(_finalDestination, true);
                    _fixTime = 2;
                }
            }
            else { Move(); }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If the mob is shot by the player -> go after the player
        if (collision.gameObject.layer == _playerBulletLayerMask)
        {
            _health -= 10;
            _particleSystem.Emit(20);
            if (_health <= 0) { _mediatorScript.DestroyMob(gameObject); }
            Kill = true;
            // If repair is needed -> assing someone else for the killing duty
            if (_health == 50)
            {
                _needRepair = true;
                FindClosestRepair();
                _mediatorScript.SendKillDutyApplication();
            }
        }
        // If the mob have hit something while moving from one node to another -> let them fall
        else if (_midMove)
        {
            _rigidBody.linearVelocity = Vector2.zero;
            _midMove = false;
        }
    }

    private void Move()
    {
        // If the mob is at the destination or went a bit farther -> stop them
        if (_midMove)
        {
            if (transform.position.x * _rigidBody.linearVelocityX >= _destination.position.x * _rigidBody.linearVelocityX)
            {
                _rigidBody.linearVelocity = Vector2.zero;
                _midMove = false;
            }
        }
        // If the mob is standing on the floor
        else if (_collider.IsTouchingLayers(_floorLayerMask))
        {
            // If the player is within the shooting range and the mob does not need to get repaired -> stop the mob
            if (PlayerVisible && !_needRepair)
            {
                _rigidBody.linearVelocity = Vector2.zero;
                return;
            }
            ClosestNode = FindClosestNode();
            // If the final destination was reached
            if (ClosestNode == _finalDestination)
            {
                _rigidBody.linearVelocity = Vector2.zero;
                // If the mob is being repaired
                if (_needRepair)
                {
                    // If the mob is repaired -> go after the player again
                    if (_health > 50)
                    {
                        _needRepair = false;
                        _finalDestination = _playerScript.ClosestNode;
                    }
                    else { _insideRepair = true; }
                }
                // If the mob is on fixing duty and they reached the assigned for them computer
                else if (OnFixDuty) { _nearPoint = true; }
                return;
            }
            _destination = FindNextNode();
            if (ClosestNode.position.y < _destination.position.y)
            {
                // Move the mob closer to the closest node if they need to jump to the next one
                if (Mathf.Abs(ClosestNode.position.x - transform.position.x) <= 0.1)
                {
                    _rigidBody.linearVelocity = Vector2.zero;
                    _midMove = true;
                    JumpUp();
                }
                else
                {
                    _destination = ClosestNode;
                    _midMove = true;
                    Walk();
                }
            }
            else if (ClosestNode.position.y > _destination.position.y)
            {
                _rigidBody.linearVelocity = Vector2.zero;
                _midMove = true;
                Fall();
            }
            else
            {
                _midMove = true;
                Walk();
            }
        }
    }

    private void JumpUp()
    {
        float height = _destination.position.y - transform.position.y;
        float length = _destination.position.x - transform.position.x;
        float distance = Mathf.Sqrt(Mathf.Pow(height, 2) + Mathf.Pow(length, 2));
        float angleSin = height / distance;
        float angleCos = length / distance;
        float velocity = Mathf.Sqrt(2 * _gravity * Mathf.Abs(height) / Mathf.Pow(angleSin, 2));
        float velocityX = velocity * angleCos * _jumpUpBoost.x;
        float velocityY = velocity * angleSin * _jumpUpBoost.y;
        _rigidBody.AddForce(new Vector2(velocityX, velocityY), ForceMode2D.Impulse);
    }

    private void Fall()
    {
        float length = _destination.position.x - transform.position.x;
        float height = transform.position.y - _destination.position.y;
        float velocity = length / Mathf.Sqrt(2 * height / _gravity);
        _rigidBody.AddForce(new Vector2(velocity, 0), ForceMode2D.Impulse);
    }

    private void Walk()
    {
        _rigidBody.linearVelocityX = transform.position.x < _destination.position.x ? _moveSpeed : -_moveSpeed;
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

    private Transform FindNextNode()
    {
        // Use the A* algorithm to find the next node to move closer to the final destination
        Dictionary<Transform, int> gScores = new();
        foreach (Transform node in _nodes.transform) { gScores[node] = int.MaxValue; }
        gScores[ClosestNode] = 0;

        void computeGScores(Transform node, int iteration)
        {
            int gScore = gScores[node] + 1;
            foreach (Transform neighbor in node.GetComponent<NodeScript>().Neighbors)
            {
                if (gScores[neighbor] > gScore)
                {
                    gScores[neighbor] = gScore;
                    if (iteration < 2) { computeGScores(neighbor, iteration + 1); }
                }
            }
        }

        computeGScores(ClosestNode, 0);
        Dictionary<Transform, int> fScores = new();
        // If the heuristics of the nodes are not computed by the Mediator -> compute them yourself
        if (_needRepair || OnFixDuty)
        {
            Dictionary<Transform, int> hScores = new();
            foreach (Transform node in _nodes.transform) { hScores[node] = int.MaxValue; }
            hScores[_finalDestination] = 0;

            void computeHScores(Transform node)
            {
                NodeScript nodeScript = node.GetComponent<NodeScript>();
                int hScore = hScores[node] + 1;
                foreach (Transform neighbor in nodeScript.Neighbors)
                {
                    if (hScores[neighbor] > hScore)
                    {
                        hScores[neighbor] = hScore;
                        computeHScores(neighbor);
                    }
                }
            }

            computeHScores(_finalDestination);
            foreach (Transform node in ClosestNode.GetComponent<NodeScript>().Neighbors)
            {
                fScores[node] = gScores[node] + hScores[node];
            }
        }
        else
        {
            foreach (Transform node in ClosestNode.GetComponent<NodeScript>().Neighbors)
            {
                fScores[node] = gScores[node] + node.GetComponent<NodeScript>().HScore;
            }
        }
        int minF = fScores.Values.Min();
        return fScores.FirstOrDefault(n => n.Value == minF).Key;
    }

    private void FindClosestRepair()
    {
        ClosestNode = FindClosestNode();
        string currentSector = ClosestNode.GetComponent<NodeScript>().Sector;
        string closestRepairSector = _repairForSectors[currentSector];
        foreach (GameObject point in _repairPoints)
        {
            if (point.GetComponent<NodeScript>().Sector == closestRepairSector)
            {
                _finalDestination = point.transform;
                break;
            }
        }
    }

    public void GetFixDutyApplication(Transform closestComputerNode, int nodeIndex)
    {
        // Apply for a fixing duty and send another mob to kill the player
        if (Kill) { return; }
        if (!_mediatorScript.FixDuty[nodeIndex])
        {
            _mediatorScript.ApplyFixDutyApplication(nodeIndex, gameObject);
            _finalDestination = closestComputerNode;
            OnFixDuty = true;
            _mediatorScript.SendKillDutyApplication();
        }
    }

    public void SignOffFixDuty()
    {
        OnFixDuty = false;
        _finalDestination = _playerScript.ClosestNode;
        Kill = true;
    }
}
