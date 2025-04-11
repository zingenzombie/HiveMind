using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class UseHotbar : MonoBehaviour
{
    public InventoryObject playerHotbar;

    void Update()
    {
        for (int i = 0; i < playerHotbar.Container.Items.Count; i++)
        {
            KeyCode alphaKey = KeyCode.Alpha0 + i + 1;
            KeyCode keypadKey = KeyCode.Keypad0 + i + 1;

            if ((Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey)) && playerHotbar.Container.Items[i].item.Prefab != null)
            {
                Debug.Log("useItemScript(playerHotbar.Container.Items[i].item.Prefab " + (i) + ");");
            }
        }
    }
}