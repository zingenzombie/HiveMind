using UnityEngine;

public class MenuNavigation : MonoBehaviour
{
    //public int option;
    public GameObject InventoryArea;
    public GameObject AvatarsArea;
    public GameObject SettingsArea;
    public GameObject QuitArea;

    void Start()
    {
        InventoryArea.SetActive(true);
        AvatarsArea.SetActive(false);
        SettingsArea.SetActive(false);
        QuitArea.SetActive(false);
    }

    public void Activate(int option) 
    {
        if (option == 0) 
        {
            InventoryArea.SetActive(true);
            AvatarsArea.SetActive(false);
            SettingsArea.SetActive(false);
            QuitArea.SetActive(false);
        }
        else if (option == 1)
        {
            InventoryArea.SetActive(false);
            AvatarsArea.SetActive(true);
            SettingsArea.SetActive(false);
            QuitArea.SetActive(false);
        }
        else if (option == 2)
        {
            InventoryArea.SetActive(false);
            AvatarsArea.SetActive(false);
            SettingsArea.SetActive(true);
            QuitArea.SetActive(false);
        }
        else if (option == 3)
        {
            InventoryArea.SetActive(false);
            AvatarsArea.SetActive(false);
            SettingsArea.SetActive(false);
            QuitArea.SetActive(true);
        }
        else {
            Debug.Log("MenuNavigation.activate: Incorrect index inputted.");
        }
    }
}
