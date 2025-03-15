using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIOpenMenu : MonoBehaviour
{
    public static bool menuIsOpen = false;
    public GameObject menu;
    public InventoryObject inventory;

    public GameObject InventoryArea;
    public GameObject AvatarsArea;
    public GameObject SettingsArea;
    public GameObject QuitArea;

    

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

            InventoryArea.SetActive(true);
            AvatarsArea.SetActive(false);
            SettingsArea.SetActive(false);
            QuitArea.SetActive(false);
            
            menu.SetActive(!menu.activeSelf);

            menuIsOpen = menu.activeSelf;
        }
    }
}
