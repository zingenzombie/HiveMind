using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class UIHotbarManager : MonoBehaviour
{
    // Set parent
    public Transform hotbarParent;

    // Number of items to be used in hotbar
    public int numberOfSlots;

    // Aspects of hotbar graphic - used image, color, distance
    public bool centerSlots;
    private List<GameObject> slotObjects = new List<GameObject>();
    public GameObject slotPrefab;
    public float slotOffset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        // Make sure slotPrefab true
        slotPrefab.SetActive(true);

        // Make sure slotObjects is empty
        if (slotObjects != null) 
        {
            for (int i = 0; i < slotObjects.Count; i++)
            {
                Destroy(slotObjects[i]);
            }
        }
    }
    
    void Start()
    {
        float totalWidth = 0;

        for (int i = 0; i < numberOfSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, hotbarParent);
            slotObj.name = "Inventory Slot " + (i + 1).ToString();

            RectTransform rectTransform = slotObj.GetComponent<RectTransform>();

            if (i == 0) 
            {
                // Calculate total width of all slots
                totalWidth = (numberOfSlots * rectTransform.sizeDelta.x) + ((numberOfSlots - 1) * slotOffset);
            }

            if (rectTransform != null)
            {
                if (centerSlots)
                {
                    rectTransform.anchoredPosition = new Vector2((rectTransform.sizeDelta.x / 2) + (Screen.width / 2) - (totalWidth / 2) + (i * (rectTransform.sizeDelta.x + slotOffset)), 0);
                }
                else
                {
                    // Get width of object itself
                    rectTransform.anchoredPosition = new Vector2((rectTransform.sizeDelta.x / 2) + (i * (rectTransform.sizeDelta.x + slotOffset)), 0);
                }
            }

            TextMeshProUGUI slotLabel = slotObj.GetComponentInChildren<TextMeshProUGUI>();

            if (slotLabel != null) 
            {
                slotLabel.text = (i + 1).ToString();
            }
        }

        slotPrefab.SetActive(false);
    }
}
