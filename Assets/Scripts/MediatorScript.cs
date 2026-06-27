using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

public class MediatorScript : MonoBehaviour
{
    private GameObject _nodes;
    private Tilemap _computersTilemap;
    [SerializeField] List<TileBase> ScreenTiles;
    [SerializeField] List<TileBase> PlayTiles;
    [SerializeField] List<TileBase> StopTiles;
    public List<Transform> ComputerClosestNodes;
    /*
     * (int, Vector3Int, Vector3Int, Vector3Int)
     *  /\   /\          /\          /\
     *  ||   ||          ||          ||
     *  ||   ||          ||          bottom right tile of the computer
     *  ||   ||          top left tile of the computer
     *  ||   position of the computer health bar inside tilemap
     *  HP of the computer
     */
    private readonly List<(int, Vector3Int, Vector3Int, Vector3Int)> _computerStates = new()
    {
        (100, new Vector3Int(0, 5, 0), new Vector3Int(-1, 12, 0), new Vector3Int(4, 4, 0)),
        (100, new Vector3Int(-45, 7, 0), new Vector3Int(-49, 12, 0), new Vector3Int(-44, 6, 0)),
        (100, new Vector3Int(-65, -9, 0), new Vector3Int(-67, -4, 0), new Vector3Int(-64, -10, 0)),
        (100, new Vector3Int(-4, 0, 0), new Vector3Int(-5, 2, 0), new Vector3Int(4, -1, 0)),
        (100, new Vector3Int(-18, -35, 0), new Vector3Int(-28, -33, 0), new Vector3Int(-17, -36, 0)),
        (100, new Vector3Int(-32, -25, 0), new Vector3Int(-33, -20, 0), new Vector3Int(-30, -26, 0)),
        (100, new Vector3Int(-15, -30, 0), new Vector3Int(-16, -28, 0), new Vector3Int(-9, -31, 0)),
        (100, new Vector3Int(-52, -23, 0), new Vector3Int(-53, -12, 0), new Vector3Int(-51, -24, 0)),
        (100, new Vector3Int(-69, 9, 0), new Vector3Int(-73, 12, 0), new Vector3Int(-68, 8, 0)),
        (100, new Vector3Int(-57, -1, 0), new Vector3Int(-58, 2, 0), new Vector3Int(-51, -2, 0))
    };

    private readonly int _damageValue = 25;

    private PlayerScript _playerScript;
    private List<GameObject> _mobs;

    public Dictionary<int, GameObject> FixDuty = new();

    private TextMeshProUGUI _goalText;
    private Button _quitButton;
    private SceneSwitchScript _sceneSwitch;

    private void Start()
    {
        _nodes = GameObject.FindGameObjectWithTag("Nodes");
        _computersTilemap = GameObject.FindGameObjectWithTag("Computers").GetComponent<Tilemap>();
        _playerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>();
        _mobs = GameObject.FindGameObjectsWithTag("Mob").ToList();
        _goalText = GameObject.FindGameObjectWithTag("GoalText").GetComponent<TextMeshProUGUI>();
        _goalText.text = $"<sprite index=0> x {_mobs.Count}\r\n<sprite index=1> x {_computerStates.Count}";
        _sceneSwitch = GameObject.FindGameObjectWithTag("SceneSwitch").GetComponent<SceneSwitchScript>();
        _quitButton = GameObject.FindGameObjectWithTag("QuitButton").GetComponent<Button>();
        _quitButton.onClick.AddListener(_sceneSwitch.QuitGame);
    }

    // Update is called once per frame
    void Update()
    {
        RecomputeHScores();
    }

    private void RecomputeHScores()
    {
        // Compute heuristics of the nodes used in the A* algorithm
        Transform endpoint = _playerScript.ClosestNode;
        if (endpoint != null)
        {
            foreach (Transform node in _nodes.transform)
            {
                node.GetComponent<NodeScript>().HScore = int.MaxValue;
            }
            endpoint.GetComponent<NodeScript>().HScore = 0;
            ComputeHScores(endpoint);
        }
    }

    private void ComputeHScores(Transform node)
    {
        NodeScript nodeScript = node.GetComponent<NodeScript>();
        int hScore = nodeScript.HScore + 1;
        foreach (Transform neighbor in nodeScript.Neighbors)
        {
            if (neighbor.GetComponent<NodeScript>().HScore > hScore)
            {
                neighbor.GetComponent<NodeScript>().HScore = hScore;
                ComputeHScores(neighbor);
            }
        }
    }

