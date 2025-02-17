using TMPro;
using UnityEngine;
using UnityEngine.Animations;

public class ItemsMenu : MonoBehaviour
{
    // Get scripts for hotbar and inventory
    public UIHotbarManager hotbar;
    public UIHotbarManager inventory;

    // Get text for hotbar and inventory
    public TextMeshProUGUI hotbarText;
    public TextMeshProUGUI inventoryText;
    
    void Start()
    {
        Canvas canvas = GetComponent<Canvas>();

        RectTransform rectTransformHotbar = hotbarText.GetComponent<RectTransform>();
        rectTransformHotbar.anchoredPosition = new Vector2(0, 0);

        float inventoryY = hotbar.PrintItems(rectTransformHotbar.sizeDelta.y) - 20;

        RectTransform rectTransformInventory = inventoryText.GetComponent<RectTransform>();
        rectTransformInventory.anchoredPosition = new Vector2(0, inventoryY);

        float spawnsY = inventory.PrintItems(-inventoryY + rectTransformInventory.sizeDelta.y) - 20;

        Debug.Log(rectTransformHotbar.sizeDelta.y + " " + -inventoryY);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
