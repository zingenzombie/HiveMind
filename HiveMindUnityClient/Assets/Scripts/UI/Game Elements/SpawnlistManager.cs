using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;

public class SpawnlistManager : MonoBehaviour
{
    // Set parent
    public GameObject spawnlistParent;
    private RectTransform spawnlistRectTransform;

    // Get prefabs for heading as well as any following entries
    public GameObject headingPrefab;
    public List<GameObject> entryPrefabs;

    // Objects refering to the inventory - the playerHotbar is used to set the numberOfSlots and slotObjects fields
    public InventoryObject playerSpawnlist;
    private int numberOfSlots;
    private List<GameObject> slotObjects = new List<GameObject>();

    void Awake()
    {
        spawnlistRectTransform = spawnlistParent.GetComponent<RectTransform>();

        numberOfSlots = playerSpawnlist.Container.Items.Count;

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
        GameObject headingObj = Instantiate(headingPrefab, spawnlistRectTransform);
        headingObj.name = "Heading Object";

        RectTransform headingTransform = headingObj.GetComponent<RectTransform>();
        float currHeight = headingTransform.sizeDelta.y;

        headingTransform.anchoredPosition = new Vector2(headingTransform.sizeDelta.x / 2, -headingTransform.sizeDelta.y - verticalOffset); // TODO: Could be fixed up

        int entryIndex = 0;
        for (int i = 0; i < numberOfSlots; i++) 
        {
            GameObject rowObj = Instantiate(entryPrefabs[entryIndex], spawnlistRectTransform);
            rowObj.name = "Spawnlist Row " + (i + 1).ToString();

            RectTransform rowTransform = rowObj.GetComponent<RectTransform>();
            rowTransform.anchoredPosition = new Vector2(rowTransform.sizeDelta.x / 2, -rowTransform.sizeDelta.y - verticalOffset - currHeight);
            currHeight += rowTransform.sizeDelta.y;

            TMP_Text[] textComponents = rowObj.GetComponentsInChildren<TMP_Text>();
            if (textComponents.Length >= 3) 
            {
                textComponents[0].text = playerSpawnlist.Container.Items[i].item.Prefab.name;
                textComponents[1].text = playerSpawnlist.Container.Items[i].item.Date;
                textComponents[2].text = playerSpawnlist.Container.Items[i].item.Creator;
            }

            if (entryIndex + 1 < entryPrefabs.Count) 
            {
                entryIndex++;
            }
            else 
            {
                entryIndex = 0;
            }
        }

        return currHeight;
    }
}
