using UnityEngine;
using UnityEngine.UI;

public class CloseMenu : MonoBehaviour
{
    public GameObject menu;
    public InventoryObject inventory;

    public Button InventoryButton;
    public Button AvatarButton;
    public Button SettingsButton;
    public Button QuitButton;
    private ColorBlock selectedColor;
    private ColorBlock basicColor;
    
    public void CloseOnClick()
    {
        if (inventory != null)
            inventory.Save();
            
        menu.SetActive(false);
        UIOpenMenu.menuIsOpen = false;

        selectedColor = InventoryButton.colors;
        selectedColor.normalColor = new Color(0.349f, 0.349f, 0.349f);
        InventoryButton.colors = selectedColor;

        basicColor = AvatarButton.colors;
        basicColor.normalColor = new Color(1f, 1f, 1f);
        AvatarButton.colors = basicColor;
        SettingsButton.colors = basicColor;
        QuitButton.colors = basicColor;
    }
}
