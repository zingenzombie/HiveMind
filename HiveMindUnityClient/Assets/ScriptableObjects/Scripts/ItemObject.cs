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
    public string Creator;
    public string Date;
    public Item(ItemObject item) 
    {
        if (item.prefab != null)
        {
            Prefab = item.prefab;
        }
    }

    public void ResetItem()
    {
        Prefab = null;
        Id = -1;
        Creator = "";
        Date = "";
    }
}