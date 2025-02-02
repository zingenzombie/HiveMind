using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// Used for UI hexagonal buttons - set 2D polygon colliders to match edges of hexagonal button, add script, set onClick operation in Unity
public class HexButtonClickable : MonoBehaviour
{
    [SerializeField] private PolygonCollider2D polygonCollider2D;
    public UnityEvent onClick;

    private void Start()
    {
        if (polygonCollider2D == null)
        {
            polygonCollider2D = GetComponent<PolygonCollider2D>();
        }
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            Vector2 mousePosition = Input.mousePosition;

            if (polygonCollider2D.OverlapPoint(mousePosition))
            {
                onClick?.Invoke();
            }
        }
    }
}