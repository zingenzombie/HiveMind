using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    [SerializeField] private bool clickOnly;
    [SerializeField] private string sceneName;

    private void Update()
    {
        if ((clickOnly && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))) || (!clickOnly && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.anyKey)))
        {
            Debug.Log("Loading " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
    }
}