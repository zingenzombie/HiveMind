using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Database", menuName = "Inventory System/Items/Database")]
public class ItemDatabaseObject : ScriptableObject, ISerializationCallbackReceiver
{
    public List<ItemObject> Items = new List<ItemObject>();
    public List<GameObject> ItemPrefabs = new List<GameObject>();
    public Dictionary<int, ItemObject> GetItem = new Dictionary<int, ItemObject>();

    public void OnAfterDeserialize()
    {
        GetItem.Clear();
        //Items.Clear();
        ItemPrefabs.Clear();
    }

    public void OnBeforeSerialize()
    {
        GetItem.Clear();
        //Items.Clear();
        ItemPrefabs.Clear();

        for (int i = 0; i < Items.Count; i++) 
        {
            GetItem.Add(i, Items[i]);
            ItemPrefabs.Add(Items[i].prefab);
        }
    }

    public void AddItem(GameObject prefab, string description)
    {
        #if UNITY_EDITOR

        int index = ItemPrefabs.FindIndex(item => item == prefab);

        if (index == -1)
        {
            // create itemObject
            ItemObject itemObj = ScriptableObject.CreateInstance<ConcreteItemObject>(); // Ensure this is a concrete class

            itemObj.prefab = prefab;
            itemObj.type = ItemType.Objects;
            itemObj.description = description;
            itemObj.id = Items.Count;

            string path = $"Assets/Resources/Items/{prefab.name}.asset";
            UnityEditor.AssetDatabase.CreateAsset(itemObj, path);
            UnityEditor.AssetDatabase.SaveAssets();

            ItemPrefabs.Add(prefab);
            Items.Add(itemObj);

            GetItem[Items.Count - 1] = itemObj;
        }

        #endif
    }

    public void OnDestroy()
    {
        GetItem.Clear();
        Items.Clear();
        ItemPrefabs.Clear();    
    }
}