    public void AffectComputer(Transform closestNode, bool fixAction)
    {
        // Affect the state of the computer near the closestNode
        int computerIndex = ComputerClosestNodes.IndexOf(closestNode);
        (int, Vector3Int, Vector3Int, Vector3Int) computerState = _computerStates[computerIndex];
        if (fixAction)
        {
            // If the computer is not completely broken -> fix it
            if (computerState.Item1 < 100 && computerState.Item1 > 0) { computerState.Item1 += _damageValue; }
            // Else if the computer is fixed or destroyed -> sign off the assigned mob from fixing duty
            else
            {
                FixDuty[computerIndex].GetComponent<MobScript>().SignOffFixDuty();
                FixDuty[computerIndex] = null;
            }
        }
        else if (computerState.Item1 > 0)
        {
            computerState.Item1 -= _damageValue;
            // If noone is assigned to fixing duty of the computer -> assign someone
            if (!FixDuty.ContainsKey(computerIndex) || !FixDuty[computerIndex]) { SendFixDutyApplication(computerIndex); }
            // If the computer is destroyed -> turn off animated tiles of the computer
            if (computerState.Item1 <= 0) { TurnOffAnimatedTiles(computerState); }
        }
        _computerStates[computerIndex] = computerState;
        int screenTileIndex = computerState.Item1 / _damageValue;
        _computersTilemap.SetTile(computerState.Item2, ScreenTiles[screenTileIndex]);
        ChangeGoalText();
    }

    private void TurnOffAnimatedTiles((int, Vector3Int, Vector3Int, Vector3Int) computerState)
    {
        // Turn off animated tiles of the computer
        for (int y = computerState.Item3.y; y >= computerState.Item4.y; y--)
        {
            for (int x = computerState.Item3.x; x <= computerState.Item4.x; x++)
            {
                Vector3Int tilePosition = new(x, y, 0);
                TileBase currentTile = _computersTilemap.GetTile(tilePosition);
                if (currentTile is AnimatedTile)
                {
                    foreach (TileBase animatedTile in PlayTiles)
                    {
                        if (currentTile == animatedTile)
                        {
                            int tileIndex = PlayTiles.IndexOf(animatedTile);
                            _computersTilemap.SetTile(tilePosition, StopTiles[tileIndex]);
                        }
                    }
                }
            }
        }
    }

    private void SendFixDutyApplication(int index)
    {
        FixDuty[index] = null;
        foreach (GameObject mob in _mobs)
        {
            mob.GetComponent<MobScript>().GetFixDutyApplication(ComputerClosestNodes[index], index);
        }
    }

    public void ApplyFixDutyApplication(int index, GameObject mob)
    {
        FixDuty[index] = mob;
    }

    public void SendKillDutyApplication()
    {
        List<MobScript> mobScripts = new();
        foreach (GameObject mob in _mobs)
        {
            MobScript mobScript = mob.GetComponent<MobScript>();
            // If the mob is not free -> do not consider them for the kill duty application
            if (!mobScript.Kill && !mobScript.OnFixDuty) { mobScripts.Add(mobScript); }
        }
        if (mobScripts.Any())
        {
            // Find the closest node to kill the player
            MobScript chosenMobScript = mobScripts[0];
            int smallestH = chosenMobScript.ClosestNode.GetComponent<NodeScript>().HScore;
            for (int i = 1; i < mobScripts.Count; i++)
            {
                int hScore = mobScripts[i].ClosestNode.GetComponent<NodeScript>().HScore;
                if (hScore < smallestH)
                {
                    smallestH = hScore;
                    chosenMobScript = mobScripts[i];
                }
            }
            chosenMobScript.Kill = true;
        }
    }

    public void DestroyMob(GameObject mob)
    {
        _mobs.Remove(mob);
        Destroy(mob);
        ChangeGoalText();
    }

    private void ChangeGoalText()
    {
        int workingComputersCount = 0;
        foreach ((int, Vector3Int, Vector3Int, Vector3Int) state in _computerStates)
        {
            if (state.Item1 > 0) { workingComputersCount++; }
        }
        if (workingComputersCount == 0 && _mobs.Count == 0) { _sceneSwitch.ShowWinMenu(); }
        else { _goalText.text = $"<sprite index=0> x {_mobs.Count}\r\n<sprite index=1> x {workingComputersCount}"; }
    }
}
