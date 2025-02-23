using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SpawnlistManager : MonoBehaviour
{
    // Set parent
    public RectTransform spawnlistParent;

    // Get prefabs for heading as well as any following entries
    public GameObject headingPrefab;
    public List<GameObject> entryPrefabs;

    // Objects refering to the inventory - the playerHotbar is used to set the numberOfSlots and slotObjects fields
    public InventoryObject playerSpawnlist;
    private int numberOfSlots;
    private List<GameObject> slotObjects = new List<GameObject>();

    void Awake()
    {
        numberOfSlots = playerSpawnlist.Container.Count;

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
        GameObject headingObj = Instantiate(headingPrefab, spawnlistParent);
        headingObj.name = "Heading Object";

        RectTransform headingTransform = headingObj.GetComponent<RectTransform>();
        float currHeight = headingTransform.sizeDelta.y + 10;

        headingTransform.anchoredPosition = new Vector2(headingTransform.sizeDelta.x / 2, -(headingTransform.sizeDelta.y / 2) - verticalOffset - 10);

        int entryIndex = 0;
        for (int i = 0; i < numberOfSlots; i++) 
        {
            GameObject rowObj = Instantiate(entryPrefabs[entryIndex], spawnlistParent);
            rowObj.name = "Spawnlist Row " + (i + 1).ToString();

            RectTransform rowTransform = rowObj.GetComponent<RectTransform>();
            rowTransform.anchoredPosition = new Vector2(rowTransform.sizeDelta.x / 2, -(rowTransform.sizeDelta.y / 2) - verticalOffset - currHeight);
            currHeight += rowTransform.sizeDelta.y;

            TMP_Text[] textComponents = rowObj.GetComponentsInChildren<TMP_Text>();
            if (textComponents.Length >= 3) 
            {
                textComponents[0].text = playerSpawnlist.Container[i].item.name;
                textComponents[1].text = playerSpawnlist.Container[i].date;
                textComponents[2].text = playerSpawnlist.Container[i].creator;
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
