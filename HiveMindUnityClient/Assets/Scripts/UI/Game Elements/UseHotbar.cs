using System;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class UseHotbar : MonoBehaviour
{
    public InventoryObject playerHotbar;
    public GameObject slotPrefab;
    public float colorShiftAmount = -0.15f;
    private int currHotbarIndex = -1;

    void Update()
    {
        for (int i = 0; i < playerHotbar.Container.Items.Count; i++)
        {
            KeyCode alphaKey = KeyCode.Alpha0 + i + 1;
            KeyCode keypadKey = KeyCode.Keypad0 + i + 1;

            if (Input.GetMouseButtonDown(0) && i == currHotbarIndex) // Using left click as a default. E is inventory key atm.
            {
                Debug.Log("useItemScript(playerHotbar.Container.Items[i].item.Prefab.GetHashCode());"); // Call object placement with this as a reference
                break;
            }
            else if ((Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey)) && playerHotbar.Container.Items[i].item.Prefab != null) 
            {
                if (playerHotbar.Container.Items[i].item.Prefab != null)
                {
                    if (i == currHotbarIndex)
                    {
                        GameObject oldPrefab = playerHotbar.Container.Items[currHotbarIndex].slotPrefab;
                        UndoHighlight(oldPrefab);
                        
                        currHotbarIndex = -1;
                    }
                    else
                    {
                        GameObject prefab = playerHotbar.Container.Items[i].slotPrefab;
                        HighlightSelectedItem(prefab);
                        
                        if (currHotbarIndex != -1)
                        {
                            GameObject oldPrefab = playerHotbar.Container.Items[currHotbarIndex].slotPrefab;
                            UndoHighlight(oldPrefab);
                        }

                        currHotbarIndex = i;
                    }

                    break;
                }
                else if (currHotbarIndex != -1)
                {
                    GameObject oldPrefab = playerHotbar.Container.Items[currHotbarIndex].slotPrefab;
                    UndoHighlight(oldPrefab);

                    currHotbarIndex = -1;
                    break;
                }
            }
        }

        for (int i = playerHotbar.Container.Items.Count; i <= 9; i++) {
            KeyCode alphaKey = KeyCode.Alpha0 + i + 1;
            KeyCode keypadKey = KeyCode.Keypad0 + i + 1;

            if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
            {
                GameObject oldPrefab = playerHotbar.Container.Items[currHotbarIndex].slotPrefab;
                UndoHighlight(oldPrefab);
            }
        }
    }

    private void HighlightSelectedItem(GameObject prefab)
    {                
        Color currentColor = prefab.GetComponent<Image>().color;;

        float newR = Mathf.Clamp(currentColor.r - colorShiftAmount, 0f, 1f);
        float newG = Mathf.Clamp(currentColor.g - colorShiftAmount, 0f, 1f);
        float newB = Mathf.Clamp(currentColor.b - colorShiftAmount, 0f, 1f);

        Color newColor = new Color(newR, newG, newB, currentColor.a);

        prefab.GetComponent<Image>().color = newColor;
    }

    private void UndoHighlight(GameObject prefab) 
    {
        Color templateColor = slotPrefab.GetComponent<Image>().color;
        Color newColor = new Color(templateColor.r, templateColor.g, templateColor.b, templateColor.a);
        
        prefab.GetComponent<Image>().color = newColor;
    }
}