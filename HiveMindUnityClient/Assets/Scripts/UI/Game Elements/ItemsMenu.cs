using TMPro;
using UnityEngine;

public class ItemsMenu : MonoBehaviour
{
    // Get scripts for hotbar, inventory, and spawnlist
    public UIHotbarManager hotbar;
    public UIHotbarManager inventory;
    public SpawnlistManager spawnlist;

    // Get text for hotbar, inventory, and spawnlist
    public TextMeshProUGUI hotbarText;
    public TextMeshProUGUI inventoryText;
    public TextMeshProUGUI spawnlistText;
    
    // Grab RectTransform for bounds so its size can be changed dynamically
    public RectTransform uiSpace;
    
    void Start()
    {
        // Position hotbar elements
        RectTransform rectTransformHotbar = hotbarText.GetComponent<RectTransform>();
        rectTransformHotbar.anchoredPosition = new Vector2(0, 0);

        float inventoryY = -hotbar.PrintItems(rectTransformHotbar.sizeDelta.y);

        // Position inventory elements
        RectTransform rectTransformInventory = inventoryText.GetComponent<RectTransform>();
        rectTransformInventory.anchoredPosition = new Vector2(0, inventoryY);

        float spawnsY = -inventory.PrintItems(-inventoryY + rectTransformInventory.sizeDelta.y); // todo

        // Position spawnlist elements
        RectTransform rectTransformSpawnlist = spawnlistText.GetComponent<RectTransform>();
        rectTransformSpawnlist.anchoredPosition = new Vector2(0, spawnsY);

        float spawnlistY = -spawnlist.PrintItems(-spawnsY + rectTransformSpawnlist.sizeDelta.y);

        uiSpace.sizeDelta = new Vector2(uiSpace.sizeDelta.x, -inventoryY - spawnsY - spawnlistY);
        //uiSpace.anchoredPosition = new Vector2(uiSpace.anchoredPosition.x, uiSpace.anchoredPosition.y + inventoryY + spawnsY + spawnlistY);
    }
}
