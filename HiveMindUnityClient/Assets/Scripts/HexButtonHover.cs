using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HexButtonHover : MonoBehaviour
{
    [SerializeField] private PolygonCollider2D polygonCollider2D;
    [SerializeField] private float scaleAmount;
    [SerializeField] private Image image;
    [SerializeField] private float colorShiftAmount;
    [SerializeField] private Image shine;

    private Vector3 initialScale;
    private Color currentColor;

    private void Start()
    {        
        initialScale = transform.localScale;
        if (polygonCollider2D == null)
        {
            polygonCollider2D = GetComponent<PolygonCollider2D>();
        }

        if (image != null) {
            currentColor = image.color;
        }
    }

    public void Update()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (polygonCollider2D.OverlapPoint(mousePosition))
        {
            OnHover(true);
        }
        else {
            OnHover(false);
        }
    }

    private void OnHover(bool hovered)
    {
        Vector3 finalScale = initialScale;
        Color newColor = currentColor;
        
        if (hovered) {
            finalScale = initialScale * scaleAmount;

            float newR = Mathf.Clamp(currentColor.r - colorShiftAmount, 0f, 1f);
            float newG = Mathf.Clamp(currentColor.g - colorShiftAmount, 0f, 1f);
            float newB = Mathf.Clamp(currentColor.b - colorShiftAmount, 0f, 1f);

            newColor = new Color(newR, newG, newB);
        }

        shine.gameObject.SetActive(hovered);

        transform.localScale = finalScale;
        transform.DOScale(finalScale, 0.15f);

        image.color = newColor;
    }
}
