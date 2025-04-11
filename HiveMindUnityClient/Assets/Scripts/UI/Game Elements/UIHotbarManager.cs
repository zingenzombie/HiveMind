using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using Unity.VisualScripting;

public class UIHotbarManager : MonoBehaviour
{
    // Set parent
    public GameObject hotbarParent;
    private RectTransform hotbarRectTransform;

    // Sizes
    private float xSize;
    private float ySize;

    // Setting this to true confirms the hotbar display is being used for the inventory, not the actual hotbar
    public bool isInventoryMenuAsset = false;

    // Aspects of hotbar graphic - whether the slots are centered on the screen, the used image, color, distance
    public bool centerSlots;
    public GameObject slotPrefab;
    public float xOffset;
    public float yOffset;

    // Objects refering to the inventory - the playerHotbar is used to set the numberOfSlots and slotObjects fields
    public InventoryObject playerHotbar;
    private int numberOfSlots;

    // Track width and height of inventory slot
    float slotWidth = 0;
    float slotHeight = 0;

    Dictionary<GameObject, InventorySlot> itemsDisplayed = new Dictionary<GameObject, InventorySlot>();

    // Item to handle drag and drop
    public MouseItem mouseItem = new MouseItem();

    // SETUP

    void Awake()
    {
        hotbarParent.SetActive(true);
        hotbarRectTransform = hotbarParent.GetComponent<RectTransform>();
        
        numberOfSlots = playerHotbar.Container.Items.Count;

        if (isInventoryMenuAsset)
            RegisterItems();
    }

    void Start()
    {
        if (!isInventoryMenuAsset)
            PrintItems(0);
    }

    void Update()
    {
        //UpdateSlots();
    }

    public void RegisterItems()
    { 
        foreach (var itemSlot in playerHotbar.Container.Items)
        {
            if (itemSlot.item.Prefab != null && itemSlot.item.Id != -1)
                playerHotbar.database.AddItem(itemSlot.item.Prefab, "test");    
        }
    }

    /*public void UpdateSlots()
    {
        foreach (KeyValuePair<GameObject, InventorySlot> _slot in itemsDisplayed)
        {
            // remove existing item on slot if dictionary confirms its empty
            if (_slot.Key.transform.childCount > 0)
            {
                Destroy(_slot.Key.transform.GetChild(0).gameObject);
            }

            if (_slot.Value?.ID != -1)
            {
                GameObject itemPrefab = playerHotbar.database.GetItem[_slot.Value.item.Id].prefab;
                
                if (itemPrefab)
                {
                    GameObject itemInstance = Instantiate(itemPrefab);
                    itemInstance.transform.SetParent(_slot.Key.transform, false);

                    itemInstance.transform.localPosition = Vector3.zero;

                    ScaleItem(itemInstance);
                }
            }
        }
    } */
    // TODO: I need to completely rethink the way im trying to implement this - im kinda in the hellzone dimension at the moment. try to find a simpler way.
    // Maybe drag and drop script from here https://www.youtube.com/watch?v=pFpK4-EqHXQ
    // If dragged over space taken by icon A, icon's B object gets set to A and old icon A is set to the B slot
    // Thats what the tutorial I followed did, but changing from images to 3d objects kinda breaks everything.
    // need a simpler method of doing things. transformation and swapping. keep it to just that
    // theres also a question of how to swap items from two different databases but tomorrow just focus on inventory-to-inventory and hotbar-to-hotbar. 

    // CORE DISPLAY FUNCTIONS

    public float PrintItems(float verticalOffset)
    {
        int numCols = findNumCols();
        int numRows = findNumRows(numCols);

        int currNumItems = 0;
        float yCurrOffset = -verticalOffset - slotWidth / 2;

        for (int i = 0; i < numRows; i++)
        {
            float xCurrOffset = slotWidth / 2;
            int currRowItems = 0;

            xCurrOffset += offsetCenterSlots(numCols);

            for (int j = 0; j < numCols; j++)
            {
                if (currNumItems >= numberOfSlots) 
                    return numRows * (slotHeight + yOffset) + verticalOffset;

                GameObject slotObj = Instantiate(slotPrefab, hotbarRectTransform);
                slotObj.name = "(" + (i + 1) + ", " + (j + 1) + ")";

                RectTransform slotTransform = slotObj.GetComponent<RectTransform>();
                slotTransform.anchoredPosition = new Vector2(xCurrOffset, yCurrOffset);

                xCurrOffset += slotTransform.sizeDelta.x + xOffset;

                TextMeshProUGUI slotLabel = slotObj.GetComponentInChildren<TextMeshProUGUI>();
                if (slotLabel != null)
                    slotLabel.text = (currNumItems + 1).ToString();

                InstantiateIcon(j, slotTransform, slotObj);

                currRowItems++;
                currNumItems++;
            }
            yCurrOffset -= slotHeight + yOffset;
        }

        return numRows * (slotHeight + yOffset) + verticalOffset;
    }

    void InstantiateIcon(int index, RectTransform rectTransform, GameObject slotObj)
    {
        GameObject iconObj;

        if (playerHotbar.Container.Items[index].item.Prefab == null)
        {
            GameObject emptyObj = Instantiate(slotObj);
            emptyObj.GetComponent<RectTransform>().localPosition = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y, 0);

            Image imageComponent = emptyObj.GetComponent<Image>();
            if (imageComponent != null)
            {
                Destroy(imageComponent);
            }

            iconObj = Instantiate(emptyObj, rectTransform);
            iconObj.name = "EmptyObject";
        }
        else 
        {
            iconObj = Instantiate(playerHotbar.Container.Items[index].item.Prefab, rectTransform);
            iconObj.name = playerHotbar.Container.Items[index].item.Prefab.name;
        }

