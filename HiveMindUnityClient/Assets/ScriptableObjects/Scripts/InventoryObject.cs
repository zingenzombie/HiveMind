using UnityEngine;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Inventory", menuName = "Inventory System/Inventory")]
public class InventoryObject : ScriptableObject
{
    public string savePath;
    public ItemDatabaseObject database;
    public Inventory Container;
    private void OnEnable()
    {
        if (Container == null)
        {
            Container = new Inventory();
        }
    }

    public void AddListItem(Item _item, int _amount)
    {
        for (int i = 0; i < Container.Items.Count; i++) 
        {
            if(Container.Items[i].item.Id == _item.Id) 
            {
                Container.Items[i].AddAmount(_amount);
                return;
            }
        }
    }

    public InventorySlot SetEmptySlot(Item _item, int _amount, string _creator, string _date) {
        for (int i = 0; i < Container.Items.Count; i++) {
            if (Container.Items[i].ID <= -1)
            {
                Container.Items[i].UpdateSlot(_item.Id, _item, _amount, _creator, _date);
                return Container.Items[i];
            }
        } 
        // inventory full
        return null;
    }

    [ContextMenu("Save")]
    public void Save()
    {
        IFormatter formatter = new BinaryFormatter();
        Stream stream = new FileStream(string.Concat(Application.persistentDataPath, savePath), FileMode.Create, FileAccess.Write);
        formatter.Serialize(stream, Container);
        stream.Close();
    }

    [ContextMenu("Load")]
    public void Load() 
    {
        if(File.Exists(string.Concat(Application.persistentDataPath, savePath)))
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(string.Concat(Application.persistentDataPath, savePath), FileMode.Open, FileAccess.Read);
            Inventory newContainer = (Inventory)formatter.Deserialize(stream);

            for (int i = 0; i < Container.Items.Count; i++)
            {
                Container.Items[i].UpdateSlot(newContainer.Items[i].ID, newContainer.Items[i].item, newContainer.Items[i].amount, newContainer.Items[i].creator, newContainer.Items[i].date);
            }

            stream.Close();
        }
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        Container = new Inventory();
    }

    public void MoveItem(InventorySlot item1, InventorySlot item2) 
    {
        InventorySlot temp = new InventorySlot(item2.ID, item2.item, item2.amount, item2.creator, item2.date);
        item2.UpdateSlot(item1.ID, item1.item, item2.amount, item2.creator, item2.date);
        item1.UpdateSlot(temp.ID, temp.item, temp.amount, item2.creator, item2.date);
    }

    public void RemoveItem(Item _item) {
        for (int i = 0; i < Container.Items.Count; i++)
        {
            if (Container.Items[i].item == _item)
            {
                Container.Items[i].UpdateSlot(-1, null, 0, "", "");
            }
        }
    }
}

[System.Serializable]
public class Inventory : ISerializationCallbackReceiver
{
    public List<InventorySlot> Items = new List<InventorySlot>();

    public void OnBeforeSerialize() {}

    public void OnAfterDeserialize()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].ID == -1)
            {
                Items[i] = new InventorySlot();
            }
        }
    }

    public Inventory()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i] = new InventorySlot();
        }
    }
}

[System.Serializable]
public class InventorySlot
{
    [SerializeField] public int ID = -1;
    [SerializeField] public Item item;
    [SerializeField] public int amount;
    [SerializeField] public string creator;
    [SerializeField] public string date;

    public InventorySlot(int _id, Item _item, int _amount, string _creator, string _date)
    {
        ID = _id;
        item = _item;
        amount = _amount;
        creator = _creator;
        date = _date;
    }

    public InventorySlot()
    {
        ID = -1;
        item = null;
        amount = 0;
        creator = "";
        date = "";
    }

    public void UpdateSlot(int _id, Item _item, int _amount, string _creator, string _date)
    {
        ID = _id;
        item = _item;
        amount = _amount;
        creator = _creator;
        date = _date;
    }

    public void AddAmount(int value)
    {
        amount += value;
    }
}