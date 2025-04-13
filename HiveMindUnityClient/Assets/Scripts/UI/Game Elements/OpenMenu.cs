using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OpenMenu : MonoBehaviour
{
    public static bool menuIsOpen = false;
    public GameObject menu;
    
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
            InventoryArea.SetActive(true);
            AvatarsArea.SetActive(false);
            SettingsArea.SetActive(false);
            QuitArea.SetActive(false);
            
            menu.SetActive(!menu.activeSelf);

            menuIsOpen = menu.activeSelf;
        }
    }
}
