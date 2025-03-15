using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using System;
using System.Linq;
using Unity.Mathematics;

public class UIHotbarManager : MonoBehaviour
{
    // Set parent
    public GameObject hotbarParent;
    private RectTransform hotbarRectTransform;

    // Setting this to true confirms the hotbar display is being used for the inventory, not the actual hotbar
    public bool inventoryAsset;

    // Aspects of hotbar graphic - whether the slots are centered on the screen, the used image, color, distance
    public bool centerSlots;
    public GameObject slotPrefab;
    public float xOffset;
    public float yOffset;

    // Objects refering to the inventory - the playerHotbar is used to set the numberOfSlots and slotObjects fields
    public InventoryObject playerHotbar;
    private int numberOfSlots;

    private List<GameObject> slotObjects = new List<GameObject>();

    void Awake()
    {
        hotbarParent.SetActive(true);
        hotbarRectTransform = hotbarParent.GetComponent<RectTransform>();

        numberOfSlots = playerHotbar.Container.Count;

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
        if (!inventoryAsset)
        {
            PrintItems(0);
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
            GameObject slotObj = Instantiate(slotPrefab, hotbarRectTransform);
            slotObj.name = "Hotbar Slot " + (i + 1);

            RectTransform rectTransform = slotObj.GetComponent<RectTransform>();
            rectHeight = rectTransform.sizeDelta.y;

            if (currWidth + rectTransform.sizeDelta.x > hotbarRectTransform.rect.width && currentRowSlots.Count > 0)
            {
                OffsetRow(currentRowSlots, currWidth);
                currentRowSlots.Clear();

                currWidth = 0;
                currHeight += rectTransform.sizeDelta.y + yOffset;

                totalHeight -= rectHeight;
            }

            if (rectTransform != null)
                rectTransform.anchoredPosition = new Vector2(currWidth + rectTransform.sizeDelta.x / 2, -currHeight - (rectTransform.sizeDelta.y / 2) - verticalOffset);

            currWidth += rectTransform.sizeDelta.x + xOffset;
            currentRowSlots.Add(slotObj);

            TextMeshProUGUI slotLabel = slotObj.GetComponentInChildren<TextMeshProUGUI>();
            if (slotLabel != null)
                slotLabel.text = (i + 1).ToString();

            InstantiateIcons(i, rectTransform, slotObj);
        }

        if (currentRowSlots.Count > 0)
        {
            OffsetRow(currentRowSlots, currWidth);
            totalHeight -= rectHeight;
        }

        return totalHeight;
    }

    void InstantiateIcons(int i, RectTransform rectTransform, GameObject slotObj)
    {
            GameObject iconObj = Instantiate(playerHotbar.Container[i].item, rectTransform);
            iconObj.name = playerHotbar.Container[i].item.name;

            for (int j = 0; j < iconObj.GetComponentCount(); j++)
            {
                Component component = iconObj.GetComponentAtIndex(j);
                if (!(component is Renderer) && !(component is MeshRenderer) && !(component is SkinnedMeshRenderer) && 
                    !(component is SpriteRenderer) && !(component is MeshFilter) && !(component is Transform))
                {
                    Destroy(component);
                }
            }
            
            if (iconObj.TryGetComponent<RectTransform>(out RectTransform iconTransform))
            {
                Vector2 newScale = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y);
                iconTransform.sizeDelta = newScale;

                iconTransform.anchorMin = new Vector2(0.5f, 0.5f);
                iconTransform.anchorMax = new Vector2(0.5f, 0.5f);
                iconTransform.pivot = new Vector2(0.5f, 0.5f);

                iconTransform.anchoredPosition = Vector2.zero;
            }
            else if (iconObj.TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
            {
                iconObj.transform.SetParent(slotObj.transform, false); 

                iconObj.transform.localPosition = Vector3.zero;

                float scaleFactor = Mathf.Min(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y) / 2.0f;
                iconObj.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);

                Bounds bounds = meshRenderer.bounds;
                Vector3 centerOffset = bounds.center - iconObj.transform.position;
                iconObj.transform.localPosition -= centerOffset;

                iconObj.transform.localRotation = new Quaternion(iconObj.transform.localRotation.x, iconObj.transform.localRotation.y, 0, 0);
            
                meshRenderer.gameObject.layer = LayerMask.NameToLayer("Icon");
            }
            else
            {
                TextMeshProUGUI itemText = gameObject.AddComponent<TextMeshProUGUI>();
                itemText.transform.SetParent(slotObj.transform, false);

                RectTransform textTransform = itemText.GetComponent<RectTransform>();
                textTransform.sizeDelta = rectTransform.sizeDelta;

                textTransform.anchorMin = new Vector2(0.5f, 0.5f);
                textTransform.anchorMax = new Vector2(0.5f, 0.5f);
                textTransform.pivot = new Vector2(0.5f, 0.5f);
                iconTransform.anchoredPosition = Vector2.zero;

                itemText.enableAutoSizing = true;
                itemText.fontSizeMin = 0.5f;
                itemText.fontSizeMax = 100f;
                itemText.alignment = TextAlignmentOptions.Center;
            }
    }
    

    void OffsetRow(List<GameObject> rowSlots, float rowWidth)
    {
        float offset = 0;
        if (centerSlots) 
        {
            offset = (hotbarRectTransform.rect.width - rowWidth) / 2;
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
