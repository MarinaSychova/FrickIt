using UnityEngine;

public class NodeScript : MonoBehaviour
{
    public Transform[] Neighbors;

    public int HScore;

    public string Sector;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.aliceBlue;
        foreach (Transform node in Neighbors)
        {
            Gizmos.DrawLine(transform.position, node.position);
        }
    }
}
