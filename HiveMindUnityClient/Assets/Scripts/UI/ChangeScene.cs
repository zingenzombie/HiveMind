using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ChangeScene : MonoBehaviour
{
    public PolygonCollider2D polygonCollider2D;
    public bool clickOnly;
    public string sceneName;

    private void Update()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if ((polygonCollider2D != null && polygonCollider2D.OverlapPoint(mousePosition) && Input.GetMouseButtonDown(0)) ||
            (polygonCollider2D == null && ((clickOnly && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))) || 
            (!clickOnly && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.anyKey)))))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}