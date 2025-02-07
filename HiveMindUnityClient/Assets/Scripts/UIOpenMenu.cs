using UnityEngine;

public class UIOpenMenu : MonoBehaviour
{
    public GameObject menu;

    void Start()
    {
        if (menu != null)
        {
            menu.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && menu != null)
        {
            menu.SetActive(!menu.activeSelf);
        }
    }
}
