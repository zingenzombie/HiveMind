using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public enum ItemType { // can use whatever types
    Objects,
    Spawnables
}

public abstract class ItemObject : ScriptableObject
{
    public int id = -1;
    public GameObject prefab;
    public ItemType type;
    [TextArea(15,20)]
    public string description;
    public Item CreateItem()
    {
        Item newItem = new Item(this);
        return newItem;
    }
}

public class ConcreteItemObject : ItemObject {} // inherit itemObject for definitions in code

[System.Serializable]
public class Item
{
    public GameObject Prefab;
    public int Id;
    public Item(ItemObject item) 
    {
        Prefab = item.prefab;
    }
    public Item()
    {
        Prefab = Resources.Load<GameObject>("Items/EmptyPrefab");
    }
}