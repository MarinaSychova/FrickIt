using UnityEngine;
using UnityEngine.UI;

public class MenuSetupScript : MonoBehaviour
{
    private SceneSwitchScript _sceneSwitch;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _sceneSwitch = GameObject.FindGameObjectWithTag("SceneSwitch").GetComponent<SceneSwitchScript>();
        transform.GetChild(1).GetComponent<Button>().onClick.AddListener(_sceneSwitch.PlayGame);
        transform.GetChild(2).GetComponent<Button>().onClick.AddListener(_sceneSwitch.QuitGame);
    }
}
