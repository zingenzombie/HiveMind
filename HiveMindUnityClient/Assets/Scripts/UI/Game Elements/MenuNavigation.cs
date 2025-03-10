using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuNavigation : MonoBehaviour
{
    public GameObject InventoryArea;
    public GameObject AvatarsArea;
    public GameObject SettingsArea;
    public GameObject QuitArea;

    public void Activate(int option) 
    {
        InventoryArea.SetActive(false);
        AvatarsArea.SetActive(false);
        SettingsArea.SetActive(false);
        QuitArea.SetActive(false);

        switch (option)
        {
            case 0:
                InventoryArea.SetActive(true);
                break;
            case 1:
                AvatarsArea.SetActive(true);
                break;
            case 2:
                SettingsArea.SetActive(true);
                break;
            case 3:
                QuitArea.SetActive(true);
                break;
            default:
                Debug.Log("MenuNavigation.activate: Incorrect index inputted.");
                break;
        }
    }
}
