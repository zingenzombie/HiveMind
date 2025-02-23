using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Database", menuName = "Inventory System/Items/Database")]
public class ItemDatabaseObject : ScriptableObject, ISerializationCallbackReceiver
{
    public GameObject[] Items;
    public Dictionary<GameObject, int> GetId = new Dictionary<GameObject, int>();
    public Dictionary<int, GameObject> GetItem = new Dictionary<int, GameObject>();

    public void OnAfterDeserialize()
    {
        GetId = new Dictionary<GameObject, int>();
        GetItem = new Dictionary<int, GameObject>();
        for (int i = 0; i < Items.Length; i++) 
        {
            GetId.Add(Items[i], i);            
            GetItem.Add(i, Items[i]);
        }
    }

    public void OnBeforeSerialize()
    {
        
    }
}