        AddEvent(iconObj, EventTriggerType.PointerEnter, delegate { OnEnter(iconObj); });
        AddEvent(iconObj, EventTriggerType.PointerExit, delegate { OnExit(iconObj); });
        AddEvent(iconObj, EventTriggerType.BeginDrag, delegate { OnDragStart(iconObj); });
        AddEvent(iconObj, EventTriggerType.EndDrag, delegate { OnDragEnd(iconObj); });
        AddEvent(iconObj, EventTriggerType.Drag, delegate { OnDrag(iconObj); });

        for (int j = 0; j < iconObj.GetComponentCount(); j++)
        {
            Component component = iconObj.GetComponentAtIndex(j);
            if (!(component is Renderer) && !(component is MeshRenderer) && !(component is SkinnedMeshRenderer) && 
                !(component is SpriteRenderer) && !(component is MeshFilter) && !(component is Transform) && !(component is CanvasRenderer))
            {
                Destroy(component);
            }
        }

        iconObj.transform.SetParent(slotObj.transform, false);

        xSize = rectTransform.sizeDelta.x;
        ySize = rectTransform.sizeDelta.y;

        ScaleItem(iconObj);

        itemsDisplayed.Add(iconObj, playerHotbar.Container.Items[index]);
    }

    public void ScaleItem(GameObject iconObj)
    {
        if (iconObj.TryGetComponent<RectTransform>(out RectTransform iconTransform))
        {
            Vector2 newScale = new Vector2(xSize, ySize);
            iconTransform.sizeDelta = newScale;
            iconTransform.localScale = Vector3.one;

            iconTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconTransform.pivot = new Vector2(0.5f, 0.5f);

            iconTransform.anchoredPosition = Vector2.zero;
        }
        else if (iconObj.TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
        {
            iconObj.transform.localPosition = Vector3.zero;

            float scaleFactor = Mathf.Min(xSize, ySize) / 2.0f;
            iconObj.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);

            Bounds bounds = meshRenderer.bounds;
            Vector3 centerOffset = bounds.center - iconObj.transform.position;
            iconObj.transform.localPosition -= centerOffset;

            iconObj.transform.localRotation = new Quaternion(iconObj.transform.localRotation.x, iconObj.transform.localRotation.y, 0, 0);
            
            meshRenderer.gameObject.layer = LayerMask.NameToLayer("Icon");

            if (isInventoryMenuAsset)
                iconObj.AddComponent<MaskObject>();
        }
        else
        {
            TextMeshProUGUI itemText = gameObject.AddComponent<TextMeshProUGUI>();

            RectTransform textTransform = itemText.GetComponent<RectTransform>();
            textTransform.sizeDelta = new Vector2(xSize, ySize);

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

    // DRAG AND DROP REFERENCE CODE

    public class MouseItem 
    {
        public GameObject obj;
        public InventorySlot item;
        public InventorySlot hoverItem;
        public GameObject hoverObj;
    }

    public void OnEnter(GameObject obj)
    {
        mouseItem.hoverObj = obj;
        if (itemsDisplayed.ContainsKey(obj))
        {
            mouseItem.hoverItem = itemsDisplayed[obj];
            Debug.Log("obj " + obj);
        }
    }

    public void OnExit(GameObject obj)
    {
        mouseItem.hoverObj = null;
        mouseItem.hoverItem = null;
    }

    public void OnDragStart(GameObject obj)
    {
        if (itemsDisplayed.ContainsKey(obj) && itemsDisplayed[obj].ID >= 0)
        {
            var originalIcon = obj;
            var mouseObject = Instantiate(originalIcon, transform.parent);
            mouseObject.name = "DraggedIcon";

            mouseItem.obj = mouseObject;
            mouseItem.item = itemsDisplayed[obj];
        }
    }

    public void OnDragEnd(GameObject obj)
    {
        if (mouseItem.hoverObj)
        {
            playerHotbar.MoveItem(itemsDisplayed[obj], itemsDisplayed[mouseItem.hoverObj]);
        }
        else 
        {
            playerHotbar.RemoveItem(itemsDisplayed[obj].item);
        }
        Destroy(mouseItem.obj);
        mouseItem.item = null;
    }

    public void OnDrag(GameObject obj)
    {
        if (mouseItem.obj != null)
        {
            mouseItem.obj.GetComponent<RectTransform>().position = Input.mousePosition;
        }
    }

    private void AddEvent(GameObject obj, EventTriggerType type, UnityAction action)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    // UTILITIES

    public int findNumRows(int numCols) 
    {
        if (numberOfSlots <= 0)
        {
            return 0;
        }

        return 1 + (numberOfSlots / numCols);
    }

    public int findNumCols()
    {
        GameObject slotObj = Instantiate(slotPrefab, hotbarRectTransform);
        RectTransform rectTransform = slotObj.GetComponent<RectTransform>();

        float screenWidth = hotbarRectTransform.rect.width;
        slotWidth = rectTransform.sizeDelta.x;
        slotHeight = rectTransform.sizeDelta.y;

        Destroy(slotObj);

        return (int)(screenWidth / (slotWidth + xOffset));
    }

    public float offsetCenterSlots(int numCols) 
    {
        if (centerSlots)
        {
            int numRelevantCols = numCols;

            if (numberOfSlots < numCols)
            {
                numRelevantCols = numberOfSlots;
            } 

            float occupiedArea = numRelevantCols * (slotWidth + xOffset) - xOffset;

            return (Screen.width - occupiedArea) / 2;
        }
        return 0;
    }
}
