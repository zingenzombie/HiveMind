using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Rendering;

public class UIHotbarManager : MonoBehaviour
{
    // Set parent
    public RectTransform hotbarParent;

    // Number of items to be used in hotbar
    public int numberOfSlots;

    // Aspects of hotbar graphic - used image, color, distance
    public bool centerSlots;
    private List<GameObject> slotObjects = new List<GameObject>();
    public GameObject slotPrefab;
    public float xOffset;
    public float yOffset;

    void Awake()
    {
        slotPrefab.SetActive(true);

        if (slotObjects != null) 
        {
            for (int i = 0; i < slotObjects.Count; i++)
            {
                Destroy(slotObjects[i]);
            }
        }
    }
    
    public float PrintItems(float verticalOffset)
    {
        float currWidth = 0;
        float currHeight = 0;
        float rectHeight = 0;
        float totalHeight = -10 - verticalOffset;
        List<GameObject> currentRowSlots = new List<GameObject>();

        for (int i = 0; i < numberOfSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, hotbarParent);
            slotObj.name = "Inventory Slot " + (i + 1).ToString();

            RectTransform rectTransform = slotObj.GetComponent<RectTransform>();
            rectHeight = rectTransform.sizeDelta.y;

            if (currWidth + rectTransform.sizeDelta.x > hotbarParent.rect.width && currentRowSlots.Count > 0)
            {
                OffsetRow(currentRowSlots, currWidth);
                currentRowSlots.Clear();

                currWidth = 0;
                currHeight += rectTransform.sizeDelta.y + yOffset;

                totalHeight -= rectHeight;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(currWidth + rectTransform.sizeDelta.x / 2, -currHeight - (rectTransform.sizeDelta.y / 2) - verticalOffset);
            }

            currWidth += rectTransform.sizeDelta.x + xOffset;
            currentRowSlots.Add(slotObj);

            TextMeshProUGUI slotLabel = slotObj.GetComponentInChildren<TextMeshProUGUI>();
            if (slotLabel != null)
            {
                slotLabel.text = (i + 1).ToString();
            }
        }

        if (currentRowSlots.Count > 0)
        {
            OffsetRow(currentRowSlots, currWidth);
            totalHeight -= rectHeight;
        }

        return totalHeight;
    }

    void OffsetRow(List<GameObject> rowSlots, float rowWidth)
    {
        float offset = 0;
        if (centerSlots) 
        {
            offset = (hotbarParent.rect.width - rowWidth) / 2;
        }

        for (int i = 0; i < rowSlots.Count; i++)
        {
            RectTransform rectTransform = rowSlots[i].GetComponent<RectTransform>();
            
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition += new Vector2(offset, 0);
            }
        }
    }
}
