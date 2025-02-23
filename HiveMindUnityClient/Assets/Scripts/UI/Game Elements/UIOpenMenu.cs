using UnityEngine;

public class UIOpenMenu : MonoBehaviour
{
    public static bool menuIsOpen = false;
    public GameObject menu;
    public InventoryObject inventory;

    void Start()
    {
        if (menu != null)
        {
            menu.SetActive(false);
            menuIsOpen = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && menu != null)
        {
            if (inventory != null)
            {
                if (menu.activeSelf)
                    inventory.Load();
                else
                    inventory.Save();
            }
            
            menu.SetActive(!menu.activeSelf);
            menuIsOpen = menu.activeSelf;
        }
    }

    public void CloseOnClick()
    {
        if (inventory != null)
            inventory.Save();
        menu.SetActive(false);
        menuIsOpen = menu.activeSelf;
    }
}
